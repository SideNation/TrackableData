using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TrackableData.Tests
{
    public class ExtensionsTest
    {
        [Fact]
        public void TestSetDefaultTracker_Dictionary()
        {
            var dict = new TrackableDictionary<int, string>();
            dict.SetDefaultTracker();

            Assert.NotNull(dict.Tracker);
            Assert.IsType<TrackableDictionaryTracker<int, string>>(dict.Tracker);
        }

        [Fact]
        public void TestSetDefaultTracker_List()
        {
            var list = new TrackableList<string>();
            list.SetDefaultTracker();

            Assert.NotNull(list.Tracker);
            Assert.IsType<TrackableListTracker<string>>(list.Tracker);
        }

        [Fact]
        public void TestSetDefaultTracker_Set()
        {
            var set = new TrackableSet<int>();
            set.SetDefaultTracker();

            Assert.NotNull(set.Tracker);
            Assert.IsType<TrackableSetTracker<int>>(set.Tracker);
        }

        [Fact]
        public void TestClearTrackerDeep_ClearsAllChanges()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();
            dict[1] = "Modified";

            Assert.True(dict.Changed);
            dict.ClearTrackerDeep();
            Assert.False(dict.Changed);
        }

        [Fact]
        public void TestRollback_RevertsChanges()
        {
            var dict = new TrackableDictionary<int, string>()
            {
                { 1, "One" },
                { 2, "Two" }
            };
            dict.SetDefaultTrackerDeep();

            dict[1] = "OneModified";
            dict.Remove(2);
            dict[3] = "Three";

            dict.Rollback();

            Assert.Equal("One", dict[1]);
            Assert.Equal("Two", dict[2]);
            Assert.False(dict.ContainsKey(3));
            Assert.False(dict.Changed);
        }

        [Fact]
        public void TestRollback_List_RevertsChanges()
        {
            var list = new TrackableList<string>() { "One", "Two", "Three" };
            list.SetDefaultTrackerDeep();

            list[0] = "Modified";
            list.Add("Four");

            list.Rollback();

            Assert.Equal(new List<string> { "One", "Two", "Three" }, list.ToList());
            Assert.False(list.Changed);
        }

        [Fact]
        public void TestGetChangedTrackablesWithPath_ReturnsChanged()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();
            dict[1] = "Modified";

            var changed = dict.GetChangedTrackablesWithPath().ToList();
            Assert.Single(changed);
            Assert.Equal("", changed[0].Key);
        }

        [Fact]
        public void TestGetChangedTrackersWithPath_ReturnsTrackers()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();
            dict[1] = "Modified";

            var trackers = dict.GetChangedTrackersWithPath().ToList();
            Assert.Single(trackers);
            Assert.IsType<TrackableDictionaryTracker<int, string>>(trackers[0].Value);
        }
    }
}
