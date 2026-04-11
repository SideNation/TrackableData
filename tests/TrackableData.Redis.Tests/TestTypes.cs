using TrackableData;

namespace TrackableData.Redis.Tests
{
    public interface ITestPerson : ITrackablePoco<ITestPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
    }
}
