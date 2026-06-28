using TrackableData;

namespace TrackableData.Redis.Tests
{
    public interface ITestPerson : ITrackablePoco<ITestPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    // Poco with a complex (non-trackable) class property, to verify class types serialize
    // through the poco mapper too.
    public interface ITestPocoWithClass : ITrackablePoco<ITestPocoWithClass>
    {
        string Name { get; set; }
        SampleData Data { get; set; }
    }

    // Container whose collection members hold a complex class value (not a primitive),
    // to verify class-typed values round-trip through every mapper.
    public interface IClassContainer : ITrackableContainer<IClassContainer>
    {
        TrackableDictionary<int, SampleData> Items { get; set; }
        TrackableList<SampleData> History { get; set; }
    }

    public class SampleData
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }

    // Shared sample data so tests build their fixtures from one place.
    public static class TestData
    {
        public static TrackableClassContainer CreateSampleClassContainer()
        {
            var container = new TrackableClassContainer();
            container.Items.Add(1, new SampleData { Name = "Alpha", Level = 1 });
            container.Items.Add(2, new SampleData { Name = "Beta", Level = 2 });
            container.History.Add(new SampleData { Name = "H1", Level = 10 });
            container.History.Add(new SampleData { Name = "H2", Level = 20 });
            return container;
        }
    }
}
