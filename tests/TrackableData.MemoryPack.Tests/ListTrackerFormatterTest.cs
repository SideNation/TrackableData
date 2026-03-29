using System.Collections.Generic;
using MemoryPack;
using TrackableData.MemoryPack;
using Xunit;

namespace TrackableData.MemoryPack.Tests
{
    public class ListTrackerFormatterTest
    {
        public ListTrackerFormatterTest()
        {
            TrackableDataFormatterInitializer.RegisterListFormatter<string>();
        }

        [Fact]
        public void TestListTracker_SerializeDeserialize()
        {
            var list = new TrackableList<string>() { "One", "Two", "Three" };
            list.SetDefaultTrackerDeep();

            list[0] = "OneModified";
            list.RemoveAt(1);
            list.Add("Four");

            var tracker = (TrackableListTracker<string>)list.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.ChangeList.Count);

            var list2 = new TrackableList<string>() { "One", "Two", "Three" };
            deserialized.ApplyTo(list2);

            Assert.Equal(new List<string> { "OneModified", "Three", "Four" }, list2);
        }

        [Fact]
        public void TestListTracker_PushPop_SerializeDeserialize()
        {
            var list = new TrackableList<string>() { "One", "Two" };
            list.SetDefaultTrackerDeep();

            list.Insert(0, "Zero");
            list.RemoveAt(list.Count - 1);

            var tracker = (TrackableListTracker<string>)list.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.NotNull(deserialized);

            var list2 = new TrackableList<string>() { "One", "Two" };
            deserialized.ApplyTo(list2);

            Assert.Equal(new List<string> { "Zero", "One" }, list2);
        }

        [Fact]
        public void TestList_SerializeDeserialize()
        {
            var list = new TrackableList<string>() { "One", "Two", "Three" };
            var bytes = MemoryPackSerializer.Serialize(list);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableList<string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("One", deserialized[0]);
            Assert.Equal("Two", deserialized[1]);
            Assert.Equal("Three", deserialized[2]);
        }

        [Fact]
        public void TestListTracker_Null_SerializeDeserialize()
        {
            TrackableListTracker<string> tracker = null;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);
            Assert.Null(deserialized);
        }

        [Fact]
        public void TestListTracker_Empty_RoundTrip()
        {
            var tracker = new TrackableListTracker<string>();
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized.ChangeList);
            Assert.False(deserialized.HasChange);
        }

        [Fact]
        public void TestListTracker_InsertOnly_RoundTrip()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackInsert(0, "a");
            tracker.TrackInsert(1, "b");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.Equal(2, deserialized.ChangeList.Count);
            Assert.Equal(TrackableListOperation.Insert, deserialized.ChangeList[0].Operation);
            Assert.Equal(0, deserialized.ChangeList[0].Index);
            Assert.Equal("a", deserialized.ChangeList[0].NewValue);
        }

        [Fact]
        public void TestListTracker_RemoveOnly_RoundTrip()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackRemove(0, "removed");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.Single(deserialized.ChangeList);
            Assert.Equal(TrackableListOperation.Remove, deserialized.ChangeList[0].Operation);
            Assert.Equal("removed", deserialized.ChangeList[0].OldValue);
        }

        [Fact]
        public void TestListTracker_ModifyOnly_RoundTrip()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackModify(2, "old", "new");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.Single(deserialized.ChangeList);
            Assert.Equal(TrackableListOperation.Modify, deserialized.ChangeList[0].Operation);
            Assert.Equal(2, deserialized.ChangeList[0].Index);
            Assert.Equal("old", deserialized.ChangeList[0].OldValue);
            Assert.Equal("new", deserialized.ChangeList[0].NewValue);
        }

        [Fact]
        public void TestListTracker_AllOperations_RoundTrip()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackInsert(0, "inserted");
            tracker.TrackRemove(1, "removed");
            tracker.TrackModify(0, "old", "new");
            tracker.TrackPushFront("front");
            tracker.TrackPushBack("back");
            tracker.TrackPopFront("popFront");
            tracker.TrackPopBack("popBack");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.Equal(7, deserialized.ChangeList.Count);
            Assert.Equal(TrackableListOperation.Insert, deserialized.ChangeList[0].Operation);
            Assert.Equal(TrackableListOperation.Remove, deserialized.ChangeList[1].Operation);
            Assert.Equal(TrackableListOperation.Modify, deserialized.ChangeList[2].Operation);
            Assert.Equal(TrackableListOperation.PushFront, deserialized.ChangeList[3].Operation);
            Assert.Equal(TrackableListOperation.PushBack, deserialized.ChangeList[4].Operation);
            Assert.Equal(TrackableListOperation.PopFront, deserialized.ChangeList[5].Operation);
            Assert.Equal(TrackableListOperation.PopBack, deserialized.ChangeList[6].Operation);
        }

        [Fact]
        public void TestList_Empty_SerializeDeserialize()
        {
            var list = new TrackableList<string>();
            var bytes = MemoryPackSerializer.Serialize(list);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableList<string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void TestList_Null_SerializeDeserialize()
        {
            TrackableList<string> list = null;
            var bytes = MemoryPackSerializer.Serialize(list);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableList<string>>(bytes);
            Assert.Null(deserialized);
        }

        [Fact]
        public void TestListTracker_Deserialized_CanApplyToList()
        {
            var tracker = new TrackableListTracker<string>();
            tracker.TrackPushBack("D");
            tracker.TrackModify(0, "A", "AA");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            var list = new List<string> { "A", "B", "C" };
            deserialized.ApplyTo(list);

            Assert.Equal(new List<string> { "AA", "B", "C", "D" }, list);
        }

        [Fact]
        public void TestListTracker_ManyItems_RoundTrip()
        {
            var tracker = new TrackableListTracker<string>();
            for (var i = 0; i < 50; i++)
                tracker.TrackPushBack($"item_{i}");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<string>>(bytes);

            Assert.Equal(50, deserialized.ChangeList.Count);
            for (var i = 0; i < 50; i++)
            {
                Assert.Equal(TrackableListOperation.PushBack, deserialized.ChangeList[i].Operation);
                Assert.Equal($"item_{i}", deserialized.ChangeList[i].NewValue);
            }
        }
    }
}
