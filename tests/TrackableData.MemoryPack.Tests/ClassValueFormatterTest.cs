using System.Linq;
using MemoryPack;
using TrackableData.MemoryPack;
using Xunit;

namespace TrackableData.MemoryPack.Tests
{
    // Verifies a complex class value ([MemoryPackable] SampleData) round-trips through every
    // collection formatter and its tracker formatter.
    public class ClassValueFormatterTest
    {
        public ClassValueFormatterTest()
        {
            TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, SampleData>();
            TrackableDataFormatterInitializer.RegisterListFormatter<SampleData>();
            TrackableDataFormatterInitializer.RegisterSetFormatter<SampleData>();
        }

        [Fact]
        public void Dictionary_ClassValue_SerializeDeserialize()
        {
            var dict = new TrackableDictionary<int, SampleData>
            {
                { 1, new SampleData { Name = "Alpha", Level = 1 } },
                { 2, new SampleData { Name = "Beta", Level = 2 } }
            };
            var bytes = MemoryPackSerializer.Serialize(dict);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionary<int, SampleData>>(bytes);

            Assert.Equal(2, deserialized.Count);
            Assert.Equal("Alpha", deserialized[1].Name);
            Assert.Equal(2, deserialized[2].Level);
        }

        [Fact]
        public void DictionaryTracker_ClassValue_RoundTripAndApply()
        {
            var dict = new TrackableDictionary<int, SampleData>
            {
                { 1, new SampleData { Name = "Alpha", Level = 1 } },
                { 2, new SampleData { Name = "Beta", Level = 2 } }
            };
            dict.SetDefaultTrackerDeep();
            dict[1] = new SampleData { Name = "AlphaX", Level = 11 };
            dict.Add(3, new SampleData { Name = "Gamma", Level = 3 });

            var tracker = (TrackableDictionaryTracker<int, SampleData>)dict.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableDictionaryTracker<int, SampleData>>(bytes);

            var target = new TrackableDictionary<int, SampleData>
            {
                { 1, new SampleData { Name = "Alpha", Level = 1 } },
                { 2, new SampleData { Name = "Beta", Level = 2 } }
            };
            deserialized.ApplyTo(target);

            Assert.Equal("AlphaX", target[1].Name);
            Assert.Equal(11, target[1].Level);
            Assert.Equal("Gamma", target[3].Name);
        }

        [Fact]
        public void List_ClassValue_SerializeDeserialize()
        {
            var list = new TrackableList<SampleData>
            {
                new SampleData { Name = "A", Level = 1 },
                new SampleData { Name = "B", Level = 2 }
            };
            var bytes = MemoryPackSerializer.Serialize(list);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableList<SampleData>>(bytes);

            Assert.Equal(2, deserialized.Count);
            Assert.Equal("A", deserialized[0].Name);
            Assert.Equal(2, deserialized[1].Level);
        }

        [Fact]
        public void ListTracker_ClassValue_RoundTripAndApply()
        {
            var list = new TrackableList<SampleData>
            {
                new SampleData { Name = "A", Level = 1 },
                new SampleData { Name = "B", Level = 2 }
            };
            list.SetDefaultTrackerDeep();
            list[0] = new SampleData { Name = "AA", Level = 11 };
            list.Add(new SampleData { Name = "C", Level = 3 });

            var tracker = (TrackableListTracker<SampleData>)list.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableListTracker<SampleData>>(bytes);

            var target = new TrackableList<SampleData>
            {
                new SampleData { Name = "A", Level = 1 },
                new SampleData { Name = "B", Level = 2 }
            };
            deserialized.ApplyTo(target);

            Assert.Equal(3, target.Count);
            Assert.Equal("AA", target[0].Name);
            Assert.Equal("C", target[2].Name);
        }

        [Fact]
        public void Set_ClassValue_SerializeDeserialize()
        {
            var set = new TrackableSet<SampleData>
            {
                new SampleData { Name = "X", Level = 1 },
                new SampleData { Name = "Y", Level = 2 }
            };
            var bytes = MemoryPackSerializer.Serialize(set);
            var deserialized = MemoryPackSerializer.Deserialize<TrackableSet<SampleData>>(bytes);

            Assert.Equal(2, deserialized.Count);
            Assert.Equal(new[] { "X", "Y" }, deserialized.Select(x => x.Name).OrderBy(n => n));
            Assert.Equal(3, deserialized.Sum(x => x.Level));
        }
    }
}
