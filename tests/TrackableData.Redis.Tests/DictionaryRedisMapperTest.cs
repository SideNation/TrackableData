using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class DictionaryRedisMapperTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _fixture;
        private readonly TrackableDictionaryRedisMapper<string, string> _mapper;

        public DictionaryRedisMapperTest(RedisFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableDictionaryRedisMapper<string, string>();
        }

        [Fact]
        public async Task TestDictionary_CreateAndLoad()
        {
            var key = _fixture.Key("dict_create");
            var dict = new Dictionary<string, string>
            {
                { "a", "One" },
                { "b", "Two" },
                { "c", "Three" }
            };
            await _mapper.CreateAsync(_fixture.Db, dict, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, loaded.Count);
            Assert.Equal("One", loaded["a"]);
            Assert.Equal("Two", loaded["b"]);
            Assert.Equal("Three", loaded["c"]);
        }

        [Fact]
        public async Task TestDictionary_Save_AddModifyRemove()
        {
            var key = _fixture.Key("dict_save");
            var dict = new TrackableDictionary<string, string>
            {
                { "a", "One" },
                { "b", "Two" },
                { "c", "Three" }
            };
            await _mapper.CreateAsync(_fixture.Db, dict, key);

            dict.SetDefaultTrackerDeep();
            dict.Add("d", "Four");
            dict["a"] = "OneModified";
            dict.Remove("c");

            await _mapper.SaveAsync(_fixture.Db,
                (TrackableDictionaryTracker<string, string>)dict.Tracker, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, loaded.Count);
            Assert.Equal("OneModified", loaded["a"]);
            Assert.Equal("Two", loaded["b"]);
            Assert.Equal("Four", loaded["d"]);
        }

        [Fact]
        public async Task TestDictionary_Delete()
        {
            var key = _fixture.Key("dict_delete");
            var dict = new Dictionary<string, string> { { "a", "One" } };
            await _mapper.CreateAsync(_fixture.Db, dict, key);

            await _mapper.DeleteAsync(_fixture.Db, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Null(loaded);
        }
    }
}
