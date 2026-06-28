# TrackableData.Core 사용법

`TrackableDataV2.Core`는 변경 추적의 기본 타입과 tracker를 제공합니다. 저장소 플러그인을 쓰지 않아도 메모리 안에서 변경 내용을 기록하고, 다른 객체에 적용하거나 롤백할 수 있습니다.

## 설치

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Trackable POCO 정의

```csharp
using TrackableData;

public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}
```

빌드 시 `TrackablePlayer` 클래스가 생성됩니다.

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

## Dictionary 변경 추적

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

## List 변경 추적

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

## Set 변경 추적

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

## Container 사용

여러 trackable 값을 한 객체로 묶을 때는 `ITrackableContainer<T>`를 사용합니다.

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

`SetDefaultTrackerDeep()`은 container와 자식 trackable에 tracker를 함께 설정합니다.

## 변경 적용과 롤백

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

## 자주 쓰는 확장 메서드

| 메서드 | 설명 |
|--------|------|
| `SetDefaultTracker()` | 현재 객체에 기본 tracker를 설정합니다. |
| `SetDefaultTrackerDeep()` | 현재 객체와 모든 자식 trackable에 tracker를 설정합니다. |
| `ClearTrackerDeep()` | 현재 객체와 모든 자식 tracker의 변경 기록을 지웁니다. |
| `Rollback()` | 현재 객체의 변경을 tracker 기준으로 되돌립니다. |
| `RollbackDeep()` | 현재 객체와 모든 자식 변경을 되돌립니다. |
| `GetChangedTrackersWithPath()` | 변경된 자식 tracker를 경로와 함께 반환합니다. |

## 사용 팁

- 초기값을 모두 채운 뒤 `SetDefaultTrackerDeep()`을 호출해야 초기 세팅이 변경으로 기록되지 않습니다.
- 저장이나 동기화가 끝난 뒤에는 `ClearTrackerDeep()`을 호출해 같은 변경이 다시 저장되지 않게 합니다.
- `TrackableDictionary`, `TrackableList`, `TrackableSet`은 각각 `IDictionary`, `IList`, `ISet` 스타일 API를 따릅니다.
