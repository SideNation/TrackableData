using MongoDB.Bson;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    // Verifies id mapping (mongodb.identity custom id and ObjectId auto id) without a live MongoDB.
    public class PocoIdMongoDbMapperUnitTest
    {
        [Fact]
        public void CustomId_IsMappedToBsonId()
        {
            var mapper = new TrackablePocoMongoDbMapper<ITestPocoWithCustomId>();
            var poco = new TrackableTestPocoWithCustomId { CustomId = 12345L, Name = "Testor", Age = 25 };

            var bson = mapper.ConvertToBsonDocument(poco);

            Assert.True(bson.Contains("_id"));
            Assert.Equal(12345L, bson["_id"].AsInt64);
            Assert.False(bson.Contains("CustomId"));
        }

        [Fact]
        public void CustomId_RoundTrip()
        {
            var mapper = new TrackablePocoMongoDbMapper<ITestPocoWithCustomId>();
            var poco = new TrackableTestPocoWithCustomId { CustomId = 999L, Name = "Alice", Age = 30 };

            var result = mapper.ConvertToTrackablePoco(mapper.ConvertToBsonDocument(poco));

            Assert.Equal(999L, result.CustomId);
            Assert.Equal("Alice", result.Name);
            Assert.Equal(30, result.Age);
        }

        [Fact]
        public void ObjectId_IsMappedToBsonId()
        {
            var mapper = new TrackablePocoMongoDbMapper<ITestPocoWithObjectId>();
            var id = ObjectId.GenerateNewId();
            var poco = new TrackableTestPocoWithObjectId { Id = id, Name = "Bob", Age = 40 };

            var bson = mapper.ConvertToBsonDocument(poco);
            Assert.Equal(id, bson["_id"].AsObjectId);

            var result = mapper.ConvertToTrackablePoco(bson);
            Assert.Equal(id, result.Id);
        }
    }
}
