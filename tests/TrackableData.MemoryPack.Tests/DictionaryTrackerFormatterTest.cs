using System.Linq;
using MemoryPack;
using TrackableData.MemoryPack;
using Xunit;

namespace TrackableData.MemoryPack.Tests
{
    public class DictionaryTrackerFormatterTest
    {
        public DictionaryTrackerFormatterTest()
        {
            TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, string>();
        }

        [Fact]
        public void TestDictionaryTracker_SerializeDeserialize()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" }, { 2, "Two" } };
            dict.SetDefaultTrackerDeep();

            dict[1] = "OneModified";
            dict.Remove(2);
            dict[3] = "ThreeAdded";

            var tracker = (TrackableDictionaryTracker<int, string>)dict.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.ChangeMap.Count);

            var dict2 = new TrackableDictionary<int, string>() { { 1, "One" }, { 2, "Two" } };
            deserialized.ApplyTo(dict2);

            Assert.Equal("OneModified", dict2[1]);
            Assert.False(dict2.ContainsKey(2));
            Assert.Equal("ThreeAdded", dict2[3]);
        }

        [Fact]
        public void TestDictionaryTracker_Null_SerializeDeserialize()
        {
            TrackableDictionaryTracker<int, string> tracker = null;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);
            Assert.Null(deserialized);
        }

        [Fact]
        public void TestDictionary_SerializeDeserialize()
        {
            var dict = new TrackableDictionary<int, string>() { { 1, "One" }, { 2, "Two" }, { 3, "Three" } };
            var bytes = MemoryPackSerializer.Serialize(dict);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionary<int, string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("One", deserialized[1]);
            Assert.Equal("Two", deserialized[2]);
            Assert.Equal("Three", deserialized[3]);
        }
    }
}
