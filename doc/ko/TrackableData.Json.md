# TrackableData.Json 사용법

`TrackableDataV2.Json`은 TrackableData tracker 타입을 Newtonsoft.Json으로 직렬화하는 converter와 경로 기반 JSON 변경 patch 헬퍼를 제공합니다.

## 설치

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Json" Version="1.0.0" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.2" />
</ItemGroup>
```

## 기본 설정

Tracker를 직렬화하거나 역직렬화할 때 `TrackableJsonSerializerSettings.Create()`를 사용합니다.

```csharp
using Newtonsoft.Json;
using TrackableData.Json;

var settings = TrackableJsonSerializerSettings.Create();
```

이 설정은 `TrackerJsonConverter`를 등록합니다. 이 converter는 실제 tracker 타입에 맞춰 POCO, Dictionary, List, Set, Container converter로 위임합니다.

## AOT / Unity IL2CPP 설정

`TrackableJsonSerializerSettings.Create()`는 일반 런타임에서 편리하게 쓰는 설정입니다. `TrackerJsonConverter`가 런타임 tracker 타입을 reflection으로 확인해 알맞은 converter로 위임합니다. Unity IL2CPP나 다른 AOT 런타임에서는 `CreateForAot()`를 사용하고 직렬화할 구체 tracker converter를 모두 등록합니다.

```csharp
using Newtonsoft.Json;
using TrackableData.Json;

var settings = TrackableJsonSerializerSettings.CreateForAot()
    .AddPocoTrackerConverter<IPlayer>()
    .AddDictionaryTrackerConverter<int, string>()
    .AddListTrackerConverter<string>()
    .AddSetTrackerConverter<int>()
    .AddContainerTrackerConverter();
```

AOT 경로에서 JSON patch helper를 사용할 때도 같은 settings를 넘깁니다.

## Tracker 직렬화

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

## 경로 기반 JSON Patch

루트 `ITrackable`의 변경 tracker를 JSON으로 보내고 싶을 때 경로 기반 헬퍼를 사용합니다.

```csharp
var patchJson = inventory.SerializeChangedTrackersWithPath();

var target = new TrackableDictionary<string, int>
{
    { "sword", 1 },
    { "potion", 5 }
};

patchJson.ApplyTo(target);
```

## Class Value

Tracker JSON에는 POCO 프로퍼티와 Dictionary/List/Set/Container tracker의 class 값을 포함할 수 있습니다. 값은 Newtonsoft.Json으로 직렬화되므로 class에 별도 JSON 동작이 필요하면 일반 Newtonsoft.Json attribute나 converter를 사용합니다.

```csharp
using Newtonsoft.Json;
using TrackableData;
using TrackableData.Json;

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

var settings = TrackableJsonSerializerSettings.Create();
var json = JsonConvert.SerializeObject(items.Tracker, settings);
var tracker = JsonConvert.DeserializeObject<TrackableDictionaryTracker<int, ItemValue>>(json, settings);
```

Collection 안의 class 값은 값을 교체해야 변경으로 기록됩니다. Plain class 인스턴스 내부를 직접 바꾸는 것은 중첩 데이터가 trackable 객체로 모델링되어 있지 않으면 추적되지 않습니다.

## 생성된 Container Tracker

생성된 container는 container tracker를 통해 하위 tracker를 연결합니다. Container tracker 자체를 직렬화하려면 변경 전에 생성된 tracker를 container에 할당합니다.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<string, int> Inventory { get; set; }
    TrackableSet<string> Achievements { get; set; }
}

var data = new TrackableUserData();
data.Tracker = new TrackableUserDataTracker();

data.Inventory["sword"] = 2;
data.Achievements.Add("first-login");

var settings = TrackableJsonSerializerSettings.Create();
var json = JsonConvert.SerializeObject(data.Tracker, settings);
var tracker = JsonConvert.DeserializeObject<TrackableUserDataTracker>(json, settings);

var target = new TrackableUserData();
tracker.ApplyTo((IUserData)target);
```

## 지원 Tracker

| Tracker | JSON 형태 |
|---------|-----------|
| `TrackablePocoTracker<T>` | 변경된 프로퍼티 이름을 가진 object |
| `TrackableDictionaryTracker<TKey, TValue>` | `+`, `-`, `=` operation prefix가 붙은 key object |
| `TrackableListTracker<T>` | operation entry 배열 |
| `TrackableSetTracker<T>` | `+`, `-` 배열을 가진 object |
| 생성된 `IContainerTracker<T>` tracker | 변경된 하위 tracker 프로퍼티 object |

## 주의할 점

- JSON 형식은 변경분을 다른 trackable 객체에 적용하는 용도에 맞춰져 있습니다.
- Newtonsoft.Json이 직렬화/역직렬화할 수 있는 class 타입은 class value로 사용할 수 있습니다.
- 변경 적용만 할 때는 remove operation에 old value가 필요하지 않습니다.
- 생성된 container는 하위 tracker가 container tracker에 연결되어 있다면 container tracker를 직접 직렬화합니다.
- Unity IL2CPP/AOT 환경에서는 `CreateForAot()`와 명시적 converter 등록을 사용합니다. 바이너리 직렬화가 필요하면 MemoryPack 플러그인을 사용합니다.
