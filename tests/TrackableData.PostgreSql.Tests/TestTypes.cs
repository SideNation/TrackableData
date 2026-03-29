using TrackableData;

namespace TrackableData.PostgreSql.Tests
{
    public interface ITestPerson : ITrackablePoco<ITestPerson>
    {
        [TrackableProperty("sql.primary-key")]
        int Id { get; set; }

        string Name { get; set; }
        int Age { get; set; }
    }
}
