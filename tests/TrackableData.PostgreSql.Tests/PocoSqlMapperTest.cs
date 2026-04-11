using System.Threading.Tasks;
using Xunit;

namespace TrackableData.PostgreSql.Tests
{
    public class PocoSqlMapperTest : IClassFixture<PostgreSqlFixture>
    {
        private readonly PostgreSqlFixture _fixture;
        private readonly TrackablePocoSqlMapper<ITestPerson> _mapper;

        public PocoSqlMapperTest(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackablePocoSqlMapper<ITestPerson>(
                PostgreSqlProvider.Instance,
                "TestPerson");
        }

        [Fact]
        public async Task TestPoco_CreateAndLoad()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var person = new TrackableTestPerson { Id = 1, Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(_fixture.Connection, person);

            var loaded = await _mapper.LoadAsync(_fixture.Connection, 1);
            Assert.NotNull(loaded);
            Assert.Equal("Alice", loaded.Name);
            Assert.Equal(30, loaded.Age);
        }

        [Fact]
        public async Task TestPoco_Save()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var person = new TrackableTestPerson { Id = 1, Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(_fixture.Connection, person);

            person.SetDefaultTrackerDeep();
            person.Name = "Bob";
            person.Age = 25;

            await _mapper.SaveAsync(_fixture.Connection,
                (TrackablePocoTracker<ITestPerson>)person.Tracker, 1);

            var loaded = await _mapper.LoadAsync(_fixture.Connection, 1);
            Assert.Equal("Bob", loaded.Name);
            Assert.Equal(25, loaded.Age);
        }

        [Fact]
        public async Task TestPoco_Delete()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var person = new TrackableTestPerson { Id = 1, Name = "Alice", Age = 30 };
            await _mapper.CreateAsync(_fixture.Connection, person);

            var deleted = await _mapper.DeleteAsync(_fixture.Connection, 1);
            Assert.Equal(1, deleted);

            var loaded = await _mapper.LoadAsync(_fixture.Connection, 1);
            Assert.Null(loaded);
        }
    }
}
