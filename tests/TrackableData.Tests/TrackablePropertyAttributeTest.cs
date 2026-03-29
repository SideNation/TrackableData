using Xunit;

namespace TrackableData.Tests
{
    public class TrackablePropertyAttributeTest
    {
        // --- Exact match parameter ---

        [Fact]
        public void ExactMatch_ReturnsTrue()
        {
            var attr = new TrackablePropertyAttribute("sql.primary-key", "readonly");
            Assert.Equal("true", attr["sql.primary-key"]);
            Assert.Equal("true", attr["readonly"]);
        }

        [Fact]
        public void ExactMatch_NotFound_ReturnsNull()
        {
            var attr = new TrackablePropertyAttribute("sql.primary-key");
            Assert.Null(attr["other"]);
        }

        // --- Prefix match parameter (ends with ':') ---

        [Fact]
        public void PrefixMatch_ReturnsValue()
        {
            var attr = new TrackablePropertyAttribute("sql.type:varchar(100)", "sql.column:user_name");
            Assert.Equal("varchar(100)", attr["sql.type:"]);
            Assert.Equal("user_name", attr["sql.column:"]);
        }

        [Fact]
        public void PrefixMatch_NotFound_ReturnsNull()
        {
            var attr = new TrackablePropertyAttribute("sql.type:varchar(100)");
            Assert.Null(attr["other:"]);
        }

        // --- Empty parameters ---

        [Fact]
        public void EmptyParameters_ReturnsNull()
        {
            var attr = new TrackablePropertyAttribute();
            Assert.Null(attr["anything"]);
            Assert.Null(attr["anything:"]);
        }

        // --- Parameters property ---

        [Fact]
        public void Parameters_ReturnsAllParameters()
        {
            var attr = new TrackablePropertyAttribute("a", "b", "c");
            Assert.Equal(new[] { "a", "b", "c" }, attr.Parameters);
        }

        // --- GetParameter static method ---

        [Fact]
        public void GetParameter_FromProperty_ReturnsValue()
        {
            var prop = typeof(IAttributeTestPoco).GetProperty("Id")!;
            var result = TrackablePropertyAttribute.GetParameter(prop, "sql.primary-key");
            Assert.Equal("true", result);
        }

        [Fact]
        public void GetParameter_FromProperty_Prefix_ReturnsValue()
        {
            var prop = typeof(IAttributeTestPoco).GetProperty("Id")!;
            var result = TrackablePropertyAttribute.GetParameter(prop, "sql.column:");
            Assert.Equal("id_col", result);
        }

        [Fact]
        public void GetParameter_FromPropertyWithNoAttribute_ReturnsNull()
        {
            var prop = typeof(IAttributeTestPoco).GetProperty("Name")!;
            var result = TrackablePropertyAttribute.GetParameter(prop, "sql.primary-key");
            Assert.Null(result);
        }

        [Fact]
        public void GetParameter_NotFound_ReturnsNull()
        {
            var prop = typeof(IAttributeTestPoco).GetProperty("Id")!;
            var result = TrackablePropertyAttribute.GetParameter(prop, "nonexistent");
            Assert.Null(result);
        }
    }

    public interface IAttributeTestPoco
    {
        [TrackableProperty("sql.primary-key", "sql.column:id_col")]
        int Id { get; set; }

        string Name { get; set; }
    }
}
