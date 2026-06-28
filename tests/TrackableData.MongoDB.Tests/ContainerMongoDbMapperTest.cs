using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class ContainerMongoDbMapperTest : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _fixture;
        private readonly TrackableContainerMongoDbMapper<ITestContainer> _mapper;

        public ContainerMongoDbMapperTest(MongoDbFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableContainerMongoDbMapper<ITestContainer>();
        }

        [Fact]
        public async Task TestContainer_CreateAndLoad()
        {
            var collection = _fixture.GetCollection("container_create_load");
            var container = TestData.CreateSampleContainer();

            await _mapper.CreateAsync(collection, container, "container1");
            var loaded = await _mapper.LoadAsync(collection, "container1");

            Assert.NotNull(loaded);
            Assert.Equal("Alice", loaded.Person.Name);
            Assert.Equal(30, loaded.Person.Age);
            Assert.Equal("First", loaded.Missions[1]);
            Assert.Equal("Second", loaded.Missions[2]);
            Assert.Equal(new List<string> { "red", "green" }, loaded.Tags);
            Assert.Equal(new[] { "a1", "a2" }, loaded.Aliases.OrderBy(v => v));
        }

        [Fact]
        public async Task TestContainer_Save()
        {
            var collection = _fixture.GetCollection("container_save");
            var container = TestData.CreateSampleContainer();
            await _mapper.CreateAsync(collection, container, "container_save");

            var loaded = await _mapper.LoadAsync(collection, "container_save");
            // A container tracker owns and propagates its child trackers, so set it non-deep;
            // SetDefaultTrackerDeep would overwrite (disconnect) the children's trackers.
            loaded.SetDefaultTracker();
            loaded.Person.Name = "Bob";
            loaded.Person.Age = 25;
            loaded.Missions.Add(3, "Third");
            loaded.Missions.Remove(1);
            loaded.Tags.Add("blue");
            loaded.Aliases.Remove("a1");
            loaded.Aliases.Add("a3");

            await _mapper.SaveAsync(collection, loaded.Tracker, "container_save");

            var reloaded = await _mapper.LoadAsync(collection, "container_save");
            Assert.Equal("Bob", reloaded.Person.Name);
            Assert.Equal(25, reloaded.Person.Age);
            Assert.False(reloaded.Missions.ContainsKey(1));
            Assert.Equal("Third", reloaded.Missions[3]);
            Assert.Equal(new List<string> { "red", "green", "blue" }, reloaded.Tags);
            Assert.Equal(new[] { "a2", "a3" }, reloaded.Aliases.OrderBy(v => v));
        }

        [Fact]
        public async Task TestContainer_Delete()
        {
            var collection = _fixture.GetCollection("container_delete");
            var container = TestData.CreateSampleContainer();
            await _mapper.CreateAsync(collection, container, "container_del");

            var count = await _mapper.DeleteAsync(collection, "container_del");
            Assert.Equal(1, count);

            var loaded = await _mapper.LoadAsync(collection, "container_del");
            Assert.Null(loaded);
        }
    }
}
