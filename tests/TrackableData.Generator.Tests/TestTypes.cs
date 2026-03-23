using System;
using TrackableData;

namespace TrackableData.Generator.Tests
{
    public interface IPerson : ITrackablePoco<IPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
        UInt128 Id { get; set; }
    }

    public interface IDataContainer : ITrackableContainer<IDataContainer>
    {
        TrackableDictionary<int, string> Names { get; set; }
        TrackableList<string> Tags { get; set; }
    }
}
