using System.Threading.Tasks;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class PocoRedisMapperTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _fixture;
        private readonly TrackablePocoRedisMapper<ITestPerson> _mapper;

        public PocoRedisMapperTest(RedisFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackablePocoRedisMapper<ITestPerson>();
        }

        [Fact]
        public async Task TestPoco_CreateAndLoad()
        {
            var key = _fixture.Key("poco_create");
            var person = new TrackableTestPerson { Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(_fixture.Db, person, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.NotNull(loaded);
            Assert.Equal("Alice", loaded.Name);
            Assert.Equal(30, loaded.Age);
        }

        [Fact]
        public async Task TestPoco_Save()
        {
            var key = _fixture.Key("poco_save");
            var person = new TrackableTestPerson { Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(_fixture.Db, person, key);

            person.SetDefaultTrackerDeep();
            person.Name = "Bob";
            person.Age = 25;

            await _mapper.SaveAsync(_fixture.Db,
                (TrackablePocoTracker<ITestPerson>)person.Tracker, key);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal("Bob", loaded.Name);
            Assert.Equal(25, loaded.Age);
        }

        [Fact]
        public async Task TestPoco_Delete()
        {
            var key = _fixture.Key("poco_delete");
            var person = new TrackableTestPerson { Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(_fixture.Db, person, key);

            var deleted = await _mapper.DeleteAsync(_fixture.Db, key);
            Assert.Equal(1, deleted);

            var loaded = await _mapper.LoadAsync(_fixture.Db, key);
            Assert.Null(loaded);
        }
    }
}
