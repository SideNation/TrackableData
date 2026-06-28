# TrackableData V2

Lightweight change-tracking library for .NET objects and collections.

Property-level change tracking for POCO, Dictionary, List, Set with storage plugins for MongoDB, PostgreSQL, Redis.

## Features

- **Trackable/Tracker separation** - Trackable objects hold state, Trackers record changes
- **Roslyn Source Generator** - No manual boilerplate, IDE IntelliSense support
- **Storage Plugins** - MongoDB, PostgreSQL, Redis (RedisJSON)
- **Serialization** - Newtonsoft.Json support for JSON patches and MemoryPack support for high-performance binary serialization
- **ILogger** - Microsoft.Extensions.Logging integration throughout
- **Unity compatible** - Core, MemoryPack, Generator target netstandard2.1 with C# 9.0

## NuGet Packages

| Package | Description |
|---------|-------------|
| `TrackableDataV2.Core` | Core interfaces and trackable collections |
| `TrackableDataV2.Generator` | Roslyn Source Generator for trackable POCO/Container |
| `TrackableDataV2.Json` | Newtonsoft.Json tracker serialization and JSON patch helpers |
| `TrackableDataV2.MemoryPack` | MemoryPack serialization formatters |
| `TrackableDataV2.MongoDB` | MongoDB storage mapper |
| `TrackableDataV2.PostgreSql` | PostgreSQL storage mapper |
| `TrackableDataV2.Redis` | Redis Stack (RedisJSON) storage mapper |

## Quick Start

### 1. Install packages

```xml
<PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

### 2. Define a Trackable POCO

```csharp
using TrackableData;

public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}
```

The Source Generator automatically creates `TrackablePlayer` class with built-in change tracking.

### 3. Track changes

```csharp
var player = new TrackablePlayer { Name = "Alice", Level = 1, Gold = 100 };

// Enable tracking
player.SetDefaultTrackerDeep();

// Modify properties - tracker records automatically
player.Level = 5;
player.Gold = 250;

// Inspect changes
var tracker = (TrackablePocoTracker<IPlayer>)player.Tracker;
foreach (var change in tracker.ChangeMap)
{
    Console.WriteLine($"{change.Key.Name}: {change.Value.OldValue} -> {change.Value.NewValue}");
}
// Output:
//   Level: 1 -> 5
//   Gold: 100 -> 250
```

### 4. Trackable Collections

```csharp
// Dictionary
var inventory = new TrackableDictionary<string, int> { { "sword", 1 } };
inventory.SetDefaultTrackerDeep();
inventory.Add("shield", 1);    // tracked as Add
inventory["sword"] = 2;        // tracked as Modify
inventory.Remove("sword");     // tracked as Remove

// List
var log = new TrackableList<string> { "A", "B" };
log.SetDefaultTrackerDeep();
log.Add("C");                  // tracked as PushBack
log[0] = "A-modified";         // tracked as Modify

// Set
var tags = new TrackableSet<string> { "vip" };
tags.SetDefaultTrackerDeep();
tags.Add("beta");              // tracked as Add
tags.Remove("vip");            // tracked as Remove
```

## Storage Plugins

### MongoDB

```csharp
var mapper = new TrackablePocoMongoDbMapper<IPlayer>();

// CRUD
await mapper.CreateAsync(collection, player, "player1");
var loaded = await mapper.LoadAsync(collection, "player1");

// Save only changed fields
player.SetDefaultTrackerDeep();
player.Gold = 500;
await mapper.SaveAsync(collection, player.Tracker, "player1");
```

### PostgreSQL

```csharp
var mapper = new TrackablePocoSqlMapper<IPlayer>(
    PostgreSqlProvider.Instance, "PlayerTable");

await mapper.ResetTableAsync(connection);
await mapper.CreateAsync(connection, player);
var loaded = await mapper.LoadAsync(connection, playerId);

// Tracker-based partial UPDATE (only changed columns)
await mapper.SaveAsync(connection, tracker, playerId);
```

### Redis (RedisJSON)

Requires Redis Stack with RedisJSON module.

```csharp
var mapper = new TrackablePocoRedisMapper<IPlayer>();

await mapper.CreateAsync(db, player, "player:1");     // JSON.SET
var loaded = await mapper.LoadAsync(db, "player:1");   // JSON.GET

// Path-based partial update
player.SetDefaultTrackerDeep();
player.Gold = 500;
await mapper.SaveAsync(db, player.Tracker, "player:1"); // JSON.SET $.Gold
```

## Container (Multi-property tracking)

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<string, int> Inventory { get; set; }
    TrackableSet<string> Achievements { get; set; }
}
```

The generator creates `TrackableUserData` with per-property trackers. Container mappers handle multi-table (SQL) or multi-key (Redis) persistence automatically.

## Project Structure

```
src/
  TrackableData.Core/          net10.0 + netstandard2.1
  TrackableData.Generator/     netstandard2.1 (Roslyn Source Generator)
  TrackableData.Json/          net10.0 + netstandard2.1
  TrackableData.MemoryPack/    net10.0 + netstandard2.1
  TrackableData.MongoDB/       net10.0
  TrackableData.PostgreSql/    net10.0
  TrackableData.Redis/         net10.0
```

## Build

```bash
./build.sh              # Compile (default)
./build.sh Test          # Run unit tests
./build.sh Pack          # Create NuGet packages (auto-increments patch version)
./build.sh Push --nuget-api-key <KEY>  # Publish to NuGet (also runs Pack, auto-increments patch version)
```

### Version Management

The current version is defined in `Directory.Build.props`. Running `Pack` or `Push` without `--version` automatically increments the patch version and updates the file.

```bash
# Auto-increment patch (e.g. 1.0.1 → 1.0.2)
./build.sh Pack
./build.sh Push --nuget-api-key <KEY>

# Specify version explicitly
./build.sh Pack --version 2.0.0
./build.sh Push --nuget-api-key <KEY> --version 2.0.0
```

> **Note:** `Push` internally runs `Pack`, so running `Pack` and then `Push` separately will increment the version twice. Use `Push` directly to pack and publish in one step.

## License

MIT
