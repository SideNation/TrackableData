using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class DictionaryMongoDbMapperTest : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _fixture;
        private readonly TrackableDictionaryMongoDbMapper<int, string> _mapper;

        public DictionaryMongoDbMapperTest(MongoDbFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableDictionaryMongoDbMapper<int, string>();
        }

        [Fact]
        public async Task TestDictionary_CreateAndLoad()
        {
            var collection = _fixture.GetCollection("dict_create_load");
            var dict = new TrackableDictionary<int, string> { { 1, "One" }, { 2, "Two" } };

            await _mapper.CreateAsync(collection, dict, "dict1");
            var loaded = await _mapper.LoadAsync(collection, "dict1");

            Assert.NotNull(loaded);
            Assert.Equal(2, loaded.Count);
            Assert.Equal("One", loaded[1]);
            Assert.Equal("Two", loaded[2]);
        }

        [Fact]
        public async Task TestDictionary_Save()
        {
            var collection = _fixture.GetCollection("dict_save");
            var dict = new TrackableDictionary<int, string> { { 1, "One" }, { 2, "Two" } };
            await _mapper.CreateAsync(collection, dict, "dict_save");

            dict.SetDefaultTrackerDeep();
            dict[1] = "OneModified";
            dict.Remove(2);
            dict[3] = "Three";

            await _mapper.SaveAsync(collection, (TrackableDictionaryTracker<int, string>)dict.Tracker, "dict_save");

            var loaded = await _mapper.LoadAsync(collection, "dict_save");
            Assert.Equal("OneModified", loaded[1]);
            Assert.False(loaded.ContainsKey(2));
            Assert.Equal("Three", loaded[3]);
        }

        [Fact]
        public async Task TestDictionary_Delete()
        {
            var collection = _fixture.GetCollection("dict_delete");
            var dict = new TrackableDictionary<int, string> { { 1, "One" } };
            await _mapper.CreateAsync(collection, dict, "dict_del");

            var count = await _mapper.DeleteAsync(collection, "dict_del");
            Assert.Equal(1, count);

            var loaded = await _mapper.LoadAsync(collection, "dict_del");
            Assert.Null(loaded);
        }
    }
}
