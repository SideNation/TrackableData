using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    // Verifies the container mapper's serialization without requiring a live MongoDB,
    // exercising ExportToBson/ImportFromBson for dictionary, list and set members.
    public class ContainerMongoDbMapperUnitTest
    {
        private readonly TrackableContainerMongoDbMapper<ITestContainer> _mapper =
            new TrackableContainerMongoDbMapper<ITestContainer>();

        [Fact]
        public void DefaultConstructor_Works()
        {
            Assert.NotNull(_mapper);
        }

        [Fact]
        public void Constructor_WithLogger()
        {
            var mapper = new TrackableContainerMongoDbMapper<ITestContainer>(new TestTrackableLogger());
            Assert.NotNull(mapper);
        }

        [Fact]
        public void ConvertToBsonDocument_ContainsAllMembers()
        {
            var bson = _mapper.ConvertToBsonDocument(TestData.CreateSampleContainer());

            Assert.True(bson.Contains("Person"));
            Assert.True(bson.Contains("Missions"));
            Assert.True(bson.Contains("Tags"));
            Assert.True(bson.Contains("Aliases"));
            Assert.Equal("Alice", bson["Person"]["Name"].AsString);
            Assert.Equal("First", bson["Missions"]["1"].AsString);
            Assert.Equal(new List<string> { "red", "green" },
                bson["Tags"].AsBsonArray.Select(v => v.AsString));
            Assert.Equal(new[] { "a1", "a2" },
                bson["Aliases"].AsBsonArray.Select(v => v.AsString).OrderBy(v => v));
        }

        [Fact]
        public void RoundTrip_PreservesValues()
        {
            var bson = _mapper.ConvertToBsonDocument(TestData.CreateSampleContainer());
            var result = _mapper.ConvertToTrackableContainer(bson);

            Assert.Equal("Alice", result.Person.Name);
            Assert.Equal(30, result.Person.Age);
            Assert.Equal("First", result.Missions[1]);
            Assert.Equal("Second", result.Missions[2]);
            Assert.Equal(new List<string> { "red", "green" }, result.Tags);
            Assert.Equal(new[] { "a1", "a2" }, result.Aliases.OrderBy(v => v));
        }
    }
}
