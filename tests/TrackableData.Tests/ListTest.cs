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
    }
}
