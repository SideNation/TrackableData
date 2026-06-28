using System;
using System.Collections.Generic;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class ListMongoDbMapperUnitTest
    {
        private readonly TrackableListMongoDbMapper<string> _mapper =
            new TrackableListMongoDbMapper<string>();

        [Fact]
        public void ConvertToBsonArray_Basic()
        {
            var list = new TrackableList<string> { "one", "two", "three" };
            var bson = _mapper.ConvertToBsonArray(list);

            Assert.Equal(3, bson.Count);
            Assert.Equal("one", bson[0].AsString);
            Assert.Equal("three", bson[2].AsString);
        }

        [Fact]
        public void ConvertToBsonArray_Empty()
        {
            var bson = _mapper.ConvertToBsonArray(new TrackableList<string>());
            Assert.Empty(bson);
        }

        [Fact]
        public void ConvertToBsonArray_ClassValue()
        {
            var mapper = new TrackableListMongoDbMapper<BsonValueMapperTestClass>();
            var list = new TrackableList<BsonValueMapperTestClass>
            {
                new BsonValueMapperTestClass { Name = "Alpha", Level = 3 }
            };
            var bson = mapper.ConvertToBsonArray(list);

            Assert.True(bson[0].IsBsonDocument);
            Assert.Equal("Alpha", bson[0].AsBsonDocument["Name"].AsString);
            Assert.Equal(3, bson[0].AsBsonDocument["Level"].AsInt32);
        }

        [Fact]
        public void ConvertToTrackableList_RoundTrip()
        {
            var list = new TrackableList<string> { "a", "b" };
            var result = _mapper.ConvertToTrackableList(_mapper.ConvertToBsonArray(list));

            Assert.Equal(new List<string> { "a", "b" }, result);
        }

        [Fact]
        public void BuildUpdatesForSave_PushBack_SingleUpdate()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackPushBack("x");

            var updates = _mapper.BuildUpdatesForSave(tracker, "items");
            Assert.Single(updates);
        }

        [Fact]
        public void BuildUpdatesForSave_MultipleOps_OneUpdatePerChange()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackPushBack("x");
            tracker.TrackModify(0, "x", "y");
            tracker.TrackPopBack("y");

            var updates = _mapper.BuildUpdatesForSave(tracker, "items");
            Assert.Equal(3, updates.Count);
        }

        [Fact]
        public void BuildUpdatesForSave_Empty_ReturnsEmpty()
        {
            var updates = _mapper.BuildUpdatesForSave(new TrackableListTracker<string>(), "items");
            Assert.Empty(updates);
        }

        [Fact]
        public void BuildUpdatesForSave_Remove_Throws()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackRemove(0, "x");

            Assert.Throws<NotSupportedException>(() => _mapper.BuildUpdatesForSave(tracker, "items"));
        }

        [Fact]
        public void Constructor_WithLogger()
        {
            var mapper = new TrackableListMongoDbMapper<int>(new TestTrackableLogger());
            Assert.NotNull(mapper);
        }
    }
}
