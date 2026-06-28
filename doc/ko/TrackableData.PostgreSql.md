# TrackableData.PostgreSql 사용법

`TrackableDataV2.PostgreSql`은 TrackableData 객체를 PostgreSQL 테이블에 매핑합니다. POCO, Dictionary, Set, Container 저장을 지원합니다.

## 설치

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <PackageReference Include="TrackableDataV2.PostgreSql" Version="1.0.0" />
  <PackageReference Include="Npgsql" Version="10.0.3" />
</ItemGroup>
```

## 기본 연결

```csharp
using Npgsql;
using TrackableData;
using TrackableData.PostgreSql;

await using var connection = new NpgsqlConnection(
    "Host=localhost;Port=5432;Database=game;Username=postgres;Password=postgres");

await connection.OpenAsync();
```

## POCO 매핑

```csharp
public interface IPlayer : ITrackablePoco<IPlayer>
{
    [TrackableProperty("sql.primary-key")]
    int Id { get; set; }

    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}

var mapper = new TrackablePocoSqlMapper<IPlayer>(
    PostgreSqlProvider.Instance,
    "Player");

await mapper.ResetTableAsync(connection);

var player = new TrackablePlayer
{
    Id = 1,
    Name = "Alice",
    Level = 1,
    Gold = 100
};

await mapper.CreateAsync(connection, player);

var loaded = await mapper.LoadAsync(connection, 1);
Console.WriteLine(loaded.Name);
```

`[TrackableProperty("sql.primary-key")]`가 없으면 이름이 `Id`인 프로퍼티를 primary key로 사용합니다.

## 변경분만 저장

```csharp
player.SetDefaultTrackerDeep();

player.Level = 2;
player.Gold = 150;

await mapper.SaveAsync(
    connection,
    (TrackablePocoTracker<IPlayer>)player.Tracker,
    1);

player.ClearTrackerDeep();
```

## SQL 속성 옵션

| 옵션 | 설명 |
|------|------|
| `sql.primary-key` | primary key 컬럼으로 사용합니다. |
| `sql.identity` | identity 컬럼으로 사용합니다. |
| `sql.ignore` | SQL 매핑에서 제외합니다. |
| `sql.column:ColumnName` | 컬럼 이름을 변경합니다. |

```csharp
public interface IAccount : ITrackablePoco<IAccount>
{
    [TrackableProperty("sql.primary-key")]
    long AccountId { get; set; }

    [TrackableProperty("sql.column:display_name")]
    string DisplayName { get; set; }

    [TrackableProperty("sql.ignore")]
    string RuntimeOnlyValue { get; set; }
}
```

## Dictionary 매핑

단일 값 dictionary는 key/value 컬럼을 지정합니다.

```csharp
var mapper = new TrackableDictionarySqlMapper<int, string>(
    PostgreSqlProvider.Instance,
    "PlayerInventory",
    new ColumnDefinition("ItemId", typeof(int)),
    new ColumnDefinition("ItemName", typeof(string), 100),
    headKeyColumnDefs: new[]
    {
        new ColumnDefinition("PlayerId", typeof(int))
    });

await mapper.ResetTableAsync(connection);

var inventory = new TrackableDictionary<int, string>
{
    { 1001, "Sword" },
    { 1002, "Potion" }
};

await mapper.CreateAsync(connection, inventory, 1);

inventory.SetDefaultTrackerDeep();
inventory[1001] = "Long Sword";
inventory.Remove(1002);

await mapper.SaveAsync(
    connection,
    (TrackableDictionaryTracker<int, string>)inventory.Tracker,
    1);
```

`headKeyColumnDefs`는 여러 owner의 데이터를 같은 테이블에 넣을 때 앞쪽 key 컬럼으로 사용합니다. 위 예제의 `1`은 `PlayerId` 값입니다.

## Set 매핑

```csharp
var mapper = new TrackableSetSqlMapper<int>(
    PostgreSqlProvider.Instance,
    "PlayerAchievements",
    new ColumnDefinition("AchievementId", typeof(int)),
    new[]
    {
        new ColumnDefinition("PlayerId", typeof(int))
    });

await mapper.ResetTableAsync(connection);

var achievements = new TrackableSet<int> { 100, 200 };
await mapper.CreateAsync(connection, achievements, 1);

achievements.SetDefaultTrackerDeep();
achievements.Add(300);
achievements.Remove(100);

await mapper.SaveAsync(
    connection,
    (TrackableSetTracker<int>)achievements.Tracker,
    1);
```

## Container 매핑

Container는 여러 trackable 프로퍼티를 각각의 테이블에 매핑합니다.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<int, string> Inventory { get; set; }
    TrackableSet<int> Achievements { get; set; }
}

var mapper = new TrackableContainerSqlMapper<IUserData>(
    PostgreSqlProvider.Instance,
    new[]
    {
        Tuple.Create(
            "Inventory",
            new object[]
            {
                "UserInventory",
                new ColumnDefinition("ItemId", typeof(int)),
                new ColumnDefinition("ItemName", typeof(string), 100),
                new[] { new ColumnDefinition("UserId", typeof(int)) }
            }),
        Tuple.Create(
            "Achievements",
            new object[]
            {
                "UserAchievements",
                new ColumnDefinition("AchievementId", typeof(int)),
                new[] { new ColumnDefinition("UserId", typeof(int)) }
            })
    });

await mapper.ResetTableAsync(connection, dropIfExists: true);

var userData = new TrackableUserData();
userData.Inventory[1001] = "Sword";
userData.Achievements.Add(200);

await mapper.CreateAsync(connection, userData, 1);

userData.SetDefaultTrackerDeep();
userData.Inventory[1001] = "Long Sword";

await mapper.SaveAsync(connection, userData.Tracker, 1);
```

## 주의할 점

- PostgreSQL 플러그인은 현재 List mapper를 제공하지 않습니다.
- mapper는 SQL 문자열을 생성해 실행합니다. 외부 입력을 column/table 이름으로 직접 넣지 않습니다.
- `ResetTableAsync`는 테이블을 초기화하므로 운영 코드에서는 신중하게 사용합니다.
- 저장 후 같은 tracker를 재사용하지 않으려면 `ClearTrackerDeep()`을 호출합니다.
