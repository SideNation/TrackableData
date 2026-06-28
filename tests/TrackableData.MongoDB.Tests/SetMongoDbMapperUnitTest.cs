using System.Linq;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class SetMongoDbMapperUnitTest
    {
        private readonly TrackableSetMongoDbMapper<int> _mapper =
            new TrackableSetMongoDbMapper<int>();

        [Fact]
        public void ConvertToBsonArray_Basic()
        {
            var set = new TrackableSet<int> { 1, 2, 3 };
            var bson = _mapper.ConvertToBsonArray(set);

            Assert.Equal(3, bson.Count);
        }

        [Fact]
        public void ConvertToTrackableSet_RoundTrip()
        {
            var set = new TrackableSet<int> { 1, 2, 3 };
            var result = _mapper.ConvertToTrackableSet(_mapper.ConvertToBsonArray(set));

            Assert.Equal(new[] { 1, 2, 3 }, result.OrderBy(v => v));
        }

        [Fact]
        public void BuildUpdatesForSave_AddOnly_SingleUpdate()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(1);
            tracker.TrackAdd(2);

            var updates = _mapper.BuildUpdatesForSave(tracker, "data");
            Assert.Single(updates);
        }

        [Fact]
        public void BuildUpdatesForSave_AddAndRemove_TwoUpdates()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(1);
            tracker.TrackRemove(2);

            var updates = _mapper.BuildUpdatesForSave(tracker, "data");
            Assert.Equal(2, updates.Count);
        }

        [Fact]
        public void BuildUpdatesForSave_Empty_ReturnsEmpty()
        {
            var updates = _mapper.BuildUpdatesForSave(new TrackableSetTracker<int>(), "data");
            Assert.Empty(updates);
        }

        [Fact]
        public void Constructor_WithLogger()
        {
            var mapper = new TrackableSetMongoDbMapper<string>(new TestTrackableLogger());
            Assert.NotNull(mapper);
        }
    }
}
