using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class ListMongoDbMapperTest : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _fixture;
        private readonly TrackableListMongoDbMapper<string> _mapper;

        public ListMongoDbMapperTest(MongoDbFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableListMongoDbMapper<string>();
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
        public async Task TestList_CreateAndLoad()
        {
            var collectionName = "list_create_load";
            await EnsureDocumentExists(collectionName, "list1");

            var collection = _fixture.GetCollection(collectionName);
            var list = new TrackableList<string> { "One", "Two", "Three" };

            await _mapper.CreateAsync(collection, list, "list1", "items");
            var loaded = await _mapper.LoadAsync(collection, "list1", "items");

            Assert.NotNull(loaded);
            Assert.Equal(new List<string> { "One", "Two", "Three" }, loaded);
        }

        [Fact]
        public async Task TestList_Save_PushBack()
        {
            var collectionName = "list_save_push";
            await EnsureDocumentExists(collectionName, "list_push");

            var collection = _fixture.GetCollection(collectionName);
            var list = new TrackableList<string> { "One", "Two" };
            await _mapper.CreateAsync(collection, list, "list_push", "items");

            list.SetDefaultTrackerDeep();
            list.Add("Three");

            await _mapper.SaveAsync(collection, (TrackableListTracker<string>)list.Tracker, "list_push", "items");

            var loaded = await _mapper.LoadAsync(collection, "list_push", "items");
            Assert.Equal(new List<string> { "One", "Two", "Three" }, loaded);
        }

        [Fact]
        public async Task TestList_Save_Modify()
        {
            var collectionName = "list_save_modify";
            await EnsureDocumentExists(collectionName, "list_mod");

            var collection = _fixture.GetCollection(collectionName);
            var list = new TrackableList<string> { "One", "Two", "Three" };
            await _mapper.CreateAsync(collection, list, "list_mod", "items");

            list.SetDefaultTrackerDeep();
            list[0] = "OneModified";

            await _mapper.SaveAsync(collection, (TrackableListTracker<string>)list.Tracker, "list_mod", "items");

            var loaded = await _mapper.LoadAsync(collection, "list_mod", "items");
            Assert.Equal("OneModified", loaded[0]);
        }

        [Fact]
        public async Task TestList_Save_PopBack()
        {
            var collectionName = "list_save_pop";
            await EnsureDocumentExists(collectionName, "list_pop");

            var collection = _fixture.GetCollection(collectionName);
            var list = new TrackableList<string> { "One", "Two", "Three" };
            await _mapper.CreateAsync(collection, list, "list_pop", "items");

            list.SetDefaultTrackerDeep();
            list.RemoveAt(list.Count - 1);

            await _mapper.SaveAsync(collection, (TrackableListTracker<string>)list.Tracker, "list_pop", "items");

            var loaded = await _mapper.LoadAsync(collection, "list_pop", "items");
            Assert.Equal(new List<string> { "One", "Two" }, loaded);
        }
    }
}
