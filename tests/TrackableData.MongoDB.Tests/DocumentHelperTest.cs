using MongoDB.Bson;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class DocumentHelperTest
    {
        [Fact]
        public void ToDotPath_SingleKey()
        {
            var path = DocumentHelper.ToDotPath(new object[] { "name" });
            Assert.Equal("name", path);
        }

        [Fact]
        public void ToDotPath_MultipleKeys()
        {
            var path = DocumentHelper.ToDotPath(new object[] { "a", "b", "c" });
            Assert.Equal("a.b.c", path);
        }

        [Fact]
        public void ToDotPath_EmptyKeys()
        {
            var path = DocumentHelper.ToDotPath(new object[0]);
            Assert.Equal("", path);
        }

        [Fact]
        public void ToDotPathWithTrailer_SingleKey()
        {
            var path = DocumentHelper.ToDotPathWithTrailer(new object[] { "name" });
            Assert.Equal("name.", path);
        }

        [Fact]
        public void ToDotPathWithTrailer_MultipleKeys()
        {
            var path = DocumentHelper.ToDotPathWithTrailer(new object[] { "a", "b" });
            Assert.Equal("a.b.", path);
        }

        [Fact]
        public void ToDotPathWithTrailer_EmptyKeys()
        {
            var path = DocumentHelper.ToDotPathWithTrailer(new object[0]);
            Assert.Equal("", path);
        }

        [Fact]
        public void QueryValue_SimpleKey()
        {
            var doc = new BsonDocument { { "name", "Alice" } };
            var value = DocumentHelper.QueryValue(doc, new object[] { "name" });
            Assert.Equal("Alice", value.AsString);
        }

        [Fact]
        public void QueryValue_NestedKeys()
        {
            var doc = new BsonDocument
            {
                { "level1", new BsonDocument { { "level2", "deep" } } }
            };
            var value = DocumentHelper.QueryValue(doc, new object[] { "level1", "level2" });
            Assert.Equal("deep", value.AsString);
        }

        [Fact]
        public void QueryValue_NullDocument_ReturnsNull()
        {
            var value = DocumentHelper.QueryValue(null, new object[] { "key" });
            Assert.Null(value);
        }

        [Fact]
        public void QueryValue_MissingKey_ReturnsNull()
        {
            var doc = new BsonDocument { { "name", "Alice" } };
            var value = DocumentHelper.QueryValue(doc, new object[] { "missing" });
            Assert.Null(value);
        }

        [Fact]
        public void QueryValue_NestedMissingKey_ReturnsNull()
        {
            var doc = new BsonDocument
            {
                { "level1", new BsonDocument { { "level2", "deep" } } }
            };
            var value = DocumentHelper.QueryValue(doc, new object[] { "level1", "missing" });
            Assert.Null(value);
        }

        [Fact]
        public void QueryValue_NonDocumentInPath_ReturnsNull()
        {
            var doc = new BsonDocument { { "name", "Alice" } };
            var value = DocumentHelper.QueryValue(doc, new object[] { "name", "subkey" });
            Assert.Null(value);
        }

        [Fact]
        public void QueryValue_EmptyKeys_ReturnsDocument()
        {
            var doc = new BsonDocument { { "name", "Alice" } };
            var value = DocumentHelper.QueryValue(doc, new object[0]);
            Assert.True(value.IsBsonDocument);
        }

        [Fact]
        public void QueryValue_IntegerValue()
        {
            var doc = new BsonDocument { { "age", 30 } };
            var value = DocumentHelper.QueryValue(doc, new object[] { "age" });
            Assert.Equal(30, value.AsInt32);
        }
    }
}
