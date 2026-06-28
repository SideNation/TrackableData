# TrackableData.Generator Usage

`TrackableDataV2.Generator` is a Roslyn Source Generator. It finds interfaces that implement `ITrackablePoco<T>` or `ITrackableContainer<T>` and generates `Trackable...` implementation classes at compile time.

## Installation

Reference the generator as an analyzer.

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

When referencing the project directly, also reference it as an analyzer.

```xml
<ProjectReference Include="..\TrackableData.Generator\TrackableData.Generator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## Generate a POCO

```csharp
using TrackableData;

public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}
```

The build generates `TrackablePlayer`.

```csharp
var player = new TrackablePlayer
{
    Name = "Alice",
    Level = 1,
    Gold = 100
};

player.SetDefaultTrackerDeep();

player.Level = 2;

var tracker = (TrackablePocoTracker<IPlayer>)player.Tracker;
Console.WriteLine(tracker.HasChange);
```

## Generate a Container

```csharp
using TrackableData;

public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<int, string> Inventory { get; set; }
    TrackableList<string> Logs { get; set; }
    TrackableSet<int> Achievements { get; set; }
}
```

The build generates `TrackableUserData` and `TrackableUserDataTracker`.

```csharp
var userData = new TrackableUserData();
userData.SetDefaultTrackerDeep();

userData.Inventory[1001] = "Sword";
userData.Logs.Add("login");
userData.Achievements.Add(200);
```

## Naming Rule

| Interface | Generated class |
|-----------|-----------------|
| `IPlayer` | `TrackablePlayer` |
| `IUserData` | `TrackableUserData` |
| `IDataContainer` | `TrackableDataContainer` |

The generator removes the leading `I` from the interface name and prefixes `Trackable`.

## Authoring Rules

- Use `ITrackablePoco<T>` and `ITrackableContainer<T>` on interfaces.
- Tracked properties must have both a getter and a setter.
- Container properties can use `TrackableDictionary`, `TrackableList`, `TrackableSet`, generated trackable POCOs, and other supported trackable types.
- Generated classes are emitted into the same namespace as the source interface.

## Diagnostics

| Rule ID | Meaning |
|---------|---------|
| `TRACK001` | The trackable type must be partial. |
| `TRACK002` | The `ITrackablePoco<T>` target must be an interface. |
| `TRACK003` | The `ITrackableContainer<T>` target must be an interface. |
| `TRACK004` | The property must have both getter and setter. |
