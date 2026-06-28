# TrackableData.MemoryPack Usage

`TrackableDataV2.MemoryPack` provides MemoryPack formatters for trackable POCO, Container, Dictionary, List, Set values and their trackers.

## Installation

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.MemoryPack" Version="1.0.0" />
  <PackageReference Include="MemoryPack" Version="1.21.4" />
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

Each registration method registers both the value formatter and the tracker formatter.

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

## Serialize Class Values

Class values are supported when the class type is serializable by MemoryPack. Register the trackable generic combination that contains the class value.

```csharp
using MemoryPack;
using TrackableData;
using TrackableData.MemoryPack;

[MemoryPackable]
public partial class ItemValue
{
    public string Name { get; set; }
    public int Level { get; set; }
}

TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, ItemValue>();

var items = new TrackableDictionary<int, ItemValue>
{
    { 1, new ItemValue { Name = "Sword", Level = 10 } }
};

var bytes = MemoryPackSerializer.Serialize(items);
var restored = MemoryPackSerializer.Deserialize<TrackableDictionary<int, ItemValue>>(bytes);

Console.WriteLine(restored[1].Name); // Sword
```

The same rule applies to class-valued POCO properties, List/Set values, and container members. Register the POCO/container formatter and every member formatter combination you serialize.

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
| `RegisterPocoFormatter<T>()` | `ITrackablePoco<T>` generated value, `TrackablePocoTracker<T>` |
| `RegisterContainerFormatter<T>()` | `ITrackableContainer<T>` generated value, `IContainerTracker<T>` |

## Notes

- Register every generic type combination you use.
- Class value types must be serializable by MemoryPack, for example with `[MemoryPackable]`.
- Container formatters do not replace member formatter registration. Register both the container and the concrete member types used inside it.
- Registrations are explicit with concrete types for AOT-friendly usage.
