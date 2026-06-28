using MemoryPack;
using TrackableData;

namespace TrackableData.MemoryPack.Tests
{
    [MemoryPackable]
    public partial class SampleData
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }

    public interface IMpPerson : ITrackablePoco<IMpPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    // Poco with a complex (non-trackable) class property.
    public interface IMpPocoWithClass : ITrackablePoco<IMpPocoWithClass>
    {
        string Name { get; set; }
        SampleData Data { get; set; }
    }

    // Container exercising a poco member plus class-valued dictionary/list members.
    public interface IMpClassContainer : ITrackableContainer<IMpClassContainer>
    {
        IMpPerson Person { get; set; }
        TrackableDictionary<int, SampleData> Items { get; set; }
        TrackableList<SampleData> History { get; set; }
    }
}
