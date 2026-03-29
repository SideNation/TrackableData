using System.Collections.Generic;
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

        [Fact]
        public void TestDictionaryTracker_AddOnly_RoundTrip()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(1, "one");
            tracker.TrackAdd(2, "two");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            Assert.Equal(2, deserialized.ChangeMap.Count);
            Assert.Equal(TrackableDictionaryOperation.Add, deserialized.ChangeMap[1].Operation);
            Assert.Equal("one", deserialized.ChangeMap[1].NewValue);
            Assert.Equal(TrackableDictionaryOperation.Add, deserialized.ChangeMap[2].Operation);
        }

        [Fact]
        public void TestDictionaryTracker_RemoveOnly_RoundTrip()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackRemove(1, "one");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            Assert.Single(deserialized.ChangeMap);
            Assert.Equal(TrackableDictionaryOperation.Remove, deserialized.ChangeMap[1].Operation);
            Assert.Equal("one", deserialized.ChangeMap[1].OldValue);
        }

        [Fact]
        public void TestDictionaryTracker_ModifyOnly_RoundTrip()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackModify(1, "old", "new");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            Assert.Single(deserialized.ChangeMap);
            Assert.Equal(TrackableDictionaryOperation.Modify, deserialized.ChangeMap[1].Operation);
            Assert.Equal("old", deserialized.ChangeMap[1].OldValue);
            Assert.Equal("new", deserialized.ChangeMap[1].NewValue);
        }

        [Fact]
        public void TestDictionaryTracker_Empty_RoundTrip()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized.ChangeMap);
            Assert.False(deserialized.HasChange);
        }

        [Fact]
        public void TestDictionary_Empty_SerializeDeserialize()
        {
            var dict = new TrackableDictionary<int, string>();
            var bytes = MemoryPackSerializer.Serialize(dict);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionary<int, string>>(bytes);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void TestDictionary_Null_SerializeDeserialize()
        {
            TrackableDictionary<int, string> dict = null;
            var bytes = MemoryPackSerializer.Serialize(dict);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionary<int, string>>(bytes);
            Assert.Null(deserialized);
        }

        [Fact]
        public void TestDictionaryTracker_Deserialized_CanApplyToTrackable()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            tracker.TrackAdd(10, "ten");
            tracker.TrackModify(1, "one", "ONE");
            tracker.TrackRemove(2, "two");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            var dict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" } };
            deserialized.ApplyTo(dict);

            Assert.Equal("ONE", dict[1]);
            Assert.False(dict.ContainsKey(2));
            Assert.Equal("three", dict[3]);
            Assert.Equal("ten", dict[10]);
        }

        [Fact]
        public void TestDictionaryTracker_ManyItems_RoundTrip()
        {
            var tracker = new TrackableDictionaryTracker<int, string>();
            for (var i = 0; i < 100; i++)
                tracker.TrackAdd(i, $"value_{i}");

            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, string>>(bytes);

            Assert.Equal(100, deserialized.ChangeMap.Count);
            for (var i = 0; i < 100; i++)
            {
                Assert.Equal(TrackableDictionaryOperation.Add, deserialized.ChangeMap[i].Operation);
                Assert.Equal($"value_{i}", deserialized.ChangeMap[i].NewValue);
            }
        }
    }
}
