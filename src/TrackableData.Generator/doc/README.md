# TrackableData.Generator - Roslyn Source Generator 가이드

## 개요

`TrackableData.Generator`는 **Roslyn Incremental Source Generator**로, 사용자가 정의한 인터페이스를 분석하여 변경 추적(change tracking) 기능이 내장된 클래스를 컴파일 타임에 자동 생성하는 프로젝트입니다.

### 핵심 개념: Source Generator란?

Roslyn Source Generator는 컴파일 과정에 개입하여 **추가적인 C# 소스 코드를 자동 생성**하는 도구입니다. 런타임 리플렉션이나 수동 보일러플레이트 코드 대신, 컴파일 타임에 코드를 생성하므로 **성능과 타입 안전성**을 모두 확보할 수 있습니다.

```
[컴파일 시작]
    │
    ▼
[Roslyn이 소스 코드 파싱 → Syntax Tree 생성]
    │
    ▼
[Source Generator가 Syntax Tree를 분석]
    │
    ▼
[조건에 맞는 인터페이스 발견 시 새 소스 코드 생성]
    │
    ▼
[생성된 코드가 컴파일에 포함됨]
    │
    ▼
[최종 빌드 완료]
```

---

## 프로젝트 구조

```
TrackableData.Generator/
├── TrackableData.Generator.csproj   # 프로젝트 설정
├── TrackableDataGenerator.cs        # 진입점 - IIncrementalGenerator 구현
├── TrackablePocoEmitter.cs          # POCO 클래스 코드 생성기
├── TrackableContainerEmitter.cs     # Container 클래스 + Tracker 코드 생성기
├── EmitterHelper.cs                 # 공통 유틸리티 (프로퍼티 수집, 타입 판별 등)
├── DiagnosticDescriptors.cs         # 컴파일 에러/경고 정의
├── AnalyzerReleases.Shipped.md      # Roslyn 분석기 릴리스 추적
└── AnalyzerReleases.Unshipped.md
```

---

## Roslyn 핵심 개념 설명

이 프로젝트를 이해하려면 알아야 할 Roslyn 개념들입니다.

### Syntax Tree vs Semantic Model

| 구분 | Syntax Tree | Semantic Model |
|------|------------|----------------|
| 역할 | 소스 코드의 **구문 구조** (AST) | 코드의 **의미/타입 정보** |
| 예시 | "여기에 interface 선언이 있다" | "이 인터페이스가 ITrackablePoco<T>를 구현한다" |
| 속도 | 빠름 (텍스트 파싱만) | 느림 (타입 해석 필요) |

Source Generator에서는 **Syntax Tree로 빠르게 후보를 필터링**한 뒤, **Semantic Model로 정확한 타입 정보를 확인**하는 2단계 접근을 사용합니다.

### IIncrementalGenerator

`ISourceGenerator`의 개선 버전으로, **변경된 파일만 재처리**하는 증분(incremental) 방식입니다. 파일을 수정할 때마다 전체 프로젝트를 재분석하지 않으므로 IDE 성능이 크게 향상됩니다.

### 주요 Roslyn 타입들

| 타입 | 설명 | 이 프로젝트에서의 용도 |
|------|------|----------------------|
| `InterfaceDeclarationSyntax` | 인터페이스 선언의 구문 노드 | `IPersonData` 같은 인터페이스 선언을 찾는 데 사용 |
| `INamedTypeSymbol` | 타입의 의미 정보를 담는 심볼 | 인터페이스가 `ITrackablePoco<T>`를 구현하는지 확인 |
| `IPropertySymbol` | 프로퍼티의 의미 정보 | 인터페이스의 프로퍼티 목록을 수집 |
| `ITypeSymbol` | 타입 심볼의 기본 인터페이스 | 프로퍼티 타입이 Trackable인지 판별 |
| `SourceProductionContext` | 생성된 소스를 출력하는 컨텍스트 | `AddSource()`로 생성된 `.g.cs` 파일을 등록 |
| `GeneratorSyntaxContext` | 구문 노드 + 시맨틱 모델 | 후보 노드의 타입 정보를 조회 |

---

## 파일별 상세 설명

### 1. TrackableDataGenerator.cs (진입점)

**Roslyn에 등록되는 메인 Generator 클래스**입니다.

```csharp
[Generator(LanguageNames.CSharp)]
public class TrackableDataGenerator : IIncrementalGenerator
```

- `[Generator]` 어트리뷰트로 Roslyn에 이 클래스가 Source Generator임을 알립니다.
- `IIncrementalGenerator`를 구현하여 증분 생성을 지원합니다.

#### Initialize() 메서드 - 파이프라인 등록

`Initialize()`에서 **2개의 독립적인 파이프라인**을 등록합니다:

```
파이프라인 1: ITrackablePoco<T> 처리
─────────────────────────────────────
SyntaxProvider.CreateSyntaxProvider
    │
    ├─ predicate: InterfaceDeclarationSyntax인가?    ← 구문 필터 (빠름)
    ├─ transform: ITrackablePoco<T>를 구현하는가?     ← 시맨틱 필터 (정확)
    │
    ▼
Combine(CompilationProvider)    ← Compilation 정보 결합
    │
    ▼
RegisterSourceOutput → EmitPoco()  ← TrackablePocoEmitter로 코드 생성


파이프라인 2: ITrackableContainer<T> 처리
─────────────────────────────────────────
(위와 동일한 구조, EmitContainer()로 코드 생성)
```

#### 핵심 메서드들

- **`GetPocoTarget()`**: 구문 노드가 `ITrackablePoco<T>`를 구현하는 인터페이스인지 확인
- **`GetContainerTarget()`**: 구문 노드가 `ITrackableContainer<T>`를 구현하는 인터페이스인지 확인
- **`ImplementsInterface()`**: 심볼의 `AllInterfaces`와 `Interfaces`를 순회하며 특정 인터페이스 구현 여부 확인
- **`EmitPoco()` / `EmitContainer()`**: 각 Emitter를 호출하고 `context.AddSource()`로 생성된 코드를 등록

#### 파일 네이밍 규칙

인터페이스 이름에서 앞의 `I`를 제거하고 `Trackable`을 붙입니다:
- `IPersonData` → `TrackablePersonData.g.cs`
- `IGameContainer` → `TrackableGameContainer.g.cs`

---

### 2. TrackablePocoEmitter.cs (POCO 코드 생성)

`ITrackablePoco<T>`를 구현하는 인터페이스에 대해 **변경 추적 가능한 구현 클래스**를 생성합니다.

#### 입력 예시

```csharp
public interface IPersonData : ITrackablePoco<IPersonData>
{
    string Name { get; set; }
    int Age { get; set; }
}
```

#### 생성되는 코드의 구조

```csharp
public partial class TrackablePersonData : IPersonData
{
    // 1. Tracker 프로퍼티
    public IPocoTracker<IPersonData> Tracker { get; set; }

    // 2. Clone 메서드
    public TrackablePersonData Clone() { ... }

    // 3. Changed 프로퍼티
    public bool Changed { get { return Tracker != null && Tracker.HasChange; } }

    // 4. ITrackable 인터페이스 명시적 구현
    ITracker ITrackable.Tracker { get; set; }
    ITracker<IPersonData> ITrackable<IPersonData>.Tracker { get; set; }
    ITrackable ITrackable.Clone() { return Clone(); }

    // 5. 자식 Trackable 탐색 (중첩 Trackable 지원)
    public ITrackable GetChildTrackable(object name) { ... }
    public IEnumerable<KeyValuePair<object, ITrackable>> GetChildTrackables(...) { ... }

    // 6. PropertyTable - 리플렉션 캐시
    public static class PropertyTable
    {
        public static readonly PropertyInfo Name = typeof(IPersonData).GetProperty("Name");
        public static readonly PropertyInfo Age = typeof(IPersonData).GetProperty("Age");
    }

    // 7. 각 프로퍼티의 backing field + setter에서 변경 추적
    private string _Name;
    public string Name
    {
        get { return _Name; }
        set
        {
            if (Tracker != null && Name != value)
                Tracker.TrackSet(PropertyTable.Name, _Name, value);  // ← 핵심!
            _Name = value;
        }
    }
}
```

**핵심 메커니즘**: 프로퍼티 setter에서 값이 변경될 때 `Tracker.TrackSet()`을 호출하여 변경 사항을 기록합니다.

---

### 3. TrackableContainerEmitter.cs (Container 코드 생성)

`ITrackableContainer<T>`를 구현하는 인터페이스에 대해 **Container 클래스와 Tracker 클래스 2개**를 생성합니다.

Container는 여러 Trackable 객체를 묶어서 관리하는 복합 객체입니다.

#### 입력 예시

```csharp
public interface IGameData : ITrackableContainer<IGameData>
{
    TrackablePersonData Person { get; set; }
    TrackableDictionary<int, string> Inventory { get; set; }
}
```

#### 생성되는 코드 (2개 클래스)

**클래스 1: `TrackableGameData`** (Container 구현)

```csharp
public partial class TrackableGameData : IGameData
{
    // Tracker 설정 시 자식 Trackable에도 전파
    public TrackableGameDataTracker Tracker
    {
        set
        {
            _tracker = value;
            Person.Tracker = value?.PersonTracker;       // ← 하위 전파
            Inventory.Tracker = value?.InventoryTracker;  // ← 하위 전파
        }
    }

    // 프로퍼티 backing field은 new로 초기화
    private TrackablePersonData _Person = new TrackablePersonData();

    // setter에서 Tracker 연결/해제 관리
    public TrackablePersonData Person
    {
        set
        {
            if (_Person != null) _Person.Tracker = null;          // 기존 객체에서 해제
            if (value != null) value.Tracker = Tracker?.PersonTracker;  // 새 객체에 연결
            _Person = value;
        }
    }
}
```

**클래스 2: `TrackableGameDataTracker`** (Tracker 구현)

```csharp
public class TrackableGameDataTracker : IContainerTracker<IGameData>
{
    // 각 자식의 Tracker를 보유
    public TrackablePocoTracker<IPersonData> PersonTracker { get; set; } = new ...();
    public TrackableDictionaryTracker<int, string> InventoryTracker { get; set; } = new ...();

    // HasChange: 자식 중 하나라도 변경되었는지
    public bool HasChange { get { return (PersonTracker?.HasChange) || ...; } }

    // Clear / ApplyTo / RollbackTo: 모든 자식에 전파
    public void Clear() { PersonTracker?.Clear(); InventoryTracker?.Clear(); }
    public void ApplyTo(IGameData trackable) { PersonTracker?.ApplyTo(trackable.Person); ... }
    public void RollbackTo(IGameData trackable) { PersonTracker?.RollbackTo(trackable.Person); ... }
}
```

---

### 4. EmitterHelper.cs (공통 유틸리티)

Emitter들이 공유하는 헬퍼 메서드와 `PropertyInfo` 데이터 클래스를 정의합니다.

#### 주요 메서드

| 메서드 | 역할 |
|--------|------|
| `GetFullNamespace()` | 심볼의 전체 네임스페이스 문자열 반환 |
| `GetProperties()` | 인터페이스의 getter+setter 프로퍼티 목록 수집 |
| `IsTrackableType()` | 타입이 Trackable 계열인지 판별 (이름 기반 + 인터페이스 기반) |
| `GetTrackerTypeName()` | 타입에 대응하는 Tracker 타입명 결정 |

#### GetTrackerTypeName() 매핑 규칙

| 입력 타입 | 반환하는 Tracker 타입 |
|-----------|---------------------|
| `TrackableDictionary<K,V>` | `TrackableDictionaryTracker<K,V>` |
| `TrackableList<T>` | `TrackableListTracker<T>` |
| `TrackableSet<T>` | `TrackableSetTracker<T>` |
| `ITrackablePoco<T>` 구현 타입 | `TrackablePocoTracker<T>` |
| `ITrackableContainer<T>` 구현 타입 | `Trackable{Name}Tracker` |
| 기타 | `ITracker` (폴백) |

#### PropertyInfo 클래스

```csharp
internal class PropertyInfo
{
    public string Name { get; }         // 프로퍼티 이름
    public string TypeName { get; }     // 타입의 표시 문자열
    public ITypeSymbol TypeSymbol { get; }  // Roslyn 타입 심볼
    public bool IsTrackable { get; }    // Trackable 타입 여부
}
```

---

### 5. DiagnosticDescriptors.cs (진단 메시지)

Source Generator가 보고하는 컴파일 에러를 정의합니다.

| ID | 메시지 | 상황 |
|----|--------|------|
| `TRACK001` | "must be declared as partial" | 생성 대상 타입이 partial이 아닐 때 |
| `TRACK002` | "must be an interface" | ITrackablePoco 구현이 인터페이스가 아닐 때 |
| `TRACK003` | "must be an interface" | ITrackableContainer 구현이 인터페이스가 아닐 때 |
| `TRACK004` | "must have both getter and setter" | 프로퍼티에 getter나 setter가 없을 때 |

> 참고: 현재 코드에서는 이 진단들이 정의만 되어 있고, Emitter 코드에서 아직 `context.ReportDiagnostic()`으로 보고하는 로직은 구현되어 있지 않습니다.

---

## 전체 파이프라인 흐름도

```
사용자 코드 (입력)
═══════════════════════════════════════════════════════════════
public interface IPersonData : ITrackablePoco<IPersonData>
{
    string Name { get; set; }
    int Age { get; set; }
}
═══════════════════════════════════════════════════════════════

         │ Roslyn 컴파일 시작
         ▼

┌─ TrackableDataGenerator.Initialize() ─────────────────────┐
│                                                            │
│  1. SyntaxProvider가 모든 InterfaceDeclarationSyntax 수집  │
│     (구문 레벨 필터 - 빠름)                                │
│                                                            │
│  2. GetPocoTarget() / GetContainerTarget()으로             │
│     ITrackablePoco<T> 또는 ITrackableContainer<T>          │
│     구현 여부 확인 (시맨틱 레벨 필터 - 정확)               │
│                                                            │
│  3. Compilation 정보와 결합                                │
│                                                            │
│  4. RegisterSourceOutput으로 Emitter 호출                  │
└────────────────────────────────────────────────────────────┘
         │
         ▼
┌─ Emitter (코드 생성) ────────────────────────────────────┐
│                                                           │
│  EmitterHelper.GetProperties()로 프로퍼티 수집            │
│           │                                               │
│     ┌─────┴──────┐                                        │
│     ▼            ▼                                        │
│  POCO용       Container용                                 │
│  Emitter      Emitter                                     │
│     │            │                                        │
│     ▼            ▼                                        │
│  StringBuilder로 소스 코드 문자열 조립                     │
│     │            │                                        │
│     ▼            ▼                                        │
│  context.AddSource("TrackableXxx.g.cs", source)           │
└───────────────────────────────────────────────────────────┘
         │
         ▼

생성된 코드 (출력)
═══════════════════════════════════════════════════════════════
// TrackablePersonData.g.cs
public partial class TrackablePersonData : IPersonData
{
    public IPocoTracker<IPersonData> Tracker { get; set; }
    ...setter에서 Tracker.TrackSet() 호출...
}
═══════════════════════════════════════════════════════════════
```

---

## csproj 설정 해설

```xml
<TargetFramework>netstandard2.1</TargetFramework>
<!-- Source Generator는 Roslyn의 일부로 실행되므로 netstandard2.1 필수 -->

<LangVersion>9.0</LangVersion>
<!-- C# 9.0 사용 -->

<IsRoslynComponent>true</IsRoslynComponent>
<!-- Roslyn 분석기/생성기 프로젝트임을 표시 -->

<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
<!-- 분석기에 적합한 엄격한 규칙 적용 -->

<IncludeBuildOutput>true</IncludeBuildOutput>
<DevelopmentDependency>true</DevelopmentDependency>
<!-- NuGet 패키지로 배포 시 analyzer로 포함, 런타임 의존성 아님 -->
```

```xml
<None Include="$(OutputPath)\$(AssemblyName).dll"
      Pack="true" PackagePath="analyzers/dotnet/cs" />
<!-- NuGet 패키지의 analyzers/dotnet/cs 경로에 DLL 배치 → 자동으로 Source Generator로 인식 -->
```

---

## POCO vs Container 비교

| 특성 | POCO (ITrackablePoco) | Container (ITrackableContainer) |
|------|----------------------|-------------------------------|
| 목적 | 단일 객체의 프로퍼티 변경 추적 | 여러 Trackable 객체를 묶어서 관리 |
| 프로퍼티 타입 | 일반 값 (string, int 등) | 다른 Trackable 타입 |
| Tracker 타입 | `IPocoTracker<T>` | 전용 `{Name}Tracker` 클래스 생성 |
| 생성 클래스 수 | 1개 (구현 클래스) | 2개 (구현 클래스 + Tracker 클래스) |
| 변경 감지 방식 | setter에서 `TrackSet()` 호출 | 자식 Tracker들의 `HasChange` 집계 |
| Tracker 전파 | 없음 | Tracker 설정 시 자식에 자동 전파 |
