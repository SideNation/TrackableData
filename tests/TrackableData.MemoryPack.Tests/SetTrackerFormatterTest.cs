using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using TrackableData.MemoryPack;
using Xunit;

namespace TrackableData.MemoryPack.Tests
{
    public class SetTrackerFormatterTest
    {
        public SetTrackerFormatterTest()
        {
            TrackableDataFormatterInitializer.RegisterSetFormatter<int>();
        }

        [Fact]
        public void TestSetTracker_SerializeDeserialize()
        {
            var set = new TrackableSet<int>() { 1, 2, 3 };
            set.SetDefaultTrackerDeep();

            set.Remove(2);
            set.Add(4);

            var tracker = (TrackableSetTracker<int>)set.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.ChangeMap.Count);

            var set2 = new TrackableSet<int>() { 1, 2, 3 };
            deserialized.ApplyTo(set2);

            Assert.Equal(new[] { 1, 3, 4 }, set2.OrderBy(v => v));
        }

        [Fact]
        public void TestSetTracker_Null_SerializeDeserialize()
        {
            TrackableSetTracker<int> tracker = null;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);
            Assert.Null(deserialized);
        }

        [Fact]
        public void TestSet_SerializeDeserialize()
        {
            var set = new TrackableSet<int>() { 1, 2, 3 };
            var bytes = MemoryPackSerializer.Serialize(set);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSet<int>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.Equal(new[] { 1, 2, 3 }, deserialized.OrderBy(v => v));
        }

        [Fact]
        public void TestSetTracker_Empty_RoundTrip()
        {
            var tracker = new TrackableSetTracker<int>();
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized.ChangeMap);
            Assert.False(deserialized.HasChange);
        }

        [Fact]
        public void TestSetTracker_AddOnly_RoundTrip()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(10);
            tracker.TrackAdd(20);
            tracker.TrackAdd(30);

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            Assert.Equal(3, deserialized.ChangeMap.Count);
            Assert.All(deserialized.ChangeMap.Values, op => Assert.Equal(TrackableSetOperation.Add, op));
            Assert.Equal(new[] { 10, 20, 30 }, deserialized.AddValues.OrderBy(v => v));
        }

        [Fact]
        public void TestSetTracker_RemoveOnly_RoundTrip()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackRemove(1);
            tracker.TrackRemove(2);

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            Assert.Equal(2, deserialized.ChangeMap.Count);
            Assert.All(deserialized.ChangeMap.Values, op => Assert.Equal(TrackableSetOperation.Remove, op));
            Assert.Equal(new[] { 1, 2 }, deserialized.RemoveValues.OrderBy(v => v));
        }

        [Fact]
        public void TestSetTracker_Deserialized_CanApplyToCollection()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(99);
            tracker.TrackRemove(1);

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            var set = new TrackableSet<int> { 1, 2, 3 };
            deserialized.ApplyTo(set);

            Assert.Equal(new[] { 2, 3, 99 }, set.OrderBy(v => v));
        }

        [Fact]
        public void TestSet_Empty_SerializeDeserialize()
        {
            var set = new TrackableSet<int>();
            var bytes = MemoryPackSerializer.Serialize(set);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSet<int>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void TestSet_Null_SerializeDeserialize()
        {
            TrackableSet<int> set = null;
            var bytes = MemoryPackSerializer.Serialize(set);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSet<int>>(bytes);
            Assert.Null(deserialized);
        }

        [Fact]
        public void TestSetTracker_ManyItems_RoundTrip()
        {
            var tracker = new TrackableSetTracker<int>();
            for (var i = 0; i < 100; i++)
                tracker.TrackAdd(i);

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            Assert.Equal(100, deserialized.ChangeMap.Count);
            Assert.Equal(100, deserialized.AddValues.Count());
        }

        [Fact]
        public void TestSetTracker_Deserialized_CanRollback()
        {
            var tracker = new TrackableSetTracker<int>();
            tracker.TrackAdd(99);
            tracker.TrackRemove(1);

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSetTracker<int>>(bytes);

            var set = new TrackableSet<int> { 2, 3, 99 };
            deserialized.RollbackTo(set);

            Assert.Equal(new[] { 1, 2, 3 }, set.OrderBy(v => v));
        }
    }
}
