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
    }
}
