using System;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class RedisMapperUnitTest
    {
        private const string JsonOptionsFieldName = "_jsonOptions";

        // --- Poco Mapper Construction ---

        [Fact]
        public void PocoMapper_DefaultConstructor_Works()
        {
            var mapper = new TrackablePocoRedisMapper<ITestPerson>();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void PocoMapper_WithLogger_Works()
        {
            var logger = new RedisTestLogger();
            var mapper = new TrackablePocoRedisMapper<ITestPerson>(logger);
            Assert.NotNull(mapper);
        }

        [Fact]
        public void PocoMapper_DefaultJsonOptions_WritesUnicodeCompactJson()
        {
            var mapper = new TrackablePocoRedisMapper<ITestPerson>();
            var jsonOptions = GetJsonOptions(mapper);
            var json = JsonSerializer.Serialize(
                new JsonOptionsSample
                {
                    Text = "한글日本"
                },
                jsonOptions);

            Assert.Equal("{\"Text\":\"한글日本\"}", json);
        }

        [Fact]
        public void PocoMapper_WithJsonOptions_UsesProvidedOptions()
        {
            var expected = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var mapper = new TrackablePocoRedisMapper<ITestPerson>(new RedisTestLogger(), expected);
            var actual = GetJsonOptions(mapper);

            Assert.Same(expected, actual);
        }

        // --- Dictionary Mapper Construction ---

        [Fact]
        public void DictionaryMapper_DefaultConstructor_Works()
        {
            var mapper = new TrackableDictionaryRedisMapper<string, string>();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void DictionaryMapper_WithLogger_Works()
        {
            var logger = new RedisTestLogger();
            var mapper = new TrackableDictionaryRedisMapper<string, string>(logger);
            Assert.NotNull(mapper);
        }

        [Fact]
        public void DictionaryMapper_IntKey_Works()
        {
            var mapper = new TrackableDictionaryRedisMapper<int, string>();
            Assert.NotNull(mapper);
        }

        // --- List Mapper Construction ---

        [Fact]
        public void ListMapper_DefaultConstructor_Works()
        {
            var mapper = new TrackableListRedisMapper<string>();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void ListMapper_WithLogger_Works()
        {
            var logger = new RedisTestLogger();
            var mapper = new TrackableListRedisMapper<string>(logger);
            Assert.NotNull(mapper);
        }

        [Fact]
        public void ListMapper_IntType_Works()
        {
            var mapper = new TrackableListRedisMapper<int>();
            Assert.NotNull(mapper);
        }

        // --- Set Mapper Construction ---

        [Fact]
        public void SetMapper_DefaultConstructor_Works()
        {
            var mapper = new TrackableSetRedisMapper<string>();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void SetMapper_WithLogger_Works()
        {
            var logger = new RedisTestLogger();
            var mapper = new TrackableSetRedisMapper<string>(logger);
            Assert.NotNull(mapper);
        }

        [Fact]
        public void SetMapper_IntType_Works()
        {
            var mapper = new TrackableSetRedisMapper<int>();
            Assert.NotNull(mapper);
        }

        // --- Container Mapper Construction ---

        [Fact]
        public void ContainerMapper_DefaultConstructor_Works()
        {
            var mapper = new TrackableContainerRedisMapper<ITestDataContainer>();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void ContainerMapper_WithLogger_Works()
        {
            var logger = new RedisTestLogger();
            var mapper = new TrackableContainerRedisMapper<ITestDataContainer>(logger);
            Assert.NotNull(mapper);
        }

        private static JsonSerializerOptions GetJsonOptions(object mapper)
        {
            var field = mapper.GetType().GetField(JsonOptionsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return Assert.IsType<JsonSerializerOptions>(field.GetValue(mapper));
        }
    }

    internal class RedisTestLogger : ITrackableLogger
    {
        public int CallCount;
        public void LogDebug(string message, params object[] args) => CallCount++;
    }

    internal class JsonOptionsSample
    {
        public string Text { get; set; }
    }

    public interface ITestDataContainer : ITrackableContainer<ITestDataContainer>
    {
        TrackableDictionary<string, string> Names { get; set; }
        TrackableList<string> Tags { get; set; }
    }
}
