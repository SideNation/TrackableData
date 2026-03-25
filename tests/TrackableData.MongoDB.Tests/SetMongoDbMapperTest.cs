using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class SetMongoDbMapperTest : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _fixture;
        private readonly TrackableSetMongoDbMapper<int> _mapper;

        public SetMongoDbMapperTest(MongoDbFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableSetMongoDbMapper<int>();
        }

        private async Task EnsureDocumentExists(string collectionName, string docId)
        {
            var collection = _fixture.GetCollection(collectionName);
            var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", docId)).FirstOrDefaultAsync();
            if (existing == null)
            {
                await collection.InsertOneAsync(new BsonDocument("_id", docId));
            }
        }

        [Fact]
        public async Task TestSet_CreateAndLoad()
        {
            var collectionName = "set_create_load";
            await EnsureDocumentExists(collectionName, "set1");

            var collection = _fixture.GetCollection(collectionName);
            var set = new TrackableSet<int> { 1, 2, 3 };

            await _mapper.CreateAsync(collection, set, "set1", "data");
            var loaded = await _mapper.LoadAsync(collection, "set1", "data");

            Assert.NotNull(loaded);
            Assert.Equal(new[] { 1, 2, 3 }, loaded.OrderBy(v => v));
        }

        [Fact]
        public async Task TestSet_Save()
        {
            var collectionName = "set_save";
            await EnsureDocumentExists(collectionName, "set_save");

            var collection = _fixture.GetCollection(collectionName);
            var set = new TrackableSet<int> { 1, 2, 3 };
            await _mapper.CreateAsync(collection, set, "set_save", "data");

            set.SetDefaultTrackerDeep();
            set.Remove(2);
            set.Add(4);

            await _mapper.SaveAsync(collection, (TrackableSetTracker<int>)set.Tracker, "set_save", "data");

            var loaded = await _mapper.LoadAsync(collection, "set_save", "data");
            Assert.Equal(new[] { 1, 3, 4 }, loaded.OrderBy(v => v));
        }
    }
}
