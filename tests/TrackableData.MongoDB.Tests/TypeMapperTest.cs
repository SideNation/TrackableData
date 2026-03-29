using MongoDB.Bson.Serialization;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class TypeMapperTest
    {
        [Fact]
        public void RegisterMap_PrimitiveType_DoesNotThrow()
        {
            TypeMapper.RegisterMap(typeof(int));
            TypeMapper.RegisterMap(typeof(string));
            TypeMapper.RegisterMap(typeof(bool));
            TypeMapper.RegisterMap(typeof(decimal));
        }

        [Fact]
        public void RegisterMap_SameTypeTwice_DoesNotThrow()
        {
            TypeMapper.RegisterMap(typeof(TypeMapperTestClass));
            TypeMapper.RegisterMap(typeof(TypeMapperTestClass));
        }

        [Fact]
        public void RegisterMap_ClassType_Registers()
        {
            TypeMapper.RegisterMap(typeof(TypeMapperTestClass2));
            Assert.True(BsonClassMap.IsClassMapRegistered(typeof(TypeMapperTestClass2)));
        }
    }

    public class TypeMapperTestClass
    {
        public string Name { get; set; }
    }

    public class TypeMapperTestClass2
    {
        public int Value { get; set; }
    }
}
