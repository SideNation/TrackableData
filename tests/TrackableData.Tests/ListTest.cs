using System;
using System.Collections.Generic;
using Xunit;

namespace TrackableData.Tests
{
    public class ListTest
    {
        private TrackableList<string> CreateTestList()
        {
            return new TrackableList<string>()
            {
                "One",
                "Two",
                "Three"
            };
        }

        private TrackableList<string> CreateTestListWithTracker()
        {
            var list = CreateTestList();
            list.SetDefaultTrackerDeep();
            return list;
        }

        private List<string> GetInitialList()
        {
            return new List<string>(CreateTestList());
        }

        private void ModifyListForTest(IList<string> list)
        {
            list[0] = "OneModified";
            list.RemoveAt(1);
            list.Insert(1, "TwoInserted");
            list.Insert(0, "Zero");
            list.RemoveAt(0);
            list.Insert(0, "ZeroAgain");
            list.Insert(4, "Four");
            list.RemoveAt(4);
            list.Insert(4, "FourAgain");
        }

        private List<string> GetModifiedList()
        {
            var list = GetInitialList();
            ModifyListForTest(list);
            return list;
        }

        [Fact]
        public void TestList_Tracking_Work()
        {
            var list = CreateTestListWithTracker();
            list[0] = "OneModified";
            list.RemoveAt(1);
            list.Insert(1, "TwoInserted");

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Equal(3, changeList.Count);

            var change0 = changeList[0];
            Assert.Equal(TrackableListOperation.Modify, change0.Operation);
            Assert.Equal(0, change0.Index);
            Assert.Equal("One", change0.OldValue);
            Assert.Equal("OneModified", change0.NewValue);

            var change1 = changeList[1];
            Assert.Equal(TrackableListOperation.Remove, change1.Operation);
            Assert.Equal(1, change1.Index);
            Assert.Equal("Two", change1.OldValue);
            Assert.Null(change1.NewValue);

            var change2 = changeList[2];
            Assert.Equal(TrackableListOperation.Insert, change2.Operation);
            Assert.Equal(1, change2.Index);
            Assert.Null(change2.OldValue);
            Assert.Equal("TwoInserted", change2.NewValue);
        }

        [Fact]
        public void TestList_HasChangedSetEvent_Work()
        {
            var changed = false;
            var list = CreateTestListWithTracker();
            list.Tracker.HasChangeSet += _ => { changed = true; };
            list.Add("Test");
            Assert.True(changed);
        }

        [Fact]
        public void TestList_ApplyToTrackable_Work()
        {
            var list = CreateTestListWithTracker();
            ModifyListForTest(list);

            var list2 = CreateTestList();
            list.Tracker.ApplyTo(list2);

            Assert.Equal(GetModifiedList(), list2);
        }

        [Fact]
        public void TestList_ApplyToTracker_Work()
        {
            var list = CreateTestListWithTracker();
            ModifyListForTest(list);

            var tracker2 = new TrackableListTracker<string>();
            list.Tracker.ApplyTo(tracker2);

            var list2 = CreateTestList();
            tracker2.ApplyTo(list2);

            Assert.Equal(GetModifiedList(), list2);
        }

        [Fact]
        public void TestList_RollbackToTrackable_Work()
        {
            var list = CreateTestListWithTracker();
            ModifyListForTest(list);

            var list2 = CreateTestList();
            list.Tracker.ApplyTo(list2);
            list.Tracker.RollbackTo(list2);

            Assert.Equal(GetInitialList(), list2);
        }

        [Fact]
        public void TestList_RollbackToTracker_Work()
        {
            var list = CreateTestListWithTracker();
            ModifyListForTest(list);

            var tracker2 = new TrackableListTracker<string>();
            list.Tracker.ApplyTo(tracker2);
            list.Tracker.RollbackTo(tracker2);

            var list2 = CreateTestList();
            tracker2.ApplyTo(list2);

            Assert.Equal(GetInitialList(), list2);
        }

        [Fact]
        public void TestList_Clone_Work()
        {
            var a = CreateTestListWithTracker();
            var b = a.Clone();

            Assert.Null(b.Tracker);
            Assert.False(ReferenceEquals(a._list, b._list));
            Assert.Equal(a._list, b._list);
        }

        [Fact]
        public void TestList_PushBack_TrackedCorrectly()
        {
            var list = CreateTestListWithTracker();
            list.Add("Four");

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal(TrackableListOperation.PushBack, changeList[0].Operation);
            Assert.Equal("Four", changeList[0].NewValue);
        }

        [Fact]
        public void TestList_PushFront_TrackedCorrectly()
        {
            var list = CreateTestListWithTracker();
            list.Insert(0, "Zero");

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal(TrackableListOperation.PushFront, changeList[0].Operation);
            Assert.Equal("Zero", changeList[0].NewValue);
        }

        [Fact]
        public void TestList_PopBack_TrackedCorrectly()
        {
            var list = CreateTestListWithTracker();
            list.RemoveAt(list.Count - 1);

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal(TrackableListOperation.PopBack, changeList[0].Operation);
            Assert.Equal("Three", changeList[0].OldValue);
        }

        [Fact]
        public void TestList_PopFront_TrackedCorrectly()
        {
            var list = CreateTestListWithTracker();
            list.RemoveAt(0);

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal(TrackableListOperation.PopFront, changeList[0].Operation);
            Assert.Equal("One", changeList[0].OldValue);
        }

        [Fact]
        public void TestList_Clear_TracksAllPopBacks()
        {
            var list = CreateTestListWithTracker();
            list.Clear();

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Equal(3, changeList.Count);
            Assert.All(changeList, c => Assert.Equal(TrackableListOperation.PopBack, c.Operation));
        }

        // --- Remove by item ---

        [Fact]
        public void TestList_RemoveByItem_TracksCorrectIndex()
        {
            var list = CreateTestListWithTracker();
            var removed = list.Remove("Two");

            Assert.True(removed);
            Assert.Equal(2, list.Count);
            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal("Two", changeList[0].OldValue);
        }

        [Fact]
        public void TestList_RemoveByItem_NonExistent_ReturnsFalse()
        {
            var list = CreateTestListWithTracker();
            var removed = list.Remove("NotExist");

            Assert.False(removed);
            Assert.False(list.Tracker.HasChange);
        }

        [Fact]
        public void TestList_RemoveByItem_FirstElement_TracksPopFront()
        {
            var list = CreateTestListWithTracker();
            list.Remove("One");

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Equal(TrackableListOperation.PopFront, changeList[0].Operation);
        }

        [Fact]
        public void TestList_RemoveByItem_LastElement_TracksPopBack()
        {
            var list = CreateTestListWithTracker();
            list.Remove("Three");

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Equal(TrackableListOperation.PopBack, changeList[0].Operation);
        }

        // --- Insert at middle position ---

        [Fact]
        public void TestList_InsertMiddle_TracksInsert()
        {
            var list = CreateTestListWithTracker();
            list.Insert(1, "Inserted");

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal(TrackableListOperation.Insert, changeList[0].Operation);
            Assert.Equal(1, changeList[0].Index);
            Assert.Equal("Inserted", changeList[0].NewValue);
        }

        [Fact]
        public void TestList_RemoveMiddle_TracksRemove()
        {
            var list = CreateTestListWithTracker();
            list.RemoveAt(1);

            var changeList = ((TrackableListTracker<string>)list.Tracker).ChangeList;
            Assert.Single(changeList);
            Assert.Equal(TrackableListOperation.Remove, changeList[0].Operation);
            Assert.Equal(1, changeList[0].Index);
            Assert.Equal("Two", changeList[0].OldValue);
        }

        // --- Null guard ---

        [Fact]
        public void TestListTracker_ApplyTo_NullTrackable_Throws()
        {
            var tracker = new TrackableListTracker<string>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((IList<string>)null!));
        }

        [Fact]
        public void TestListTracker_ApplyTo_NullTracker_Throws()
        {
            var tracker = new TrackableListTracker<string>();
            Assert.Throws<ArgumentNullException>(() => tracker.ApplyTo((TrackableListTracker<string>)null!));
        }

        [Fact]
        public void TestListTracker_RollbackTo_NullTrackable_Throws()
        {
            var tracker = new TrackableListTracker<string>();
            Assert.Throws<ArgumentNullException>(() => tracker.RollbackTo((IList<string>)null!));
        }

        // --- HasChangeSet event ---

        [Fact]
        public void TestListTracker_HasChangeSet_NotFiredOnSubsequentChanges()
        {
            var callCount = 0;
            var tracker = new TrackableListTracker<string>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackPushBack("a");
            tracker.TrackPushBack("b");
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void TestListTracker_HasChangeSet_FiredAgainAfterClearAndNewChange()
        {
            var callCount = 0;
            var tracker = new TrackableListTracker<string>();
            tracker.HasChangeSet += _ => { callCount++; };
            tracker.TrackPushBack("a");
            tracker.Clear();
            tracker.TrackPushBack("b");
            Assert.Equal(2, callCount);
        }

        // --- ToString ---

        [Fact]
        public void TestListTracker_ToString_ShowsAllOperations()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackInsert(0, "a");
            tracker.TrackRemove(1, "b");
            tracker.TrackModify(0, "a", "c");
            tracker.TrackPushFront("d");
            tracker.TrackPushBack("e");
            tracker.TrackPopFront("f");
            tracker.TrackPopBack("g");

            var str = tracker.ToString();
            Assert.Contains("+0:", str);
            Assert.Contains("-1:", str);
            Assert.Contains("=0:", str);
            Assert.Contains("+F:", str);
            Assert.Contains("+B:", str);
            Assert.Contains("-F:", str);
            Assert.Contains("-B:", str);
        }

        // --- Constructor variants ---

        [Fact]
        public void TestList_CopyConstructor_CopiesData()
        {
            var original = CreateTestList();
            var copy = new TrackableList<string>(original);

            Assert.Equal(3, copy.Count);
            Assert.Equal("One", copy[0]);
            Assert.Null(copy.Tracker);
        }

        [Fact]
        public void TestList_EnumerableConstructor_CopiesData()
        {
            var source = new[] { "A", "B", "C" };
            var list = new TrackableList<string>(source);
            Assert.Equal(3, list.Count);
            Assert.Equal("B", list[1]);
        }

        // --- No tracker ---

        [Fact]
        public void TestList_NoTracker_NoException()
        {
            var list = CreateTestList();
            list[0] = "Modified";
            list.Add("Four");
            list.RemoveAt(1);
            list.Insert(0, "Zero");
            list.Clear();
            Assert.False(list.Changed);
        }

        // --- Complex rollback ---

        [Fact]
        public void TestList_ComplexRollback_RestoresOriginal()
        {
            var list = CreateTestListWithTracker();
            list.Add("Four");
            list.RemoveAt(0);
            list[0] = "Modified";
            list.Insert(1, "Inserted");

            list.Rollback();

            Assert.Equal(new List<string> { "One", "Two", "Three" }, new List<string>(list));
            Assert.False(list.Changed);
        }
    }
}
