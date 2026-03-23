using System;
using Xunit;

namespace TrackableData.Tests
{
    public class ResolverTest
    {
        [Fact]
        public void TestTrackerResolver_Dictionary_ReturnsCorrectType()
        {
            var trackerType = TrackerResolver.GetDefaultTracker(typeof(TrackableDictionary<int, string>));
            Assert.Equal(typeof(TrackableDictionaryTracker<int, string>), trackerType);
        }

        [Fact]
        public void TestTrackerResolver_List_ReturnsCorrectType()
        {
            var trackerType = TrackerResolver.GetDefaultTracker(typeof(TrackableList<string>));
            Assert.Equal(typeof(TrackableListTracker<string>), trackerType);
        }

        [Fact]
        public void TestTrackerResolver_Set_ReturnsCorrectType()
        {
            var trackerType = TrackerResolver.GetDefaultTracker(typeof(TrackableSet<int>));
            Assert.Equal(typeof(TrackableSetTracker<int>), trackerType);
        }

        [Fact]
        public void TestTrackerResolver_UnknownType_ReturnsNull()
        {
            var trackerType = TrackerResolver.GetDefaultTracker(typeof(string));
            Assert.Null(trackerType);
        }

        [Fact]
        public void TestTrackerResolver_CreateDefaultTracker_Dictionary()
        {
            var tracker = TrackerResolver.CreateDefaultTracker(typeof(TrackableDictionary<int, string>));
            Assert.NotNull(tracker);
            Assert.IsType<TrackableDictionaryTracker<int, string>>(tracker);
        }

        [Fact]
        public void TestTrackerResolver_CreateDefaultTracker_List()
        {
            var tracker = TrackerResolver.CreateDefaultTracker(typeof(TrackableList<string>));
            Assert.NotNull(tracker);
            Assert.IsType<TrackableListTracker<string>>(tracker);
        }

        [Fact]
        public void TestTrackerResolver_CreateDefaultTracker_Set()
        {
            var tracker = TrackerResolver.CreateDefaultTracker(typeof(TrackableSet<int>));
            Assert.NotNull(tracker);
            Assert.IsType<TrackableSetTracker<int>>(tracker);
        }

        [Fact]
        public void TestTrackerResolver_CreateDefaultTracker_UnknownType_ReturnsNull()
        {
            var tracker = TrackerResolver.CreateDefaultTracker(typeof(string));
            Assert.Null(tracker);
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableDictionary()
        {
            Assert.True(TrackableResolver.IsTrackableDictionary(typeof(TrackableDictionary<int, string>)));
            Assert.False(TrackableResolver.IsTrackableDictionary(typeof(TrackableList<string>)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableList()
        {
            Assert.True(TrackableResolver.IsTrackableList(typeof(TrackableList<string>)));
            Assert.False(TrackableResolver.IsTrackableList(typeof(TrackableSet<int>)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableSet()
        {
            Assert.True(TrackableResolver.IsTrackableSet(typeof(TrackableSet<int>)));
            Assert.False(TrackableResolver.IsTrackableSet(typeof(TrackableDictionary<int, string>)));
        }
    }
}
