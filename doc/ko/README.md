# TrackableData 사용법 문서

이 폴더는 TrackableData V2 패키지를 플러그인별로 사용하는 방법을 정리합니다.

## 문서 목록

| 문서 | 내용 |
|------|------|
| [TrackableData.Core.md](TrackableData.Core.md) | 기본 설정, POCO/Dictionary/List/Set 변경 추적 |
| [TrackableData.Generator.md](TrackableData.Generator.md) | Source Generator 설정과 생성 규칙 |
| [TrackableData.MemoryPack.md](TrackableData.MemoryPack.md) | MemoryPack 직렬화 formatter 등록과 사용 |
| [TrackableData.MongoDB.md](TrackableData.MongoDB.md) | MongoDB 저장/로드/부분 저장 |
| [TrackableData.PostgreSql.md](TrackableData.PostgreSql.md) | PostgreSQL 테이블 매핑과 저장/로드 |
| [TrackableData.Redis.md](TrackableData.Redis.md) | Redis Stack RedisJSON 저장/로드/부분 저장 |

## 기본 패키지 구성

가장 작은 구성은 `Core`와 `Generator`입니다.

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

저장소나 직렬화가 필요할 때만 플러그인을 추가합니다.

```xml
<PackageReference Include="TrackableDataV2.MemoryPack" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.MongoDB" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.PostgreSql" Version="1.0.0" />
<PackageReference Include="TrackableDataV2.Redis" Version="1.0.0" />
```

## 공통 사용 흐름

1. `ITrackablePoco<T>` 또는 `ITrackableContainer<T>` 인터페이스를 정의합니다.
2. `TrackableDataV2.Generator`가 `Trackable...` 클래스를 생성합니다.
3. 변경을 기록하기 전에 `SetDefaultTrackerDeep()`을 호출합니다.
4. 객체나 컬렉션을 수정합니다.
5. `Tracker`를 조회하거나 저장 플러그인에 넘겨 변경분만 저장합니다.

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

## 주의할 점

- `Tracker`가 없으면 변경이 기록되지 않습니다. 변경 전 `SetDefaultTrackerDeep()`을 호출합니다.
- 저장 플러그인의 `SaveAsync`는 전체 객체가 아니라 tracker를 받아 변경분만 반영합니다.
- 저장 성공 후 같은 객체를 계속 사용할 경우 `ClearTrackerDeep()`으로 저장된 변경 기록을 지웁니다.
- Source Generator가 만든 클래스 이름은 인터페이스 이름의 앞 `I`를 제거하고 `Trackable`을 붙인 형태입니다. 예: `IPlayer` -> `TrackablePlayer`.
