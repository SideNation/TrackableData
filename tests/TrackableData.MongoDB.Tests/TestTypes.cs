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

    // Poco with a complex (non-trackable) class property, to verify class types serialize
    // through the poco mapper too.
    public interface ITestPocoWithClass : ITrackablePoco<ITestPocoWithClass>
    {
        string Name { get; set; }
        BsonValueMapperTestClass Data { get; set; }
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

    // Container whose collection members hold a complex class value (not a primitive),
    // to verify class-typed values round-trip through every mapper.
    public interface IClassContainer : ITrackableContainer<IClassContainer>
    {
        TrackableDictionary<int, BsonValueMapperTestClass> Items { get; set; }
        TrackableList<BsonValueMapperTestClass> History { get; set; }
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

        public static TrackableClassContainer CreateSampleClassContainer()
        {
            var container = new TrackableClassContainer();
            container.Items.Add(1, new BsonValueMapperTestClass { Name = "Alpha", Level = 1 });
            container.Items.Add(2, new BsonValueMapperTestClass { Name = "Beta", Level = 2 });
            container.History.Add(new BsonValueMapperTestClass { Name = "H1", Level = 10 });
            container.History.Add(new BsonValueMapperTestClass { Name = "H2", Level = 20 });
            return container;
        }
    }
}
