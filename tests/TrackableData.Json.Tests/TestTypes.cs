namespace TrackableData.Json.Tests
{
    public interface IJsonPerson : ITrackablePoco<IJsonPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    public interface IJsonDataContainer : ITrackableContainer<IJsonDataContainer>
    {
        TrackableDictionary<int, string> Names { get; set; }
        TrackableList<string> Events { get; set; }
        TrackableSet<int> Values { get; set; }
    }

    // Poco with a complex (non-trackable) class property, to verify class types serialize
    // through the poco tracker converter too.
    public interface IJsonPocoWithClass : ITrackablePoco<IJsonPocoWithClass>
    {
        string Name { get; set; }
        SampleData Data { get; set; }
    }

    // Container whose collection members hold a complex class value (not a primitive).
    public interface IJsonClassContainer : ITrackableContainer<IJsonClassContainer>
    {
        TrackableDictionary<int, SampleData> Items { get; set; }
        TrackableList<SampleData> History { get; set; }
    }

    public class SampleData
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }
}
