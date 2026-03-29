using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TrackableData.Tests
{
    public class SetTest
    {
        private TrackableSet<int> CreateTestSet()
        {
            return new TrackableSet<int>() { 1, 2, 3 };
        }

        private TrackableSet<int> CreateTestSetWithTracker()
        {
            var set = CreateTestSet();
            set.SetDefaultTrackerDeep();
            return set;
        }

        [Fact]
        public void TestSet_UnionWith_Work()
        {
            var evenNumbers = new TrackableSet<int>() { 0, 2, 4, 6, 8 };
            var oddNumbers = new TrackableSet<int>() { 1, 3, 5, 7, 9 };

            evenNumbers.SetDefaultTrackerDeep();
            evenNumbers.UnionWith(oddNumbers);

            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                         evenNumbers.OrderBy(x => x));

            var tracker = (TrackableSetTracker<int>)evenNumbers.Tracker;
            Assert.Equal(new[] { 1, 3, 5, 7, 9 },
                         tracker.AddValues.OrderBy(x => x));
            Assert.Empty(tracker.RemoveValues);
        }

        [Fact]
        public void TestSet_IntersectWith_Work()
        {
            var lowNumbers = new TrackableSet<int>() { 0, 1, 2, 3, 4, 5 };
            var highNumbers = new TrackableSet<int>() { 3, 4, 5, 6, 7, 8, 9 };

            lowNumbers.SetDefaultTrackerDeep();
            lowNumbers.IntersectWith(highNumbers);

            Assert.Equal(new[] { 3, 4, 5 },
                         lowNumbers.OrderBy(x => x));

            var tracker = (TrackableSetTracker<int>)lowNumbers.Tracker;
            Assert.Empty(tracker.AddValues);
            Assert.Equal(new[] { 0, 1, 2 },
                         tracker.RemoveValues.OrderBy(x => x));
        }

        [Fact]
        public void TestSet_ExceptWith_Work()
        {
            var lowNumbers = new TrackableSet<int>() { 0, 1, 2, 3, 4, 5 };
            var highNumbers = new TrackableSet<int>() { 3, 4, 5, 6, 7, 8, 9 };

            lowNumbers.SetDefaultTrackerDeep();
            lowNumbers.ExceptWith(highNumbers);

            Assert.Equal(new[] { 0, 1, 2 },
                         lowNumbers.OrderBy(x => x));

            var tracker = (TrackableSetTracker<int>)lowNumbers.Tracker;
            Assert.Empty(tracker.AddValues);
            Assert.Equal(new[] { 3, 4, 5 },
                         tracker.RemoveValues.OrderBy(x => x));
        }

        [Fact]
        public void TestSet_SymmetricExceptWith_Work()
        {
            var lowNumbers = new TrackableSet<int>() { 0, 1, 2, 3, 4, 5 };
            var highNumbers = new TrackableSet<int>() { 3, 4, 5, 6, 7, 8, 9 };

            lowNumbers.SetDefaultTrackerDeep();
            lowNumbers.SymmetricExceptWith(highNumbers);

            Assert.Equal(new[] { 0, 1, 2, 6, 7, 8, 9 },
                         lowNumbers.OrderBy(x => x));

            var tracker = (TrackableSetTracker<int>)lowNumbers.Tracker;
            Assert.Equal(new[] { 6, 7, 8, 9 },
                         tracker.AddValues.OrderBy(x => x));
            Assert.Equal(new[] { 3, 4, 5 },
                         tracker.RemoveValues.OrderBy(x => x));
        }

        [Fact]
        public void TestSet_Tracking_Work()
        {
            var set = CreateTestSetWithTracker();
            set.Remove(2);
            set.Add(4);

            var changeMap = ((TrackableSetTracker<int>)set.Tracker).ChangeMap;
            Assert.Equal(2, changeMap.Count);

            Assert.Equal(TrackableSetOperation.Remove, changeMap[2]);
            Assert.Equal(TrackableSetOperation.Add, changeMap[4]);
        }

        [Fact]
        public void TestSet_HasChangedSetEvent_Work()
        {
            var changed = false;
            var set = CreateTestSetWithTracker();
            set.Tracker.HasChangeSet += _ => { changed = true; };
            set.Add(4);
            Assert.True(changed);
        }

        [Fact]
        public void TestSet_ApplyToTrackable_Work()
        {
            var set = CreateTestSetWithTracker();
            set.Remove(2);
            set.Add(4);

            var set2 = CreateTestSet();
            set.Tracker.ApplyTo(set2);

            Assert.Equal(new[] { 1, 3, 4 }, set2.OrderBy(v => v));
        }

        [Fact]
        public void TestSet_ApplyToTracker_Work()
        {
            var set = CreateTestSetWithTracker();
            set.Remove(2);
            set.Add(4);

            var tracker2 = new TrackableSetTracker<int>();
            set.Tracker.ApplyTo(tracker2);

            var set2 = CreateTestSet();
            tracker2.ApplyTo(set2);

            Assert.Equal(new[] { 1, 3, 4 }, set2.OrderBy(v => v));
        }

        [Fact]
        public void TestSet_RollbackToTrackable_Work()
        {
            var set = CreateTestSetWithTracker();
            set.Remove(2);
            set.Add(4);

            var set2 = CreateTestSet();
            set.Tracker.ApplyTo(set2);
            set.Tracker.RollbackTo(set2);

            Assert.Equal(new[] { 1, 2, 3 }, set2.OrderBy(v => v));
        }

        [Fact]
        public void TestSet_RollbackToTracker_Work()
        {
            var set = CreateTestSetWithTracker();
            set.Remove(2);
            set.Add(4);

            var tracker2 = new TrackableSetTracker<int>();
            set.Tracker.ApplyTo(tracker2);
            set.Tracker.RollbackTo(tracker2);

            var set2 = CreateTestSet();
            tracker2.ApplyTo(set2);

            Assert.Equal(new[] { 1, 2, 3 }, set2.OrderBy(v => v));
        }

        [Fact]
        public void TestSet_Clone_Work()
        {
            var a = CreateTestSetWithTracker();
            var b = a.Clone();

            Assert.Null(b.Tracker);
            Assert.False(ReferenceEquals(a._set, b._set));
            Assert.Equal(a._set, b._set);
        }

        [Fact]
        public void TestSet_AddRemoveSameItem_CancelsOut()
        {
            var set = CreateTestSetWithTracker();
            set.Remove(2);
            set.Add(2);

            var changeMap = ((TrackableSetTracker<int>)set.Tracker).ChangeMap;
            Assert.Empty(changeMap);
        }

        [Fact]
        public void TestSet_Clear_TracksAllRemoves()
        {
            var set = CreateTestSetWithTracker();
            set.Clear();

            var changeMap = ((TrackableSetTracker<int>)set.Tracker).ChangeMap;
            Assert.Equal(3, changeMap.Count);
            Assert.All(changeMap.Values, op => Assert.Equal(TrackableSetOperation.Remove, op));
        }

        // --- Exception scenarios ---

        [Fact]
        public void TestSetTracker_AddAfterAdd_Throws()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(1);
            Assert.Throws<InvalidOperationException>(() => tracker.TrackAdd(1));
        }

        [Fact]
        public void TestSetTracker_RemoveAfterRemove_Throws()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackRemove(1);
            Assert.Throws<InvalidOperationException>(() => tracker.TrackRemove(1));
        }

        // --- Enumerable helpers ---

        [Fact]
        public void TestSetTracker_AddValues_ReturnsOnlyAdds()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(10);
            tracker.TrackAdd(20);
            tracker.TrackRemove(5);

            var adds = new List<int>(tracker.AddValues);
            Assert.Equal(2, adds.Count);
            Assert.Contains(10, adds);
            Assert.Contains(20, adds);
        }

        [Fact]
        public void TestSetTracker_RemoveValues_ReturnsOnlyRemoves()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(10);
            tracker.TrackRemove(1);
            tracker.TrackRemove(2);

            var removes = new List<int>(tracker.RemoveValues);
            Assert.Equal(2, removes.Count);
            Assert.Contains(1, removes);
            Assert.Contains(2, removes);
        }

        // --- Null guard ---

        [Fact]
        public void TestSetTracker_ApplyTo_NullTrackable_Throws()
        {
            var tracker = new TrackableSetTracker<int>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((ICollection<int>)null!));
        }

        [Fact]
        public void TestSetTracker_ApplyTo_NullTracker_Throws()
        {
            var tracker = new TrackableSetTracker<int>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((TrackableSetTracker<int>)null!));
        }

        [Fact]
        public void TestSetTracker_RollbackTo_NullTrackable_Throws()
        {
            var tracker = new TrackableSetTracker<int>();
            Assert.Throws<ArgumentNullException>(() => tracker.RollbackTo((ICollection<int>)null!));
        }

        // --- HasChangeSet event ---

        [Fact]
        public void TestSetTracker_HasChangeSet_NotFiredOnSubsequentChanges()
        {
            var callCount = 0;
            var tracker = new TrackableSetTracker<int>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackAdd(1);
            tracker.TrackAdd(2);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void TestSetTracker_HasChangeSet_FiredAgainAfterClearAndNewChange()
        {
            var callCount = 0;
            var tracker = new TrackableSetTracker<int>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackAdd(1);
            tracker.Clear();
            tracker.TrackAdd(2);
            Assert.Equal(2, callCount);
        }

        // --- ToString ---

        [Fact]
        public void TestSetTracker_ToString_ShowsOperations()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(10);
            tracker.TrackRemove(5);

            var str = tracker.ToString();
            Assert.Contains("+10", str);
            Assert.Contains("-5", str);
        }

        // --- Add duplicate item ---

        [Fact]
        public void TestSet_AddDuplicate_ReturnsFalse_NoTracking()
        {
            var set = CreateTestSetWithTracker();
            var added = set.Add(1);

            Assert.False(added);
            Assert.False(set.Tracker.HasChange);
        }

        [Fact]
        public void TestSet_RemoveNonExistent_ReturnsFalse_NoTracking()
        {
            var set = CreateTestSetWithTracker();
            var removed = set.Remove(99);

            Assert.False(removed);
            Assert.False(set.Tracker.HasChange);
        }

        // --- Constructor variants ---

        [Fact]
        public void TestSet_CopyConstructor_CopiesData()
        {
            var original = CreateTestSet();
            var copy = new TrackableSet<int>(original);

            Assert.Equal(3, copy.Count);
            Assert.Contains(1, copy);
            Assert.Null(copy.Tracker);
        }

        [Fact]
        public void TestSet_EnumerableConstructor_CopiesData()
        {
            var source = new[] { 10, 20, 30 };
            var set = new TrackableSet<int>(source);
            Assert.Equal(3, set.Count);
            Assert.Contains(20, set);
        }

        // --- No tracker ---

        [Fact]
        public void TestSet_NoTracker_NoException()
        {
            var set = CreateTestSet();
            set.Add(4);
            set.Remove(1);
            set.Clear();
            Assert.False(set.Changed);
        }

        // --- Set operations edge cases ---

        [Fact]
        public void TestSet_ExceptWithSelf_ClearsAll()
        {
            var set = CreateTestSetWithTracker();
            set.ExceptWith(set);

            Assert.Empty(set);
            var changeMap = ((TrackableSetTracker<int>)set.Tracker).ChangeMap;
            Assert.Equal(3, changeMap.Count);
        }

        [Fact]
        public void TestSet_SymmetricExceptWithSelf_ClearsAll()
        {
            var set = CreateTestSetWithTracker();
            set.SymmetricExceptWith(set);

            Assert.Empty(set);
        }

        [Fact]
        public void TestSet_IntersectWithSelf_NoChange()
        {
            var set = CreateTestSetWithTracker();
            set.IntersectWith(set);

            Assert.Equal(3, set.Count);
            Assert.False(set.Tracker.HasChange);
        }

        [Fact]
        public void TestSet_UnionWith_NoNewItems_NoChange()
        {
            var set = CreateTestSetWithTracker();
            set.UnionWith(new[] { 1, 2, 3 });

            Assert.False(set.Tracker.HasChange);
        }

        [Fact]
        public void TestSet_IntersectWith_EmptySet_RemovesAll()
        {
            var set = CreateTestSetWithTracker();
            set.IntersectWith(new int[0]);

            Assert.Empty(set);
            var changeMap = ((TrackableSetTracker<int>)set.Tracker).ChangeMap;
            Assert.Equal(3, changeMap.Count);
        }

        [Fact]
        public void TestSet_SymmetricExceptWith_OnEmptySet_UnionsAll()
        {
            var set = new TrackableSet<int>();
            set.SetDefaultTrackerDeep();
            set.SymmetricExceptWith(new[] { 1, 2, 3 });

            Assert.Equal(3, set.Count);
            var changeMap = ((TrackableSetTracker<int>)set.Tracker).ChangeMap;
            Assert.Equal(3, changeMap.Count);
            Assert.All(changeMap.Values, op => Assert.Equal(TrackableSetOperation.Add, op));
        }

        // --- Rollback ---

        [Fact]
        public void TestSet_Rollback_RevertsChanges()
        {
            var set = CreateTestSetWithTracker();
            set.Add(4);
            set.Remove(1);

            set.Rollback();

            Assert.Equal(new[] { 1, 2, 3 }, new List<int>(set).OrderBy(x => x).ToArray());
            Assert.False(set.Changed);
        }
    }
}
