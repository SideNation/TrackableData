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
    }
}
