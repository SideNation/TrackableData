using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    // Verifies a complex class value (BsonValueMapperTestClass) round-trips end-to-end through
    // every mapper against a live MongoDB.
    public class ClassValueMongoDbMapperTest : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _fixture;

        public ClassValueMongoDbMapperTest(MongoDbFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task EnsureDocumentExists(IMongoCollection<BsonDocument> collection, string docId)
        {
            var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", docId)).FirstOrDefaultAsync();
            if (existing == null)
                await collection.InsertOneAsync(new BsonDocument("_id", docId));
        }

        [Fact]
        public async Task Poco_ClassProperty_CreateLoadSave()
        {
            var collection = _fixture.GetCollection("class_poco");
            var mapper = new TrackablePocoMongoDbMapper<ITestPocoWithClass>();
            var poco = new TrackableTestPocoWithClass
            {
                Name = "P",
                Data = new BsonValueMapperTestClass { Name = "D", Level = 5 }
            };

            await mapper.CreateAsync(collection, poco, "poco1");

            var loaded = await mapper.LoadAsync(collection, "poco1");
            Assert.Equal("P", loaded.Name);
            Assert.NotNull(loaded.Data);
            Assert.Equal("D", loaded.Data.Name);
            Assert.Equal(5, loaded.Data.Level);

            loaded.SetDefaultTrackerDeep();
            loaded.Data = new BsonValueMapperTestClass { Name = "D2", Level = 50 };
            await mapper.SaveAsync(collection, loaded.Tracker, "poco1");

            var reloaded = await mapper.LoadAsync(collection, "poco1");
            Assert.Equal("D2", reloaded.Data.Name);
            Assert.Equal(50, reloaded.Data.Level);
        }

        [Fact]
        public async Task Dictionary_ClassValue_CreateLoadSave()
        {
            var collection = _fixture.GetCollection("class_dict");
            var mapper = new TrackableDictionaryMongoDbMapper<int, BsonValueMapperTestClass>();
            var dict = new TrackableDictionary<int, BsonValueMapperTestClass>
            {
                { 1, new BsonValueMapperTestClass { Name = "Alpha", Level = 1 } },
                { 2, new BsonValueMapperTestClass { Name = "Beta", Level = 2 } }
            };

            await mapper.CreateAsync(collection, dict, "dict1");

            var loaded = await mapper.LoadAsync(collection, "dict1");
            Assert.Equal(2, loaded.Count);
            Assert.Equal("Alpha", loaded[1].Name);
            Assert.Equal(2, loaded[2].Level);

            loaded.SetDefaultTrackerDeep();
            loaded[1] = new BsonValueMapperTestClass { Name = "AlphaX", Level = 11 };
            loaded.Add(3, new BsonValueMapperTestClass { Name = "Gamma", Level = 3 });
            await mapper.SaveAsync(
                collection, (TrackableDictionaryTracker<int, BsonValueMapperTestClass>)loaded.Tracker, "dict1");

            var reloaded = await mapper.LoadAsync(collection, "dict1");
            Assert.Equal("AlphaX", reloaded[1].Name);
            Assert.Equal(11, reloaded[1].Level);
            Assert.Equal("Gamma", reloaded[3].Name);
        }

        [Fact]
        public async Task List_ClassValue_CreateLoadSave()
        {
            var collection = _fixture.GetCollection("class_list");
            await EnsureDocumentExists(collection, "list1");
            var mapper = new TrackableListMongoDbMapper<BsonValueMapperTestClass>();
            var list = new TrackableList<BsonValueMapperTestClass>
            {
                new BsonValueMapperTestClass { Name = "A", Level = 1 },
                new BsonValueMapperTestClass { Name = "B", Level = 2 }
            };

            await mapper.CreateAsync(collection, list, "list1", "items");

            var loaded = await mapper.LoadAsync(collection, "list1", "items");
            Assert.Equal(2, loaded.Count);
            Assert.Equal("A", loaded[0].Name);
            Assert.Equal(2, loaded[1].Level);

            loaded.SetDefaultTrackerDeep();
            loaded.Add(new BsonValueMapperTestClass { Name = "C", Level = 3 });
            await mapper.SaveAsync(
                collection, (TrackableListTracker<BsonValueMapperTestClass>)loaded.Tracker, "list1", "items");

            var reloaded = await mapper.LoadAsync(collection, "list1", "items");
            Assert.Equal(3, reloaded.Count);
            Assert.Equal("C", reloaded[2].Name);
            Assert.Equal(3, reloaded[2].Level);
        }

        [Fact]
        public async Task Set_ClassValue_CreateAndLoad()
        {
            var collection = _fixture.GetCollection("class_set");
            await EnsureDocumentExists(collection, "set1");
            var mapper = new TrackableSetMongoDbMapper<BsonValueMapperTestClass>();
            var set = new TrackableSet<BsonValueMapperTestClass>
            {
                new BsonValueMapperTestClass { Name = "X", Level = 1 },
                new BsonValueMapperTestClass { Name = "Y", Level = 2 }
            };

            await mapper.CreateAsync(collection, set, "set1", "data");

            var loaded = await mapper.LoadAsync(collection, "set1", "data");
            Assert.Equal(2, loaded.Count);
            Assert.Equal(new[] { "X", "Y" }, loaded.Select(x => x.Name).OrderBy(n => n));
            Assert.Equal(3, loaded.Sum(x => x.Level));
        }

        [Fact]
        public async Task Container_ClassValues_CreateLoadSave()
        {
            var collection = _fixture.GetCollection("class_container");
            var mapper = new TrackableContainerMongoDbMapper<IClassContainer>();

            await mapper.CreateAsync(collection, TestData.CreateSampleClassContainer(), "cc1");

            var loaded = await mapper.LoadAsync(collection, "cc1");
            Assert.Equal("Alpha", loaded.Items[1].Name);
            Assert.Equal(2, loaded.Items[2].Level);
            Assert.Equal(2, loaded.History.Count);
            Assert.Equal("H1", loaded.History[0].Name);

            loaded.SetDefaultTracker();
            loaded.Items[1] = new BsonValueMapperTestClass { Name = "AlphaX", Level = 11 };
            loaded.Items.Add(3, new BsonValueMapperTestClass { Name = "Gamma", Level = 3 });
            loaded.History.Add(new BsonValueMapperTestClass { Name = "H3", Level = 30 });
            await mapper.SaveAsync(collection, loaded.Tracker, "cc1");

            var reloaded = await mapper.LoadAsync(collection, "cc1");
            Assert.Equal("AlphaX", reloaded.Items[1].Name);
            Assert.Equal("Gamma", reloaded.Items[3].Name);
            Assert.Equal(3, reloaded.History.Count);
            Assert.Equal("H3", reloaded.History[2].Name);
        }
    }
}
