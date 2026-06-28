using MongoDB.Bson;
using TrackableData;

namespace TrackableData.MongoDB.Tests
{
    public interface ITestPerson : ITrackablePoco<ITestPerson>
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    public interface ITestPocoWithCustomId : ITrackablePoco<ITestPocoWithCustomId>
    {
        [TrackableProperty("mongodb.identity")] long CustomId { get; set; }
        string Name { get; set; }
        int Age { get; set; }
    }

    public interface ITestPocoWithObjectId : ITrackablePoco<ITestPocoWithObjectId>
    {
        ObjectId Id { get; set; }
        string Name { get; set; }
        int Age { get; set; }
    }

    // A poco member is declared by its interface (ITestPerson); the generator emits a concrete
    // TrackableTestPerson backing field for it.
    public interface ITestContainer : ITrackableContainer<ITestContainer>
    {
        ITestPerson Person { get; set; }
        TrackableDictionary<int, string> Missions { get; set; }
        TrackableList<string> Tags { get; set; }
        TrackableSet<string> Aliases { get; set; }
    }

    public class BsonValueMapperTestClass
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }

    internal class TestTrackableLogger : ITrackableLogger
    {
        public int CallCount;
        public void LogDebug(string message, params object[] args) => CallCount++;
    }

    // Shared sample data so tests build their fixtures from one place.
    public static class TestData
    {
        public static TrackableTestContainer CreateSampleContainer()
        {
            var container = new TrackableTestContainer();
            container.Person.Name = "Alice";
            container.Person.Age = 30;
            container.Missions.Add(1, "First");
            container.Missions.Add(2, "Second");
            container.Tags.Add("red");
            container.Tags.Add("green");
            container.Aliases.Add("a1");
            container.Aliases.Add("a2");
            return container;
        }
    }
}
