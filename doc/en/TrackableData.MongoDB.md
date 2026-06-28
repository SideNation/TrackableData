# TrackableData.MongoDB Usage

`TrackableDataV2.MongoDB` stores TrackableData objects in MongoDB `BsonDocument` collections and applies tracker changes with MongoDB update operations.

## Installation

```xml
<ItemGroup>
  <PackageReference Include="TrackableDataV2.Core" Version="1.0.0" />
  <PackageReference Include="TrackableDataV2.Generator" Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <PackageReference Include="TrackableDataV2.MongoDB" Version="1.0.0" />
  <PackageReference Include="MongoDB.Driver" Version="3.9.0" />
</ItemGroup>
```

## Basic Connection

```csharp
using MongoDB.Bson;
using MongoDB.Driver;
using TrackableData;
using TrackableData.MongoDB;

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("game");
var collection = database.GetCollection<BsonDocument>("players");
```

Mappers use `IMongoCollection<BsonDocument>`.

## Save and Load a POCO

```csharp
public interface IPlayer : ITrackablePoco<IPlayer>
{
    string Name { get; set; }
    int Level { get; set; }
    int Gold { get; set; }
}

var mapper = new TrackablePocoMongoDbMapper<IPlayer>();

var player = new TrackablePlayer
{
    Name = "Alice",
    Level = 1,
    Gold = 100
};

await mapper.CreateAsync(collection, player, "player:1");

var loaded = await mapper.LoadAsync(collection, "player:1");
Console.WriteLine(loaded.Name);
```

The final argument, `"player:1"`, is used as the document `_id`.

## POCO Identity

Mark a POCO property with `[TrackableProperty("mongodb.identity")]` when a domain property should be serialized as MongoDB `_id`.

```csharp
public interface IPlayerAccount : ITrackablePoco<IPlayerAccount>
{
    [TrackableProperty("mongodb.identity")]
    long AccountId { get; set; }

    string Name { get; set; }
}

var mapper = new TrackablePocoMongoDbMapper<IPlayerAccount>();

var account = new TrackablePlayerAccount
{
    AccountId = UniqueInt64Id.GenerateNewId(),
    Name = "Alice"
};

await mapper.CreateAsync(collection, account);
var loaded = await mapper.LoadAsync(collection, account.AccountId);
```

When a POCO has an `Id` property such as `ObjectId Id`, keyless `CreateAsync(collection, value)` lets MongoDB generate `_id` and writes the generated value back to `Id`.

## Save Only Changes

```csharp
player.SetDefaultTrackerDeep();

player.Level = 2;
player.Gold = 150;

await mapper.SaveAsync(collection, player.Tracker, "player:1");

player.ClearTrackerDeep();
```

## Save Dictionary/List/Set

```csharp
var dictMapper = new TrackableDictionaryMongoDbMapper<int, string>();
var listMapper = new TrackableListMongoDbMapper<string>();
var setMapper = new TrackableSetMongoDbMapper<int>();

var dict = new TrackableDictionary<int, string> { { 1, "Sword" } };
var list = new TrackableList<string> { "login", "quest" };
var set = new TrackableSet<int> { 1001, 1002 };

await dictMapper.CreateAsync(collection, dict, "player:1", "inventory");
await listMapper.CreateAsync(collection, list, "player:1", "logs");
await setMapper.CreateAsync(collection, set, "player:1", "achievements");
```

`keyValues` are used as the MongoDB document path.

| Call | Meaning |
|------|---------|
| `CreateAsync(collection, value, "player:1")` | Store in the document with `_id = "player:1"` |
| `CreateAsync(collection, value, "player:1", "inventory")` | Store in the `inventory` field of the document with `_id = "player:1"` |

Collection values can be primitive values or class values. The mapper converts each value through the MongoDB BSON serializer, so class values round-trip through dictionary, list, set, and container mappers.

```csharp
public sealed class ItemValue
{
    public string Name { get; set; }
    public int Level { get; set; }
}

var itemMapper = new TrackableDictionaryMongoDbMapper<int, ItemValue>();

var items = new TrackableDictionary<int, ItemValue>
{
    { 1, new ItemValue { Name = "Sword", Level = 10 } }
};

await itemMapper.CreateAsync(collection, items, "player:1", "items");
var loadedItems = await itemMapper.LoadAsync(collection, "player:1", "items");
```

## Save Collection Changes

```csharp
dict.SetDefaultTrackerDeep();
dict[1] = "Long Sword";
dict.Add(2, "Potion");

await dictMapper.SaveAsync(
    collection,
    (TrackableDictionaryTracker<int, string>)dict.Tracker,
    "player:1",
    "inventory");

dict.ClearTrackerDeep();
```

## Save a Container

Container properties can be trackable POCO, Dictionary, List, or Set values. Dictionary/List/Set members may also contain class values.

```csharp
public interface IUserData : ITrackableContainer<IUserData>
{
    TrackableDictionary<int, ItemValue> Items { get; set; }
    TrackableList<ItemValue> History { get; set; }
}

var containerMapper = new TrackableContainerMongoDbMapper<IUserData>();

var userData = new TrackableUserData();
userData.Items.Add(1, new ItemValue { Name = "Sword", Level = 10 });
userData.History.Add(new ItemValue { Name = "LoginReward", Level = 1 });

await containerMapper.CreateAsync(collection, userData, "player:1");
var loadedUserData = await containerMapper.LoadAsync(collection, "player:1");
```

Use `SetDefaultTracker()` when you want to save changes from a loaded container.

```csharp
loadedUserData.SetDefaultTracker();
loadedUserData.Items[1] = new ItemValue { Name = "Long Sword", Level = 12 };
loadedUserData.History.Add(new ItemValue { Name = "QuestReward", Level = 2 });

await containerMapper.SaveAsync(collection, loadedUserData.Tracker, "player:1");
loadedUserData.ClearTrackerDeep();
```

Avoid `SetDefaultTrackerDeep()` in this MongoDB container save flow because the generated container tracker owns the child trackers. Calling the deep variant can replace the child trackers that the container tracker uses to collect changes.

Use `[TrackableProperty("mongodb.ignore")]` on a container property when that property should not be persisted.

## Delete

```csharp
var deletedCount = await mapper.DeleteAsync(collection, "player:1");
```

## Integration Test Configuration

MongoDB integration tests read `MONGODB_CONNECTION_STRING` from the environment first, then from `.env`. If the value is not present, the test fixture falls back to an SSH tunnel/local `mongodb://localhost:27017`, creates a temporary `trackable_test_<guid>` database, and drops it during cleanup.

## Notes

- Mappers register the required BSON class maps when they are created.
- The MongoDB plugin targets `BsonDocument` collections.
- Collection values are serialized through the MongoDB BSON serializer. Null values are stored as BSON null.
- `SaveAsync` saves only changes stored in the tracker. Use a create/replace flow or the MongoDB driver directly when you need a full replacement.
- List changes are index-based. Define your own conflict policy when multiple writers modify the same document concurrently.
