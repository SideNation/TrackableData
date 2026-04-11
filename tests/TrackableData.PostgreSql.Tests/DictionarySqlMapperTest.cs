using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TrackableData.PostgreSql.Tests
{
    public class DictionarySqlMapperTest : IClassFixture<PostgreSqlFixture>
    {
        private readonly PostgreSqlFixture _fixture;
        private readonly TrackableDictionarySqlMapper<int, string> _mapper;

        public DictionarySqlMapperTest(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
            _mapper = new TrackableDictionarySqlMapper<int, string>(
                PostgreSqlProvider.Instance,
                "TestDictionary",
                new ColumnDefinition("Key", typeof(int)),
                new ColumnDefinition("Value", typeof(string), 100),
                null);
        }

        [Fact]
        public async Task TestDictionary_CreateAndLoad()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var dict = new Dictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" },
                { 3, "Three" }
            };
            await _mapper.CreateAsync(_fixture.Connection, dict);

            var loaded = await _mapper.LoadAsync(_fixture.Connection);
            Assert.Equal(3, loaded.Count);
            Assert.Equal("One", loaded[1]);
            Assert.Equal("Two", loaded[2]);
            Assert.Equal("Three", loaded[3]);
        }

        [Fact]
        public async Task TestDictionary_Save_AddModifyRemove()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var dict = new TrackableDictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" },
                { 3, "Three" }
            };
            await _mapper.CreateAsync(_fixture.Connection, dict);

            dict.SetDefaultTrackerDeep();
            dict.Add(4, "Four");
            dict[1] = "OneModified";
            dict.Remove(3);

            await _mapper.SaveAsync(_fixture.Connection,
                (TrackableDictionaryTracker<int, string>)dict.Tracker);

            var loaded = await _mapper.LoadAsync(_fixture.Connection);
            Assert.Equal(3, loaded.Count);
            Assert.Equal("OneModified", loaded[1]);
            Assert.Equal("Two", loaded[2]);
            Assert.Equal("Four", loaded[4]);
        }

        [Fact]
        public async Task TestDictionary_Delete()
        {
            await _mapper.ResetTableAsync(_fixture.Connection);

            var dict = new Dictionary<int, string>
            {
                { 1, "One" },
                { 2, "Two" }
            };
            await _mapper.CreateAsync(_fixture.Connection, dict);

            await _mapper.DeleteAsync(_fixture.Connection);

            var loaded = await _mapper.LoadAsync(_fixture.Connection);
            Assert.Empty(loaded);
        }
    }
}
