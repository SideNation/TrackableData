using TrackableData;

namespace TrackableData.MongoDB.Tests
{
    public interface ITestPerson : ITrackablePoco<ITestPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
    }
}
