# TrackableData.Json Usage

`TrackableDataV2.Json` provides Newtonsoft.Json converters for TrackableData tracker types and helpers for path-based JSON change patches.

## Installation

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Json" Version="1.0.0" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.2" />
</ItemGroup>
```

## Basic Setup

Use `TrackableJsonSerializerSettings.Create()` when serializing or deserializing trackers.

```csharp
using Newtonsoft.Json;
using TrackableData.Json;

var settings = TrackableJsonSerializerSettings.Create();
```

The settings register `TrackerJsonConverter`, which dispatches to the concrete POCO, Dictionary, List, Set, or Container tracker converter.

## Serialize a Tracker

```csharp
using Newtonsoft.Json;
using TrackableData;
using TrackableData.Json;

var inventory = new TrackableDictionary<string, int>
{
    { "sword", 1 },
    { "potion", 5 }
};

inventory.SetDefaultTrackerDeep();
inventory["potion"] = 3;
inventory.Add("shield", 1);
inventory.Remove("sword");

var settings = TrackableJsonSerializerSettings.Create();
var tracker = (TrackableDictionaryTracker<string, int>)inventory.Tracker;
var json = JsonConvert.SerializeObject(tracker, settings);

var restoredTracker = JsonConvert.DeserializeObject<TrackableDictionaryTracker<string, int>>(json, settings);

var target = new TrackableDictionary<string, int>
{
    { "sword", 1 },
    { "potion", 5 }
};

restoredTracker.ApplyTo(target);
```

## Path-Based JSON Patch

Use path-based helpers when you want to send the changed tracker for a root `ITrackable` as JSON.

```csharp
var patchJson = inventory.SerializeChangedTrackersWithPath();

var target = new TrackableDictionary<string, int>
{
    { "sword", 1 },
    { "potion", 5 }
};

patchJson.ApplyTo(target);
```

## Supported Trackers

| Tracker | JSON shape |
|---------|------------|
| `TrackablePocoTracker<T>` | Object with changed property names |
| `TrackableDictionaryTracker<TKey, TValue>` | Object with `+`, `-`, `=` operation-prefixed keys |
| `TrackableListTracker<T>` | Array of operation entries |
| `TrackableSetTracker<T>` | Object with `+` and `-` arrays |
| `IContainerTracker<T>` generated tracker | Object with changed child tracker properties |

## Notes

- The JSON format is optimized for applying changes to another trackable object.
- Remove operations do not need old values when applying changes to a target object.
- For generated containers, serialize the container tracker directly when the child trackers are already connected through the container tracker.
- Use MemoryPack when you need binary serialization or explicit formatter registration for AOT scenarios.
