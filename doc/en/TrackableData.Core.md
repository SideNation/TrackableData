# TrackableData.Core Usage

`TrackableDataV2.Core` provides the base change-tracking types and trackers. You can use it without a storage plugin to record changes in memory, apply them to another object, or roll them back.

## Installation

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Define a Trackable POCO

```csharp
using TrackableData;

public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}
```

The build generates a `TrackablePlayer` class.

```csharp
var player = new TrackablePlayer
{
    Name = "Alice",
    Level = 1,
    Gold = 100
};

player.SetDefaultTrackerDeep();

player.Level = 5;
player.Gold = 250;

var tracker = (TrackablePocoTracker<IPlayer>)player.Tracker;
foreach (var change in tracker.ChangeMap)
{
    Console.WriteLine($"{change.Key.Name}: {change.Value.OldValue} -> {change.Value.NewValue}");
}
```

## Dictionary Tracking

```csharp
var inventory = new TrackableDictionary<string, int>
{
    { "sword", 1 },
    { "potion", 5 }
};

inventory.SetDefaultTrackerDeep();

inventory.Add("shield", 1);
inventory["potion"] = 3;
inventory.Remove("sword");

var tracker = (TrackableDictionaryTracker<string, int>)inventory.Tracker;
foreach (var change in tracker.ChangeMap)
{
    Console.WriteLine($"{change.Key}: {change.Value.Operation}");
}
```

## List Tracking

```csharp
var log = new TrackableList<string> { "A", "B" };

log.SetDefaultTrackerDeep();

log.Add("C");
log[0] = "A edited";
log.RemoveAt(1);

var tracker = (TrackableListTracker<string>)log.Tracker;
foreach (var change in tracker.ChangeList)
{
    Console.WriteLine($"{change.Operation}: {change.Index}");
}
```

## Set Tracking

```csharp
var tags = new TrackableSet<string> { "vip", "beta" };

tags.SetDefaultTrackerDeep();

tags.Add("early-access");
tags.Remove("beta");

var tracker = (TrackableSetTracker<string>)tags.Tracker;
foreach (var change in tracker.ChangeMap)
{
    Console.WriteLine($"{change.Value}: {change.Key}");
}
```

## Class Values in Collections

`TrackableDictionary`, `TrackableList`, and `TrackableSet` can hold class values as well as primitive values.

```csharp
using TrackableData;

public sealed class ItemValue
{
    public string Name { get; set; }
    public int Level { get; set; }
}

var items = new TrackableDictionary<int, ItemValue>
{
    { 1, new ItemValue { Name = "Sword", Level = 10 } }
};

items.SetDefaultTrackerDeep();
items[1] = new ItemValue { Name = "Long Sword", Level = 12 };
items.Add(2, new ItemValue { Name = "Potion", Level = 1 });
```

Collection trackers record add, remove, and replace operations for class values. Mutating a plain class instance in place, such as `items[1].Level = 12`, is not a collection change. Replace the value or model the nested data as a trackable POCO when you need field-level tracking.

## Container Usage

Use `ITrackableContainer<T>` to group multiple trackable values into one object.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<string, int> Inventory { get; set; }
    TrackableSet<string> Achievements { get; set; }
}

var userData = new TrackableUserData();
userData.SetDefaultTrackerDeep();

userData.Inventory["sword"] = 1;
userData.Achievements.Add("first-login");
```

`SetDefaultTrackerDeep()` sets trackers on the container and all child trackables.

## Apply and Roll Back Changes

```csharp
var source = new TrackablePlayer { Name = "Alice", Level = 1, Gold = 100 };
var target = new TrackablePlayer { Name = "Alice", Level = 1, Gold = 100 };

source.SetDefaultTrackerDeep();
source.Level = 10;

source.Tracker.ApplyTo(target);
Console.WriteLine(target.Level); // 10

source.RollbackDeep();
Console.WriteLine(source.Level); // 1
```

## Common Extension Methods

| Method | Description |
|--------|-------------|
| `SetDefaultTracker()` | Sets the default tracker on the current object. |
| `SetDefaultTrackerDeep()` | Sets trackers on the current object and all child trackables. |
| `ClearTrackerDeep()` | Clears change records on the current object and all child trackers. |
| `Rollback()` | Rolls back the current object using its tracker. |
| `RollbackDeep()` | Rolls back the current object and all child changes. |
| `GetChangedTrackersWithPath()` | Returns changed child trackers with their paths. |

## Tips

- Fill initial values before calling `SetDefaultTrackerDeep()` so initialization is not recorded as a change.
- After save or synchronization, call `ClearTrackerDeep()` to avoid saving the same changes again.
- `TrackableDictionary`, `TrackableList`, and `TrackableSet` follow `IDictionary`, `IList`, and `ISet` style APIs and can store class values.
