# TrackableData.PostgreSql Usage

`TrackableDataV2.PostgreSql` maps TrackableData objects to PostgreSQL tables. It supports POCO, Dictionary, Set, and Container persistence.

## Installation

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <PackageReference Include="TrackableDataV2.PostgreSql" Version="1.0.0" />
  <PackageReference Include="Npgsql" Version="9.0.3" />
</ItemGroup>
```

## Basic Connection

```csharp
using Npgsql;
using TrackableData;
using TrackableData.PostgreSql;

await using var connection = new NpgsqlConnection(
    "Host=localhost;Port=5432;Database=game;Username=postgres;Password=postgres");

await connection.OpenAsync();
```

## POCO Mapping

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

If `[TrackableProperty("sql.primary-key")]` is not present, a property named `Id` is used as the primary key.

## Save Only Changes

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

## SQL Attribute Options

| Option | Description |
|--------|-------------|
| `sql.primary-key` | Use this property as a primary key column. |
| `sql.identity` | Use this property as an identity column. |
| `sql.ignore` | Exclude this property from SQL mapping. |
| `sql.column:ColumnName` | Override the SQL column name. |

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

## Dictionary Mapping

For a single-value dictionary, specify key and value columns.

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

`headKeyColumnDefs` is used as leading owner key columns when multiple owners share one table. In the example, `1` is the `PlayerId` value.

## Set Mapping

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

## Container Mapping

A container maps each trackable property to its own table.

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

## Notes

- The PostgreSQL plugin does not currently provide a List mapper.
- Mappers generate and execute SQL strings. Do not pass untrusted external input as table or column names.
- `ResetTableAsync` resets tables, so use it carefully outside tests and setup code.
- After saving, call `ClearTrackerDeep()` if you do not want to reuse the same tracker changes.
