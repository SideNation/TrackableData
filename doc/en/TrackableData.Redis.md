# TrackableData.Redis Usage

`TrackableDataV2.Redis` stores TrackableData objects as JSON documents using Redis Stack RedisJSON.

## Installation

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

## Prepare Redis

Redis Stack with the RedisJSON module is required.

```bash
docker run --rm -p 6379:6379 redis/redis-stack-server:latest
```

## Basic Connection

```csharp
using StackExchange.Redis;
using TrackableData;
using TrackableData.Redis;

var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
var db = multiplexer.GetDatabase();
```

## Save and Load a POCO

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

## Save Only Changes

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

## Save Dictionary/List/Set

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

## Save Collection Changes

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

## Save a Container

The container mapper stores each property as a separate Redis key by appending a property-name suffix to the base key.

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

Use `SetDefaultTracker()` for loaded containers. The generated container tracker owns the child trackers used by `TrackableContainerRedisMapper<T>.SaveAsync`; calling `SetDefaultTrackerDeep()` can replace those child trackers.

POCO properties can use `redis.field:` and `redis.ignore`. Container properties can use `redis.keysuffix:` and `redis.ignore`.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    [TrackableProperty("redis.keysuffix:bag")]
    TrackableDictionary<int, string> Inventory { get; set; }

    [TrackableProperty("redis.ignore")]
    TrackableSet<int> RuntimeOnlyAchievements { get; set; }
}
```

## Delete

```csharp
var deleted = await mapper.DeleteAsync(db, "player:1");
```

Container delete removes the keys for properties under the base key.

```csharp
var deletedCount = await new TrackableContainerRedisMapper<IUserData>()
    .DeleteAsync(db, "user:1");
```

## JSON Serialization Options

When a mapper is created without `JsonSerializerOptions`, Redis uses the built-in default options.

The default options:

- Store compact JSON with `WriteIndented = false`.
- Use `JavaScriptEncoder.Create(UnicodeRanges.All)`, so Unicode text such as Korean or Japanese is stored as readable text instead of `\uXXXX` escapes.

Pass your own `JsonSerializerOptions` when you need different enum, naming, converter, or encoder behavior.

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

## Integration Test Configuration

Redis integration tests read `REDIS_CONNECTION_STRING` from the environment first, then from `.env`. Plain StackExchange.Redis connection strings and `redis://`/`rediss://` URLs are supported. If the value is not present, the fixture falls back to an SSH tunnel/local `localhost:6379` and cleans keys under a temporary `test:<guid>:` prefix.

## Notes

- JSON commands fail if Redis Stack RedisJSON is not available.
- `CreateAsync` stores the full JSON value, while `SaveAsync` applies tracker changes to JSON paths.
- If multiple writers modify the same Redis key concurrently, the last write can win. Add distributed locking or optimistic concurrency when needed.
- After saving, call `ClearTrackerDeep()` if you do not want to reuse the same tracker changes.
