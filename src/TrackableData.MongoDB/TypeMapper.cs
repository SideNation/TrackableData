using System;
using System.Linq;
using MongoDB.Bson.Serialization;

namespace TrackableData.MongoDB
{
    public static class TypeMapper
    {
        public static void RegisterTrackablePocoMap(Type trackableType)
        {
            if (BsonClassMap.IsClassMapRegistered(trackableType))
                return;

            var classMap = new BsonClassMap(trackableType);
            classMap.AutoMap();
            classMap.SetIgnoreExtraElements(true);

            // a property marked with [TrackableProperty("mongodb.identity")] becomes the _id
            var pocoType = TrackableResolver.GetPocoType(trackableType);
            if (pocoType != null)
            {
                var identityProperty = pocoType.GetProperties().FirstOrDefault(
                    p => TrackablePropertyAttribute.GetParameter(p, "mongodb.identity") != null);
                if (identityProperty != null)
                    classMap.MapIdProperty(identityProperty.Name);
            }

            BsonClassMap.RegisterClassMap(classMap);
        }

        public static void RegisterMap(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return;

            if (BsonClassMap.IsClassMapRegistered(type))
                return;

            try
            {
                var classMap = new BsonClassMap(type);
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
                BsonClassMap.RegisterClassMap(classMap);
            }
            catch
            {
                // ignore registration failure for types that don't need mapping
            }
        }
    }
}
