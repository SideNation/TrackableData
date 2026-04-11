using System.Threading.Tasks;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class PocoMongoDbMapperTest : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _fixture;
        private readonly TrackablePocoMongoDbMapper<ITestPerson> _mapper;

        public PocoMongoDbMapperTest(MongoDbFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackablePocoMongoDbMapper<ITestPerson>();
        }

        [Fact]
        public async Task TestPoco_CreateAndLoad()
        {
            var collection = _fixture.GetCollection("poco_create_load");
            var person = new TrackableTestPerson { Name = "Alice", Age = 30 };

            await _mapper.CreateAsync(collection, person, "person1");
            var loaded = await _mapper.LoadAsync(collection, "person1");

            Assert.NotNull(loaded);
            Assert.Equal("Alice", loaded.Name);
            Assert.Equal(30, loaded.Age);
        }

        [Fact]
        public async Task TestPoco_Save()
        {
            var collection = _fixture.GetCollection("poco_save");
            var person = new TrackableTestPerson { Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(collection, person, "person_save");

            person.SetDefaultTrackerDeep();
            person.Name = "Bob";
            person.Age = 25;

            await _mapper.SaveAsync(collection, person.Tracker, "person_save");

            var loaded = await _mapper.LoadAsync(collection, "person_save");
            Assert.Equal("Bob", loaded.Name);
            Assert.Equal(25, loaded.Age);
        }

        [Fact]
        public async Task TestPoco_Delete()
        {
            var collection = _fixture.GetCollection("poco_delete");
            var person = new TrackableTestPerson { Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(collection, person, "person_del");

            var count = await _mapper.DeleteAsync(collection, "person_del");
            Assert.Equal(1, count);

            var loaded = await _mapper.LoadAsync(collection, "person_del");
            Assert.Null(loaded);
        }
    }
}
