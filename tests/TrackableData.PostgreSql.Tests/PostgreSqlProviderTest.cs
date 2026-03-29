using System;
using Xunit;

namespace TrackableData.PostgreSql.Tests
{
    public class PostgreSqlProviderTest
    {
        private readonly PostgreSqlProvider _provider = PostgreSqlProvider.Instance;

        // --- Instance ---

        [Fact]
        public void Instance_IsSingleton()
        {
            Assert.Same(PostgreSqlProvider.Instance, PostgreSqlProvider.Instance);
        }

        // --- EscapeName ---

        [Fact]
        public void EscapeName_WrapsWithQuotes()
        {
            Assert.Equal("\"MyTable\"", _provider.EscapeName("MyTable"));
        }

        [Fact]
        public void EscapeName_SimpleColumn()
        {
            Assert.Equal("\"id\"", _provider.EscapeName("id"));
        }

        // --- GetSqlType ---

        [Theory]
        [InlineData(typeof(bool), "BOOLEAN")]
        [InlineData(typeof(short), "SMALLINT")]
        [InlineData(typeof(int), "INTEGER")]
        [InlineData(typeof(long), "BIGINT")]
        [InlineData(typeof(float), "FLOAT4")]
        [InlineData(typeof(double), "FLOAT8")]
        [InlineData(typeof(decimal), "NUMERIC")]
        [InlineData(typeof(DateTime), "TIMESTAMP")]
        [InlineData(typeof(DateTimeOffset), "TIMESTAMPTZ")]
        [InlineData(typeof(TimeSpan), "TIME")]
        [InlineData(typeof(Guid), "UUID")]
        [InlineData(typeof(byte[]), "BYTEA")]
        [InlineData(typeof(byte), "SMALLINT")]
        public void GetSqlType_ReturnsCorrectType(Type type, string expected)
        {
            Assert.Equal(expected, _provider.GetSqlType(type));
        }

        [Fact]
        public void GetSqlType_String_DefaultLength()
        {
            Assert.Equal("VARCHAR(10000)", _provider.GetSqlType(typeof(string)));
        }

        [Fact]
        public void GetSqlType_String_CustomLength()
        {
            Assert.Equal("VARCHAR(100)", _provider.GetSqlType(typeof(string), 100));
        }

        // --- GetConvertToSqlValueFunc ---

        [Fact]
        public void ConvertToSqlValue_Bool_True()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(bool));
            Assert.Equal("TRUE", func(true));
        }

        [Fact]
        public void ConvertToSqlValue_Bool_False()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(bool));
            Assert.Equal("FALSE", func(false));
        }

        [Fact]
        public void ConvertToSqlValue_Int()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(int));
            Assert.Equal("42", func(42));
        }

        [Fact]
        public void ConvertToSqlValue_String()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(string));
            Assert.Equal("'hello'", func("hello"));
        }

        [Fact]
        public void ConvertToSqlValue_String_EscapesSingleQuotes()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(string));
            Assert.Equal("'it''s'", func("it's"));
        }

        [Fact]
        public void ConvertToSqlValue_String_Null()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(string));
            Assert.Equal("NULL", func(null));
        }

        [Fact]
        public void ConvertToSqlValue_Guid()
        {
            var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            var func = _provider.GetConvertToSqlValueFunc(typeof(Guid));
            Assert.Equal("'12345678-1234-1234-1234-123456789abc'", func(guid));
        }

        [Fact]
        public void ConvertToSqlValue_NullableInt_HasValue()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(int?));
            Assert.Equal("42", func((int?)42));
        }

        [Fact]
        public void ConvertToSqlValue_NullableInt_Null()
        {
            var func = _provider.GetConvertToSqlValueFunc(typeof(int?));
            Assert.Equal("NULL", func((int?)null));
        }

        // --- GetConvertFromDbValueFunc ---

        [Fact]
        public void ConvertFromDbValue_String()
        {
            var func = _provider.GetConvertFromDbValueFunc(typeof(string));
            Assert.Equal("hello", func("hello"));
        }

        [Fact]
        public void ConvertFromDbValue_String_DbNull()
        {
            var func = _provider.GetConvertFromDbValueFunc(typeof(string));
            Assert.Null(func(DBNull.Value));
        }

        [Fact]
        public void ConvertFromDbValue_Int()
        {
            var func = _provider.GetConvertFromDbValueFunc(typeof(int));
            Assert.Equal(42, func(42));
        }

        // --- BuildInsertIntoSql ---

        [Fact]
        public void BuildInsertIntoSql_WithoutIdentity()
        {
            var sql = _provider.BuildInsertIntoSql("MyTable", "\"col1\",\"col2\"", "'a','b'", null);
            Assert.Contains("INSERT INTO \"MyTable\"", sql);
            Assert.Contains("VALUES ('a','b')", sql);
            Assert.DoesNotContain("RETURNING", sql);
        }

        [Fact]
        public void BuildInsertIntoSql_WithIdentity()
        {
            var identity = new ColumnProperty("Id", "\"Id\"", typeof(int), isIdentity: true);
            var sql = _provider.BuildInsertIntoSql("MyTable", "\"col1\"", "'a'", identity);
            Assert.Contains("RETURNING \"Id\"", sql);
        }
    }

    public class ColumnDefinitionTest
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var def = new ColumnDefinition("Name", typeof(string), 100);
            Assert.Equal("Name", def.Name);
            Assert.Equal(typeof(string), def.Type);
            Assert.Equal(100, def.Length);
        }

        [Fact]
        public void Constructor_DefaultValues()
        {
            var def = new ColumnDefinition("Id");
            Assert.Equal("Id", def.Name);
            Assert.Null(def.Type);
            Assert.Equal(0, def.Length);
        }
    }

    public class ColumnPropertyTest
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var prop = new ColumnProperty(
                "Name", "\"Name\"",
                type: typeof(string),
                length: 50,
                isIdentity: true);

            Assert.Equal("Name", prop.Name);
            Assert.Equal("\"Name\"", prop.EscapedName);
            Assert.Equal(typeof(string), prop.Type);
            Assert.Equal(50, prop.Length);
            Assert.True(prop.IsIdentity);
        }

        [Fact]
        public void Constructor_DefaultValues()
        {
            var prop = new ColumnProperty("Id", "\"Id\"");
            Assert.Equal("Id", prop.Name);
            Assert.Null(prop.Type);
            Assert.Equal(0, prop.Length);
            Assert.False(prop.IsIdentity);
            Assert.Null(prop.PropertyInfo);
            Assert.Null(prop.ConvertToSqlValue);
            Assert.Null(prop.ConvertFromDbValue);
        }
    }
}
