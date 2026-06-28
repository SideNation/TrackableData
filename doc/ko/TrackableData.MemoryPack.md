# TrackableData.MemoryPack 사용법

`TrackableDataV2.MemoryPack`은 `TrackableDictionary`, `TrackableList`, `TrackableSet`과 각 tracker를 MemoryPack으로 직렬화하기 위한 formatter를 제공합니다.

## 설치

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.MemoryPack" Version="1.0.0" />
  <PackageReference Include="MemoryPack" Version="1.21.4" />
</ItemGroup>
```

## 기본 설정

앱 시작 시 사용할 구체 타입별 formatter를 등록합니다.

```csharp
using TrackableData.MemoryPack;

TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, string>();
TrackableDataFormatterInitializer.RegisterListFormatter<string>();
TrackableDataFormatterInitializer.RegisterSetFormatter<int>();
```

등록 메서드는 collection과 tracker formatter를 함께 등록합니다.

## Dictionary 직렬화

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

## Tracker 직렬화

변경분만 네트워크로 보내거나 저장하고 싶을 때 tracker를 직렬화합니다.

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

## 지원 타입

| 등록 메서드 | 지원 타입 |
|-------------|-----------|
| `RegisterDictionaryFormatter<TKey, TValue>()` | `TrackableDictionary<TKey, TValue>`, `TrackableDictionaryTracker<TKey, TValue>` |
| `RegisterListFormatter<T>()` | `TrackableList<T>`, `TrackableListTracker<T>` |
| `RegisterSetFormatter<T>()` | `TrackableSet<T>`, `TrackableSetTracker<T>` |

## 주의할 점

- formatter 등록은 사용할 generic 조합마다 필요합니다.
- POCO 자체 formatter는 이 플러그인에서 자동 등록하지 않습니다. POCO 직렬화가 필요하면 일반 MemoryPack 설정을 함께 사용합니다.
- AOT 환경을 고려해 구체 타입을 명시적으로 등록하는 방식입니다.
