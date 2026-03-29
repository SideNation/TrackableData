using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class ListRedisMapperTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _fixture;
        private readonly TrackableListRedisMapper<string> _mapper;

        public ListRedisMapperTest(RedisFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableListRedisMapper<string>();
        }

        [Fact]
        public async Task TestList_CreateAndLoad()
        {
            var key = _fixture.Key("list_create");
            var list = new List<string> { "Alpha", "Beta", "Gamma" };
            await _mapper.CreateAsync(_fixture.Db, list, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, loaded.Count);
            Assert.Equal("Alpha", loaded[0]);
            Assert.Equal("Beta", loaded[1]);
            Assert.Equal("Gamma", loaded[2]);
        }

        [Fact]
        public async Task TestList_Save_PushBackAndModify()
        {
            var key = _fixture.Key("list_push");
            var list = new TrackableList<string> { "Alpha", "Beta" };
            await _mapper.CreateAsync(_fixture.Db, list, key);

            list.SetDefaultTrackerDeep();
            list.Add("Gamma");
            list[0] = "AlphaModified";

            await _mapper.SaveAsync(_fixture.Db,
                (TrackableListTracker<string>)list.Tracker, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, loaded.Count);
            Assert.Equal("AlphaModified", loaded[0]);
            Assert.Equal("Beta", loaded[1]);
            Assert.Equal("Gamma", loaded[2]);
        }

        [Fact]
        public async Task TestList_Delete()
        {
            var key = _fixture.Key("list_delete");
            var list = new List<string> { "Alpha" };
            await _mapper.CreateAsync(_fixture.Db, list, key);

            await _mapper.DeleteAsync(_fixture.Db, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Null(loaded);
        }
    }
}
