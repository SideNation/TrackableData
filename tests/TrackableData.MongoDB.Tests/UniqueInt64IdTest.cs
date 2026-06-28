using System.Collections.Generic;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class UniqueInt64IdTest
    {
        [Fact]
        public void GenerateNewId_IsNonZero()
        {
            Assert.NotEqual(0, UniqueInt64Id.GenerateNewId());
        }

        [Fact]
        public void GenerateNewId_ConsecutiveAreDistinct()
        {
            Assert.NotEqual(UniqueInt64Id.GenerateNewId(), UniqueInt64Id.GenerateNewId());
        }

        [Fact]
        public void GenerateNewId_ManyAreUnique()
        {
            var ids = new HashSet<long>();
            for (var i = 0; i < 1000; i++)
                Assert.True(ids.Add(UniqueInt64Id.GenerateNewId()));
        }
    }
}
