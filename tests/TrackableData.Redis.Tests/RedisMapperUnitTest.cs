using System;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class RedisMapperUnitTest
    {
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

    }

    internal class RedisTestLogger : ITrackableLogger
    {
        public int CallCount;
        public void LogDebug(string message, params object[] args) => CallCount++;
    }

    public interface ITestDataContainer : ITrackableContainer<ITestDataContainer>
    {
        TrackableDictionary<string, string> Names { get; set; }
        TrackableList<string> Tags { get; set; }
    }
}
