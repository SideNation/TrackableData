using System.Collections.Generic;
using MongoDB.Bson;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class DictionaryMongoDbMapperUnitTest
    {
        private readonly TrackableDictionaryMongoDbMapper<string, string> _mapper =
            new TrackableDictionaryMongoDbMapper<string, string>();

        [Fact]
        public void ConvertToBsonDocument_BasicDictionary()
        {
            var dict = new Dictionary<string, string> { { "a", "one" }, { "b", "two" } };
            var bson = _mapper.ConvertToBsonDocument(dict);

            Assert.Equal(2, bson.ElementCount);
            Assert.True(bson.Contains("a"));
            Assert.True(bson.Contains("b"));
        }

        [Fact]
        public void ConvertToBsonDocument_EmptyDictionary()
        {
            var dict = new Dictionary<string, string>();
            var bson = _mapper.ConvertToBsonDocument(dict);

            Assert.Equal(0, bson.ElementCount);
        }

        [Fact]
        public void ConvertToTrackableDictionary_BasicDocument()
        {
            var doc = new BsonDocument { { "x", "hello" }, { "y", "world" } };
            var dict = _mapper.ConvertToTrackableDictionary(doc);

            Assert.Equal(2, dict.Count);
            Assert.Equal("hello", dict["x"]);
            Assert.Equal("world", dict["y"]);
        }

        [Fact]
        public void ConvertToTrackableDictionary_SkipsIdField_ForIntKey()
        {
            var intMapper = new TrackableDictionaryMongoDbMapper<int, string>();
            var doc = new BsonDocument { { "_id", "some_id" }, { "1", "hello" } };
            var dict = intMapper.ConvertToTrackableDictionary(doc);

            Assert.Single(dict);
            Assert.Equal("hello", dict[1]);
        }

        [Fact]
        public void ConvertToTrackableDictionary_EmptyDocument()
        {
            var doc = new BsonDocument();
            var dict = _mapper.ConvertToTrackableDictionary(doc);

            Assert.Empty(dict);
        }

        [Fact]
        public void BuildUpdatesForSave_AddAndModify()
        {
            var tracker = new TrackableDictionaryTracker<string, string>();
            tracker.TrackAdd("new_key", "new_value");
            tracker.TrackModify("existing", "old", "updated");

            var update = _mapper.BuildUpdatesForSave(null, tracker);
            Assert.NotNull(update);
        }

        [Fact]
        public void BuildUpdatesForSave_RemoveKey()
        {
            var tracker = new TrackableDictionaryTracker<string, string>();
            tracker.TrackRemove("key", "value");

            var update = _mapper.BuildUpdatesForSave(null, tracker);
            Assert.NotNull(update);
        }

        [Fact]
        public void BuildUpdatesForSave_EmptyTracker_ReturnsNull()
        {
            var tracker = new TrackableDictionaryTracker<string, string>();
            var update = _mapper.BuildUpdatesForSave(null, tracker);
            Assert.Null(update);
        }

        [Fact]
        public void DefaultConstructor_UsesNullLogger()
        {
            var mapper = new TrackableDictionaryMongoDbMapper<int, string>();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void Constructor_WithLogger()
        {
            var logger = new TestTrackableLogger();
            var mapper = new TrackableDictionaryMongoDbMapper<int, string>(logger);
            Assert.NotNull(mapper);
        }
    }

    internal class TestTrackableLogger : ITrackableLogger
    {
        public int CallCount;
        public void LogDebug(string message, params object[] args) => CallCount++;
    }
}
