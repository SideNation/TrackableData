using MemoryPack;
using TrackableData.MemoryPack;
using Xunit;

namespace TrackableData.MemoryPack.Tests
{
    // Verifies the new poco and container formatters (whole object + tracker), so MemoryPack now
    // covers every trackable type like the Json plugin does.
    public class PocoContainerFormatterTest
    {
        public PocoContainerFormatterTest()
        {
            TrackableDataFormatterInitializer.RegisterPocoFormatter<IMpPerson>();
            TrackableDataFormatterInitializer.RegisterPocoFormatter<IMpPocoWithClass>();
            TrackableDataFormatterInitializer.RegisterDictionaryFormatter<int, SampleData>();
            TrackableDataFormatterInitializer.RegisterListFormatter<SampleData>();
            TrackableDataFormatterInitializer.RegisterContainerFormatter<IMpClassContainer>();
        }

        // ---- Poco ----

        [Fact]
        public void Poco_SerializeDeserialize()
        {
            IMpPocoWithClass poco = new TrackableMpPocoWithClass
            {
                Name = "P",
                Data = new SampleData { Name = "D", Level = 5 }
            };
            var bytes = MemoryPackSerializer.Serialize(poco);
            var deserialized = MemoryPackSerializer.Deserialize<IMpPocoWithClass>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal("P", deserialized.Name);
            Assert.Equal("D", deserialized.Data.Name);
            Assert.Equal(5, deserialized.Data.Level);
        }

        [Fact]
        public void PocoTracker_RoundTripAndApply()
        {
            var poco = new TrackableMpPocoWithClass
            {
                Name = "P",
                Data = new SampleData { Name = "D", Level = 5 }
            };
            poco.SetDefaultTrackerDeep();
            poco.Name = "P2";
            poco.Data = new SampleData { Name = "D2", Level = 50 };

            var tracker = (TrackablePocoTracker<IMpPocoWithClass>)poco.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<TrackablePocoTracker<IMpPocoWithClass>>(bytes);

            var target = new TrackableMpPocoWithClass
            {
                Name = "P",
                Data = new SampleData { Name = "D", Level = 5 }
            };
            deserialized.ApplyTo((IMpPocoWithClass)target);

            Assert.Equal("P2", target.Name);
            Assert.Equal("D2", target.Data.Name);
            Assert.Equal(50, target.Data.Level);
        }

        // ---- Container ----

        [Fact]
        public void Container_SerializeDeserialize()
        {
            var container = CreateSampleContainer();
            var bytes = MemoryPackSerializer.Serialize((IMpClassContainer)container);
            var deserialized = MemoryPackSerializer.Deserialize<IMpClassContainer>(bytes);

            Assert.NotNull(deserialized);
            Assert.Equal("Alice", deserialized.Person.Name);
            Assert.Equal(30, deserialized.Person.Age);
            Assert.Equal("Alpha", deserialized.Items[1].Name);
            Assert.Equal(2, deserialized.Items[2].Level);
            Assert.Equal(2, deserialized.History.Count);
            Assert.Equal("H1", deserialized.History[0].Name);
        }

        [Fact]
        public void ContainerTracker_RoundTripAndApply()
        {
            var container = CreateSampleContainer();
            container.SetDefaultTracker();
            container.Person.Name = "Bob";
            container.Items[1] = new SampleData { Name = "AlphaX", Level = 11 };
            container.Items.Add(3, new SampleData { Name = "Gamma", Level = 3 });
            container.History.Add(new SampleData { Name = "H3", Level = 30 });

            var tracker = (IContainerTracker<IMpClassContainer>)container.Tracker;
            var bytes = MemoryPackSerializer.Serialize(tracker);
            var deserialized = MemoryPackSerializer.Deserialize<IContainerTracker<IMpClassContainer>>(bytes);

            var target = CreateSampleContainer();
            deserialized.ApplyTo((IMpClassContainer)target);

            Assert.Equal("Bob", target.Person.Name);
            Assert.Equal("AlphaX", target.Items[1].Name);
            Assert.Equal("Gamma", target.Items[3].Name);
            Assert.Equal(3, target.History.Count);
            Assert.Equal("H3", target.History[2].Name);
        }

        private static TrackableMpClassContainer CreateSampleContainer()
        {
            var container = new TrackableMpClassContainer();
            container.Person.Name = "Alice";
            container.Person.Age = 30;
            container.Items.Add(1, new SampleData { Name = "Alpha", Level = 1 });
            container.Items.Add(2, new SampleData { Name = "Beta", Level = 2 });
            container.History.Add(new SampleData { Name = "H1", Level = 10 });
            container.History.Add(new SampleData { Name = "H2", Level = 20 });
            return container;
        }
    }
}
