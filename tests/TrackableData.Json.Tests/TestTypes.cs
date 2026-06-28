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
}
