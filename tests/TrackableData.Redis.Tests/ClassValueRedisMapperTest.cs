using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.Redis.Tests
{
    // Verifies a complex class value (SampleData) round-trips end-to-end through every
    // Redis mapper against a live Redis.
    public class ClassValueRedisMapperTest : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture _fixture;

        public ClassValueRedisMapperTest(RedisFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Poco_ClassProperty_CreateLoadSave()
        {
            var key = _fixture.Key("class_poco");
            var mapper = new TrackablePocoRedisMapper<ITestPocoWithClass>();
            var poco = new TrackableTestPocoWithClass
            {
                Name = "P",
                Data = new SampleData { Name = "D", Level = 5 }
            };
            await mapper.CreateAsync(_fixture.Db, poco, key);

            var loaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal("P", loaded.Name);
            Assert.NotNull(loaded.Data);
            Assert.Equal("D", loaded.Data.Name);
            Assert.Equal(5, loaded.Data.Level);

            loaded.SetDefaultTrackerDeep();
            loaded.Data = new SampleData { Name = "D2", Level = 50 };
            await mapper.SaveAsync(_fixture.Db, loaded.Tracker, key);

            var reloaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal("D2", reloaded.Data.Name);
            Assert.Equal(50, reloaded.Data.Level);
        }

        [Fact]
        public async Task Dictionary_ClassValue_CreateLoadSave()
        {
            var key = _fixture.Key("class_dict");
            var mapper = new TrackableDictionaryRedisMapper<int, SampleData>();
            var dict = new TrackableDictionary<int, SampleData>
            {
                { 1, new SampleData { Name = "Alpha", Level = 1 } },
                { 2, new SampleData { Name = "Beta", Level = 2 } }
            };
            await mapper.CreateAsync(_fixture.Db, dict, key);

            var loaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(2, loaded.Count);
            Assert.Equal("Alpha", loaded[1].Name);
            Assert.Equal(2, loaded[2].Level);

            loaded.SetDefaultTrackerDeep();
            loaded[1] = new SampleData { Name = "AlphaX", Level = 11 };
            loaded.Add(3, new SampleData { Name = "Gamma", Level = 3 });
            await mapper.SaveAsync(_fixture.Db, (TrackableDictionaryTracker<int, SampleData>)loaded.Tracker, key);

            var reloaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal("AlphaX", reloaded[1].Name);
            Assert.Equal(11, reloaded[1].Level);
            Assert.Equal("Gamma", reloaded[3].Name);
        }

        [Fact]
        public async Task List_ClassValue_CreateLoadSave()
        {
            var key = _fixture.Key("class_list");
            var mapper = new TrackableListRedisMapper<SampleData>();
            var list = new TrackableList<SampleData>
            {
                new SampleData { Name = "A", Level = 1 },
                new SampleData { Name = "B", Level = 2 }
            };
            await mapper.CreateAsync(_fixture.Db, list, key);

            var loaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(2, loaded.Count);
            Assert.Equal("A", loaded[0].Name);
            Assert.Equal(2, loaded[1].Level);

            loaded.SetDefaultTrackerDeep();
            loaded.Add(new SampleData { Name = "C", Level = 3 });
            await mapper.SaveAsync(_fixture.Db, (TrackableListTracker<SampleData>)loaded.Tracker, key);

            var reloaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(3, reloaded.Count);
            Assert.Equal("C", reloaded[2].Name);
            Assert.Equal(3, reloaded[2].Level);
        }

        [Fact]
        public async Task Set_ClassValue_CreateAndLoad()
        {
            var key = _fixture.Key("class_set");
            var mapper = new TrackableSetRedisMapper<SampleData>();
            var set = new TrackableSet<SampleData>
            {
                new SampleData { Name = "X", Level = 1 },
                new SampleData { Name = "Y", Level = 2 }
            };
            await mapper.CreateAsync(_fixture.Db, set, key);

            var loaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal(2, loaded.Count);
            Assert.Equal(new[] { "X", "Y" }, loaded.Select(x => x.Name).OrderBy(n => n));
            Assert.Equal(3, loaded.Sum(x => x.Level));
        }

        [Fact]
        public async Task Container_ClassValues_CreateLoadSave()
        {
            var key = _fixture.Key("class_container");
            var mapper = new TrackableContainerRedisMapper<IClassContainer>();
            await mapper.CreateAsync(_fixture.Db, TestData.CreateSampleClassContainer(), key);

            var loaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal("Alpha", loaded.Items[1].Name);
            Assert.Equal(2, loaded.Items[2].Level);
            Assert.Equal(2, loaded.History.Count);
            Assert.Equal("H1", loaded.History[0].Name);

            loaded.SetDefaultTracker();
            loaded.Items[1] = new SampleData { Name = "AlphaX", Level = 11 };
            loaded.Items.Add(3, new SampleData { Name = "Gamma", Level = 3 });
            loaded.History.Add(new SampleData { Name = "H3", Level = 30 });
            await mapper.SaveAsync(_fixture.Db, loaded.Tracker, key);

            var reloaded = await mapper.LoadAsync(_fixture.Db, key);
            Assert.Equal("AlphaX", reloaded.Items[1].Name);
            Assert.Equal("Gamma", reloaded.Items[3].Name);
            Assert.Equal(3, reloaded.History.Count);
            Assert.Equal("H3", reloaded.History[2].Name);
        }
    }
}
