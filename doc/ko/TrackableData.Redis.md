# TrackableData.Redis 사용법

`TrackableDataV2.Redis`는 Redis Stack의 RedisJSON 기능을 사용해 TrackableData 객체를 JSON 문서로 저장합니다.

## 설치

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <PackageReference Include="TrackableDataV2.Redis" Version="1.0.0" />
  <PackageReference Include="NRedisStack" Version="1.3.0" />
</ItemGroup>
```

## Redis 준비

RedisJSON 모듈이 포함된 Redis Stack이 필요합니다.

```bash
docker run --rm -p 6379:6379 redis/redis-stack-server:latest
```

## 기본 연결

```csharp
using StackExchange.Redis;
using TrackableData;
using TrackableData.Redis;

var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
var db = multiplexer.GetDatabase();
```

## POCO 저장과 로드

```csharp
public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}

var mapper = new TrackablePocoRedisMapper<IPlayer>();
RedisKey key = "player:1";

var player = new TrackablePlayer
{
    Name = "Alice",
    Level = 1,
    Gold = 100
};

await mapper.CreateAsync(db, player, key);

var loaded = await mapper.LoadAsync(db, key);
Console.WriteLine(loaded.Name);
```

## 변경분만 저장

```csharp
player.SetDefaultTrackerDeep();

player.Level = 2;
player.Gold = 150;

await mapper.SaveAsync(
    db,
    (TrackablePocoTracker<IPlayer>)player.Tracker,
    key);

player.ClearTrackerDeep();
```

## Dictionary/List/Set 저장

```csharp
var dictMapper = new TrackableDictionaryRedisMapper<int, string>();
var listMapper = new TrackableListRedisMapper<string>();
var setMapper = new TrackableSetRedisMapper<int>();

var inventory = new TrackableDictionary<int, string> { { 1001, "Sword" } };
var logs = new TrackableList<string> { "login", "quest" };
var achievements = new TrackableSet<int> { 100, 200 };

await dictMapper.CreateAsync(db, inventory, "player:1:inventory");
await listMapper.CreateAsync(db, logs, "player:1:logs");
await setMapper.CreateAsync(db, achievements, "player:1:achievements");
```

## Collection 변경 저장

```csharp
inventory.SetDefaultTrackerDeep();

inventory[1001] = "Long Sword";
inventory.Add(1002, "Potion");

await dictMapper.SaveAsync(
    db,
    (TrackableDictionaryTracker<int, string>)inventory.Tracker,
    "player:1:inventory");

inventory.ClearTrackerDeep();
```

## Container 저장

Container mapper는 각 프로퍼티를 base key 뒤에 프로퍼티 이름 suffix를 붙인 별도 Redis key로 저장합니다.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<int, string> Inventory { get; set; }
    TrackableList<string> Logs { get; set; }
    TrackableSet<int> Achievements { get; set; }
}

var mapper = new TrackableContainerRedisMapper<IUserData>();

var userData = new TrackableUserData();
userData.Inventory[1001] = "Sword";
userData.Logs.Add("login");
userData.Achievements.Add(200);

await mapper.CreateAsync(db, userData, "user:1");

var loaded = await mapper.LoadAsync(db, "user:1");

loaded.SetDefaultTrackerDeep();
loaded.Inventory[1001] = "Long Sword";
loaded.Logs.Add("quest");

await mapper.SaveAsync(db, loaded.Tracker, "user:1");
```

## 삭제

```csharp
var deleted = await mapper.DeleteAsync(db, "player:1");
```

Container 삭제는 base key에 연결된 프로퍼티별 key를 삭제합니다.

```csharp
var deletedCount = await new TrackableContainerRedisMapper<IUserData>()
    .DeleteAsync(db, "user:1");
```

## 주의할 점

- Redis Stack RedisJSON 모듈이 없으면 JSON 명령이 실패합니다.
- `CreateAsync`는 전체 JSON 값을 저장하고, `SaveAsync`는 tracker 변경분만 JSON path에 반영합니다.
- 같은 Redis key를 여러 writer가 동시에 수정하면 마지막 write가 이길 수 있습니다. 필요하면 분산 lock이나 optimistic concurrency를 별도로 적용합니다.
- 저장 후 같은 tracker를 다시 쓰지 않으려면 `ClearTrackerDeep()`을 호출합니다.
