using MongoDB.Bson;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class BsonValueMapperTest
    {
        [Fact]
        public void ToBsonValue_Null_ReturnsBsonNull()
        {
            var value = BsonValueMapper.ToBsonValue<string>(null);

            Assert.True(value.IsBsonNull);
        }

        [Fact]
        public void ToBsonValue_Class_ReturnsBsonDocument()
        {
            var value = BsonValueMapper.ToBsonValue(new BsonValueMapperTestClass
            {
                Name = "Alpha",
                Level = 3
            });

            Assert.True(value.IsBsonDocument);
            Assert.Equal("Alpha", value.AsBsonDocument["Name"].AsString);
            Assert.Equal(3, value.AsBsonDocument["Level"].AsInt32);
        }

        [Fact]
        public void ToValue_Primitive_ReturnsValue()
        {
            var value = BsonValueMapper.ToValue<int>(new BsonInt32(7));

            Assert.Equal(7, value);
        }

        [Fact]
        public void ToValue_Class_ReturnsValue()
        {
            var value = BsonValueMapper.ToValue<BsonValueMapperTestClass>(new BsonDocument
            {
                { "Name", "Beta" },
                { "Level", 5 }
            });

            Assert.Equal("Beta", value.Name);
            Assert.Equal(5, value.Level);
        }
    }
}
