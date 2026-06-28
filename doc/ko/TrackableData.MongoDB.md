# TrackableData.MongoDB 사용법

`TrackableDataV2.MongoDB`는 TrackableData 객체를 MongoDB `BsonDocument` 컬렉션에 저장하고, tracker 변경분만 `$set`, `$unset`, 배열 연산 등으로 반영합니다.

## 설치

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <PackageReference Include="TrackableDataV2.MongoDB" Version="1.0.0" />
  <PackageReference Include="MongoDB.Driver" Version="3.9.0" />
</ItemGroup>
```

## 기본 연결

```csharp
using MongoDB.Bson;
using MongoDB.Driver;
using TrackableData;
using TrackableData.MongoDB;

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("game");
var collection = database.GetCollection<BsonDocument>("players");
```

mapper는 `IMongoCollection<BsonDocument>`를 사용합니다.

## POCO 저장과 로드

```csharp
public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}

var mapper = new TrackablePocoMongoDbMapper<IPlayer>();

var player = new TrackablePlayer
{
    Name = "Alice",
    Level = 1,
    Gold = 100
};

await mapper.CreateAsync(collection, player, "player:1");

var loaded = await mapper.LoadAsync(collection, "player:1");
Console.WriteLine(loaded.Name);
```

마지막 인자인 `"player:1"`은 문서의 `_id`로 사용됩니다.

## POCO Identity

도메인 프로퍼티를 MongoDB `_id`로 저장해야 하면 `[TrackableProperty("mongodb.identity")]`를 붙입니다.

```csharp
public interface IPlayerAccount : ITrackablePoco<IPlayerAccount>
{
    [TrackableProperty("mongodb.identity")]
    long AccountId { get; set; }

    string Name { get; set; }
}

var mapper = new TrackablePocoMongoDbMapper<IPlayerAccount>();

var account = new TrackablePlayerAccount
{
    AccountId = UniqueInt64Id.GenerateNewId(),
    Name = "Alice"
};

await mapper.CreateAsync(collection, account);
var loaded = await mapper.LoadAsync(collection, account.AccountId);
```

POCO에 `ObjectId Id` 같은 `Id` 프로퍼티가 있으면 key 없는 `CreateAsync(collection, value)`에서 MongoDB가 `_id`를 생성하고 생성된 값을 `Id`에 다시 써 줍니다.

## 변경분만 저장

```csharp
player.SetDefaultTrackerDeep();

player.Level = 2;
player.Gold = 150;

await mapper.SaveAsync(collection, player.Tracker, "player:1");

player.ClearTrackerDeep();
```

## Dictionary/List/Set 저장

```csharp
var dictMapper = new TrackableDictionaryMongoDbMapper<int, string>();
var listMapper = new TrackableListMongoDbMapper<string>();
var setMapper = new TrackableSetMongoDbMapper<int>();

var dict = new TrackableDictionary<int, string> { { 1, "Sword" } };
var list = new TrackableList<string> { "login", "quest" };
var set = new TrackableSet<int> { 1001, 1002 };

await dictMapper.CreateAsync(collection, dict, "player:1", "inventory");
await listMapper.CreateAsync(collection, list, "player:1", "logs");
await setMapper.CreateAsync(collection, set, "player:1", "achievements");
```

`keyValues`는 MongoDB 문서 내부 경로로 사용됩니다.

| 호출 | 의미 |
|------|------|
| `CreateAsync(collection, value, "player:1")` | `_id = "player:1"` 문서에 저장 |
| `CreateAsync(collection, value, "player:1", "inventory")` | `_id = "player:1"` 문서의 `inventory` 필드에 저장 |

Collection value는 primitive 값뿐 아니라 class 값도 사용할 수 있습니다. Mapper는 각 값을 MongoDB BSON serializer를 통해 변환하므로 dictionary, list, set, container mapper에서 class 값도 round-trip 됩니다.

```csharp
public sealed class ItemValue
{
    public string Name { get; set; }
    public int Level { get; set; }
}

var itemMapper = new TrackableDictionaryMongoDbMapper<int, ItemValue>();

var items = new TrackableDictionary<int, ItemValue>
{
    { 1, new ItemValue { Name = "Sword", Level = 10 } }
};

await itemMapper.CreateAsync(collection, items, "player:1", "items");
var loadedItems = await itemMapper.LoadAsync(collection, "player:1", "items");
```

## Collection 변경 저장

```csharp
dict.SetDefaultTrackerDeep();
dict[1] = "Long Sword";
dict.Add(2, "Potion");

await dictMapper.SaveAsync(
    collection,
    (TrackableDictionaryTracker<int, string>)dict.Tracker,
    "player:1",
    "inventory");

dict.ClearTrackerDeep();
```

## Container 저장

Container 프로퍼티는 trackable POCO, Dictionary, List, Set 값을 사용할 수 있습니다. Dictionary/List/Set 멤버는 class 값도 포함할 수 있습니다.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<int, ItemValue> Items { get; set; }
    TrackableList<ItemValue> History { get; set; }
}

var containerMapper = new TrackableContainerMongoDbMapper<IUserData>();

var userData = new TrackableUserData();
userData.Items.Add(1, new ItemValue { Name = "Sword", Level = 10 });
userData.History.Add(new ItemValue { Name = "LoginReward", Level = 1 });

await containerMapper.CreateAsync(collection, userData, "player:1");
var loadedUserData = await containerMapper.LoadAsync(collection, "player:1");
```

로드한 container의 변경분을 저장할 때는 `SetDefaultTracker()`를 사용합니다.

```csharp
loadedUserData.SetDefaultTracker();
loadedUserData.Items[1] = new ItemValue { Name = "Long Sword", Level = 12 };
loadedUserData.History.Add(new ItemValue { Name = "QuestReward", Level = 2 });

await containerMapper.SaveAsync(collection, loadedUserData.Tracker, "player:1");
loadedUserData.ClearTrackerDeep();
```

MongoDB container 저장 흐름에서는 `SetDefaultTrackerDeep()`을 피합니다. 생성된 container tracker가 하위 tracker를 소유하므로 deep variant를 호출하면 container tracker가 변경 수집에 쓰는 하위 tracker가 교체될 수 있습니다.

저장하지 않을 container 프로퍼티에는 `[TrackableProperty("mongodb.ignore")]`를 붙입니다.

## 삭제

```csharp
var deletedCount = await mapper.DeleteAsync(collection, "player:1");
```

## Integration Test 설정

MongoDB integration test는 `MONGODB_CONNECTION_STRING`을 먼저 환경변수에서 읽고, 없으면 `.env`에서 읽습니다. 값이 없으면 SSH tunnel/local `mongodb://localhost:27017`로 fallback하며, 임시 `trackable_test_<guid>` 데이터베이스를 만들고 cleanup 시 drop합니다.

## 주의할 점

- mapper는 생성 시 필요한 BSON class map을 등록합니다.
- MongoDB 플러그인은 `BsonDocument` 컬렉션을 대상으로 합니다.
- Collection value는 MongoDB BSON serializer를 통해 직렬화됩니다. Null 값은 BSON null로 저장됩니다.
- `SaveAsync`는 tracker가 가진 변경분만 저장합니다. 전체 교체가 필요하면 다시 `CreateAsync` 흐름을 사용하거나 별도 MongoDB API를 사용합니다.
- List는 index 기반 변경을 저장하므로 같은 문서를 여러 writer가 동시에 수정하는 경우 충돌 정책을 별도로 정해야 합니다.
