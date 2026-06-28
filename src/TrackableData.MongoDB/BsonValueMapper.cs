using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace TrackableData.MongoDB
{
    internal static class BsonValueMapper
    {
        private const string ValueElementName = "value";

        public static BsonValue ToBsonValue<T>(T value)
        {
            if (ReferenceEquals(value, null))
                return BsonNull.Value;

            var type = typeof(T);
            TypeMapper.RegisterMap(type);

            var doc = new BsonDocument();
            using (var writer = new BsonDocumentWriter(doc))
            {
                writer.WriteStartDocument();
                writer.WriteName(ValueElementName);
                BsonSerializer.Serialize(writer, type, value);
                writer.WriteEndDocument();
            }
            return doc[ValueElementName];
        }

        public static T ToValue<T>(BsonValue value)
        {
            if (value == null || value.IsBsonNull)
                return default(T);

            TypeMapper.RegisterMap(typeof(T));

            var doc = new BsonDocument(ValueElementName, value);
            using (var reader = new BsonDocumentReader(doc))
            {
                reader.ReadStartDocument();
                reader.ReadBsonType();
                reader.ReadName();
                var result = BsonSerializer.Deserialize(reader, typeof(T));
                reader.ReadEndDocument();
                return (T)result;
            }
        }
    }
}
