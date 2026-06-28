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
  <PackageReference Include="NRedisStack" Version="1.6.0" />
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

loaded.SetDefaultTracker();
loaded.Inventory[1001] = "Long Sword";
loaded.Logs.Add("quest");

await mapper.SaveAsync(db, loaded.Tracker, "user:1");
```

로드한 container에는 `SetDefaultTracker()`를 사용합니다. 생성된 container tracker가 `TrackableContainerRedisMapper<T>.SaveAsync`에서 사용하는 하위 tracker를 소유하므로 `SetDefaultTrackerDeep()`을 호출하면 그 하위 tracker가 교체될 수 있습니다.

POCO 프로퍼티에는 `redis.field:`와 `redis.ignore`를 사용할 수 있습니다. Container 프로퍼티에는 `redis.keysuffix:`와 `redis.ignore`를 사용할 수 있습니다.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    [TrackableProperty("redis.keysuffix:bag")]
    TrackableDictionary<int, string> Inventory { get; set; }

    [TrackableProperty("redis.ignore")]
    TrackableSet<int> RuntimeOnlyAchievements { get; set; }
}
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

## JSON 직렬화 옵션

Mapper를 `JsonSerializerOptions` 없이 생성하면 Redis는 내장 기본 옵션을 사용합니다.

기본 옵션은 다음과 같습니다.

- `WriteIndented = false`로 compact JSON을 저장합니다.
- `JavaScriptEncoder.Create(UnicodeRanges.All)`을 사용해 한글이나 일본어 같은 Unicode 문자를 `\uXXXX` escape가 아닌 읽을 수 있는 문자로 저장합니다.

Enum, naming, converter, encoder 동작을 바꿔야 하면 직접 `JsonSerializerOptions`를 넘깁니다.

```csharp
using System.Text.Json;
using TrackableData;
using TrackableData.Redis;

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var mapper = new TrackablePocoRedisMapper<IPlayer>(
    NullTrackableLogger.Instance,
    options);
```

## Integration Test 설정

Redis integration test는 `REDIS_CONNECTION_STRING`을 먼저 환경변수에서 읽고, 없으면 `.env`에서 읽습니다. 일반 StackExchange.Redis connection string과 `redis://`/`rediss://` URL을 지원합니다. 값이 없으면 SSH tunnel/local `localhost:6379`로 fallback하며, 임시 `test:<guid>:` prefix 아래의 key를 cleanup합니다.

## 주의할 점

- Redis Stack RedisJSON 모듈이 없으면 JSON 명령이 실패합니다.
- `CreateAsync`는 전체 JSON 값을 저장하고, `SaveAsync`는 tracker 변경분만 JSON path에 반영합니다.
- 같은 Redis key를 여러 writer가 동시에 수정하면 마지막 write가 이길 수 있습니다. 필요하면 분산 lock이나 optimistic concurrency를 별도로 적용합니다.
- 저장 후 같은 tracker를 다시 쓰지 않으려면 `ClearTrackerDeep()`을 호출합니다.
