# TrackableData.MemoryPack Usage

`TrackableDataV2.MemoryPack` provides MemoryPack formatters for `TrackableDictionary`, `TrackableList`, `TrackableSet`, and their trackers.

## Installation

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.MemoryPack" Version="1.0.0" />
  <PackageReference Include="MemoryPack" Version="1.21.3" />
</ItemGroup>
```

## Basic Setup

Register formatters for each concrete type at application startup.

```csharp
using TrackableData.MemoryPack;

TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, string>();
TrackableDataFormatterInitializer.RegisterListFormatter<string>();
TrackableDataFormatterInitializer.RegisterSetFormatter<int>();
```

Each registration method registers both the collection formatter and the tracker formatter.

## Serialize a Dictionary

```csharp
using MemoryPack;
using TrackableData;
using TrackableData.MemoryPack;

TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, string>();

var inventory = new TrackableDictionary<int, string>
{
    { 1, "Sword" },
    { 2, "Potion" }
};

var bytes = MemoryPackSerializer.Serialize(inventory);
var restored = MemoryPackSerializer.Deserialize<TrackableDictionary<int, string>>(bytes);

Console.WriteLine(restored[1]); // Sword
```

## Serialize a Tracker

Serialize a tracker when you want to send or store only the changes.

```csharp
TrackableDataFormatterInitializer.RegisterListFormatter<string>();

var log = new TrackableList<string> { "A", "B" };
log.SetDefaultTrackerDeep();
log.Add("C");

var tracker = (TrackableListTracker<string>)log.Tracker;
var bytes = MemoryPackSerializer.Serialize(tracker);

var restoredTracker = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

var target = new TrackableList<string> { "A", "B" };
restoredTracker.ApplyTo(target);

Console.WriteLine(target[2]); // C
```

## Supported Types

| Registration method | Supported types |
|---------------------|-----------------|
| `RegisterDictionaryFormatter<TKey, TValue>()` | `TrackableDictionary<TKey, TValue>`, `TrackableDictionaryTracker<TKey, TValue>` |
| `RegisterListFormatter<T>()` | `TrackableList<T>`, `TrackableListTracker<T>` |
| `RegisterSetFormatter<T>()` | `TrackableSet<T>`, `TrackableSetTracker<T>` |

## Notes

- Register every generic type combination you use.
- This plugin does not automatically register POCO formatters. Use normal MemoryPack setup for POCO serialization.
- Registrations are explicit with concrete types for AOT-friendly usage.
