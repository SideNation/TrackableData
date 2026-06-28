# TrackableData Usage Documentation

This folder documents how to use TrackableData V2 by plugin.

## Documents

| Document | Contents |
|----------|----------|
| [TrackableData.Core.md](TrackableData.Core.md) | Basic setup and POCO/Dictionary/List/Set change tracking |
| [TrackableData.Generator.md](TrackableData.Generator.md) | Source Generator setup and generation rules |
| [TrackableData.Json.md](TrackableData.Json.md) | Newtonsoft.Json tracker serialization and JSON patch usage |
| [TrackableData.MemoryPack.md](TrackableData.MemoryPack.md) | MemoryPack formatter registration and usage |
| [TrackableData.MongoDB.md](TrackableData.MongoDB.md) | MongoDB create/load/partial save and BSON value mapping |
| [TrackableData.PostgreSql.md](TrackableData.PostgreSql.md) | PostgreSQL table mapping and persistence |
| [TrackableData.Redis.md](TrackableData.Redis.md) | Redis Stack RedisJSON create/load/partial save and JSON options |

## Basic Package Setup

The smallest setup uses `Core` and `Generator`.

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

Add storage or serialization plugins only when needed.

```xml
<PackageReference Include="TrackableDataV2.MemoryPack" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.Json" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.MongoDB" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.PostgreSql" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.Redis" Version="1.0.0" />
```

## Common Workflow

1. Define an `ITrackablePoco<T>` or `ITrackableContainer<T>` interface.
2. `TrackableDataV2.Generator` generates the `Trackable...` implementation class.
3. Call `SetDefaultTrackerDeep()` before recording changes.
4. Modify the object or collection.
5. Inspect `Tracker` or pass it to a storage plugin to save only the changes.

```csharp
using TrackableData;

public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}

var player = new TrackablePlayer
{
    Name = "Alice",
    Level = 1,
    Gold = 100
};

player.SetDefaultTrackerDeep();

player.Level = 2;
player.Gold = 150;

var tracker = (TrackablePocoTracker<IPlayer>)player.Tracker;
Console.WriteLine(tracker.HasChange);
```

## Notes

- Changes are not recorded without a `Tracker`. Call `SetDefaultTrackerDeep()` before modifying values.
- Storage plugin `SaveAsync` methods take a tracker and apply only recorded changes.
- After a successful save, call `ClearTrackerDeep()` if you will keep using the same object.
- Generated class names remove the leading `I` from the interface name and add `Trackable`. Example: `IPlayer` -> `TrackablePlayer`.
