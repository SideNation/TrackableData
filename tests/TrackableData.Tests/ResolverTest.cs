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

        // --- IsTrackable negative cases ---

        [Fact]
        public void TestTrackableResolver_IsTrackableDictionary_PlainType_False()
        {
            Assert.False(TrackableResolver.IsTrackableDictionary(typeof(string)));
            Assert.False(TrackableResolver.IsTrackableDictionary(typeof(int)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableList_PlainType_False()
        {
            Assert.False(TrackableResolver.IsTrackableList(typeof(string)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableSet_PlainType_False()
        {
            Assert.False(TrackableResolver.IsTrackableSet(typeof(string)));
        }

        // --- Generic CreateDefaultTracker ---

        [Fact]
        public void TestTrackerResolver_GenericGetDefaultTracker_Dictionary()
        {
            var type = TrackerResolver.GetDefaultTracker<TrackableDictionary<int, string>>();
            Assert.Equal(typeof(TrackableDictionaryTracker<int, string>), type);
        }

        [Fact]
        public void TestTrackerResolver_GenericCreateDefaultTracker_List()
        {
            var tracker = TrackerResolver.CreateDefaultTracker<TrackableList<string>>();
            Assert.NotNull(tracker);
            Assert.IsType<TrackableListTracker<string>>(tracker);
        }

        // --- Cross-type checks ---

        [Fact]
        public void TestTrackableResolver_IsTrackableDictionary_NotList()
        {
            Assert.False(TrackableResolver.IsTrackableDictionary(typeof(TrackableList<string>)));
            Assert.False(TrackableResolver.IsTrackableDictionary(typeof(TrackableSet<int>)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableList_NotDictionaryOrSet()
        {
            Assert.False(TrackableResolver.IsTrackableList(typeof(TrackableDictionary<int, string>)));
            Assert.False(TrackableResolver.IsTrackableList(typeof(TrackableSet<int>)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableSet_NotDictionaryOrList()
        {
            Assert.False(TrackableResolver.IsTrackableSet(typeof(TrackableDictionary<int, string>)));
            Assert.False(TrackableResolver.IsTrackableSet(typeof(TrackableList<string>)));
        }

        // --- ITrackablePoco / ITrackableContainer type checks ---

        [Fact]
        public void TestTrackableResolver_IsTrackablePoco_PlainType_False()
        {
            Assert.False(TrackableResolver.IsTrackablePoco(typeof(string)));
            Assert.False(TrackableResolver.IsTrackablePoco(typeof(TrackableDictionary<int, string>)));
        }

        [Fact]
        public void TestTrackableResolver_IsTrackableContainer_PlainType_False()
        {
            Assert.False(TrackableResolver.IsTrackableContainer(typeof(string)));
            Assert.False(TrackableResolver.IsTrackableContainer(typeof(TrackableList<string>)));
        }

        // --- GetPocoType / GetContainerType for non-poco types ---

        [Fact]
        public void TestTrackableResolver_GetPocoType_NonPoco_ReturnsNull()
        {
            Assert.Null(TrackableResolver.GetPocoType(typeof(string)));
            Assert.Null(TrackableResolver.GetPocoType(typeof(TrackableDictionary<int, string>)));
        }

        [Fact]
        public void TestTrackableResolver_GetContainerType_NonContainer_ReturnsNull()
        {
            Assert.Null(TrackableResolver.GetContainerType(typeof(string)));
            Assert.Null(TrackableResolver.GetContainerType(typeof(TrackableList<string>)));
        }

        [Fact]
        public void TestTrackableResolver_GetPocoTrackerType_NonPoco_ReturnsNull()
        {
            Assert.Null(TrackableResolver.GetPocoTrackerType(typeof(string)));
        }

        [Fact]
        public void TestTrackableResolver_GetContainerTrackerType_NonContainer_ReturnsNull()
        {
            Assert.Null(TrackableResolver.GetContainerTrackerType(typeof(string)));
        }
    }
}
