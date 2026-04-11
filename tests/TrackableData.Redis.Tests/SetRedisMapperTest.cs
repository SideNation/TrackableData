using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class SetRedisMapperTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _fixture;
        private readonly TrackableSetRedisMapper<string> _mapper;

        public SetRedisMapperTest(RedisFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableSetRedisMapper<string>();
        }

        [Fact]
        public async Task TestSet_CreateAndLoad()
        {
            var key = _fixture.Key("set_create");
            var set = new HashSet<string> { "Alpha", "Beta", "Gamma" };
            await _mapper.CreateAsync(_fixture.Db, set, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, loaded.Count);
            Assert.Contains("Alpha", loaded);
            Assert.Contains("Beta", loaded);
            Assert.Contains("Gamma", loaded);
        }

        [Fact]
        public async Task TestSet_Save_AddAndRemove()
        {
            var key = _fixture.Key("set_save");
            var set = new TrackableSet<string> { "Alpha", "Beta", "Gamma" };
            await _mapper.CreateAsync(_fixture.Db, set, key);

            set.SetDefaultTrackerDeep();
            set.Add("Delta");
            set.Remove("Beta");

            await _mapper.SaveAsync(_fixture.Db,
                (TrackableSetTracker<string>)set.Tracker, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, loaded.Count);
            Assert.Contains("Alpha", loaded);
            Assert.Contains("Gamma", loaded);
            Assert.Contains("Delta", loaded);
            Assert.DoesNotContain("Beta", loaded);
        }
    }
}
