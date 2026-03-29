using System;
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

        // --- Rollback edge cases ---

        [Fact]
        public void TestRollback_NoChanges_DoesNothing()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();

            dict.Rollback();

            Assert.Equal("One", dict[1]);
            Assert.False(dict.Changed);
        }

        [Fact]
        public void TestRollback_WithoutTracker_Throws()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };

            Assert.Throws<ArgumentException>(() => dict.Rollback());
        }

        [Fact]
        public void TestRollback_Set_RevertsChanges()
        {
            var set = new TrackableSet<int>() { 1, 2, 3 };
            set.SetDefaultTrackerDeep();

            set.Add(4);
            set.Remove(1);

            set.Rollback();

            Assert.Equal(new[] { 1, 2, 3 }, set.OrderBy(x => x).ToArray());
            Assert.False(set.Changed);
        }

        // --- SetDefaultTrackerDeep ---

        [Fact]
        public void TestSetDefaultTrackerDeep_SetsTrackerOnSelf()
        {
            var dict = new TrackableDictionary<int, string>();
            dict.SetDefaultTrackerDeep();

            Assert.NotNull(dict.Tracker);
        }

        // --- ClearTrackerDeep ---

        [Fact]
        public void TestClearTrackerDeep_WithNoTracker_DoesNotThrow()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.ClearTrackerDeep();
            // No exception
        }

        [Fact]
        public void TestClearTrackerDeep_ClearsMultipleCollections()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();
            dict[1] = "Modified";

            var list = new TrackableList<string>() { "A" };
            list.SetDefaultTrackerDeep();
            list.Add("B");

            Assert.True(dict.Changed);
            Assert.True(list.Changed);

            dict.ClearTrackerDeep();
            list.ClearTrackerDeep();

            Assert.False(dict.Changed);
            Assert.False(list.Changed);
        }

        // --- GetChangedTrackablesWithPath ---

        [Fact]
        public void TestGetChangedTrackablesWithPath_NoChanges_ReturnsEmpty()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();

            var changed = dict.GetChangedTrackablesWithPath().ToList();
            Assert.Empty(changed);
        }

        [Fact]
        public void TestGetChangedTrackablesWithPath_WithParentPath_PrefixesPath()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();
            dict[1] = "Modified";

            var changed = dict.GetChangedTrackablesWithPath("parent").ToList();
            Assert.Single(changed);
            Assert.Equal("parent", changed[0].Key);
        }

        // --- GetChangedTrackersWithPath ---

        [Fact]
        public void TestGetChangedTrackersWithPath_NoChanges_ReturnsEmpty()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" } };
            dict.SetDefaultTrackerDeep();

            var trackers = dict.GetChangedTrackersWithPath().ToList();
            Assert.Empty(trackers);
        }

        // --- ApplyTo extension (path-based) ---

        [Fact]
        public void TestApplyToExtension_AppliesTrackersByPath()
        {
            var source = new TrackableDictionary<int, string>() { { 1, "One" }, { 2, "Two" } };
            source.SetDefaultTrackerDeep();
            source[1] = "OneModified";
            source.Remove(2);

            var pathAndTrackers = source.GetChangedTrackersWithPath().ToList();

            var target = new TrackableDictionary<int, string>() { { 1, "One" }, { 2, "Two" } };
            pathAndTrackers.ApplyTo(target);

            Assert.Equal("OneModified", target[1]);
            Assert.False(target.ContainsKey(2));
        }

        // --- GetTrackableByPath ---

        [Fact]
        public void TestGetTrackableByPath_EmptyPath_ReturnsSelf()
        {
            var dict = new TrackableDictionary<int, string>();
            var result = dict.GetTrackableByPath("");
            Assert.Same(dict, result);
        }

        [Fact]
        public void TestGetTrackableByPath_NullPath_ReturnsSelf()
        {
            var dict = new TrackableDictionary<int, string>();
            var result = dict.GetTrackableByPath(null!);
            Assert.Same(dict, result);
        }

        // --- RollbackDeep ---

        [Fact]
        public void TestRollbackDeep_RevertsAllChanges()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" }, { 2, "Two" } };
            dict.SetDefaultTrackerDeep();
            dict[1] = "Modified";
            dict[3] = "Three";

            dict.RollbackDeep();

            Assert.Equal("One", dict[1]);
            Assert.Equal("Two", dict[2]);
            Assert.False(dict.ContainsKey(3));
            Assert.False(dict.Changed);
        }
    }
}
