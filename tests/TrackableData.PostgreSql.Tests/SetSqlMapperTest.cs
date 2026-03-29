using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.PostgreSql.Tests
{
    public class SetSqlMapperTest : IClassFixture<PostgreSqlFixture>
    {
        private readonly PostgreSqlFixture _fixture;
        private readonly TrackableSetSqlMapper<string> _mapper;

        public SetSqlMapperTest(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableSetSqlMapper<string>(
                PostgreSqlProvider.Instance,
                "TestSet",
                new ColumnDefinition("Value", typeof(string), 100));
        }

        [Fact]
        public async Task TestSet_CreateAndLoad()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var set = new HashSet<string> { "Alpha", "Beta", "Gamma" };
            await _mapper.CreateAsync(_fixture.Connection, set);

            var loaded = await _mapper.LoadAsync(_fixture.Connection);
            Assert.Equal(3, loaded.Count);
            Assert.Contains("Alpha", loaded);
            Assert.Contains("Beta", loaded);
            Assert.Contains("Gamma", loaded);
        }

        [Fact]
        public async Task TestSet_Save_AddAndRemove()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var set = new TrackableSet<string> { "Alpha", "Beta", "Gamma" };
            await _mapper.CreateAsync(_fixture.Connection, set);

            set.SetDefaultTrackerDeep();
            set.Add("Delta");
            set.Remove("Beta");

            await _mapper.SaveAsync(_fixture.Connection,
                (TrackableSetTracker<string>)set.Tracker);

            var loaded = await _mapper.LoadAsync(_fixture.Connection);
            Assert.Equal(3, loaded.Count);
            Assert.Contains("Alpha", loaded);
            Assert.Contains("Gamma", loaded);
            Assert.Contains("Delta", loaded);
            Assert.DoesNotContain("Beta", loaded);
        }
    }
}
