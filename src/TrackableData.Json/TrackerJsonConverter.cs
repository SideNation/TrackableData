using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;

namespace TrackableData.Json
{
    public sealed class TrackerJsonConverter : JsonConverter
    {
        private readonly ConcurrentDictionary<Type, JsonConverter> _converterMap = new ConcurrentDictionary<Type, JsonConverter>();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ITracker).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            return FindConverter(objectType).ReadJson(reader, objectType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            FindConverter(value.GetType()).WriteJson(writer, value, serializer);
        }

        private JsonConverter FindConverter(Type objectType)
        {
            return _converterMap.GetOrAdd(objectType, CreateConverter);
        }

        private static JsonConverter CreateConverter(Type objectType)
        {
            var pocoArguments = GetTrackerArguments(objectType, typeof(IPocoTracker<>));
            if (pocoArguments != null)
                return (JsonConverter)Activator.CreateInstance(typeof(TrackablePocoTrackerJsonConverter<>).MakeGenericType(pocoArguments))!;

            if (typeof(IContainerTracker).IsAssignableFrom(objectType))
                return new TrackableContainerTrackerJsonConverter();

            var dictionaryArguments = GetTrackerArguments(objectType, typeof(IDictionaryTracker<,>));
            if (dictionaryArguments != null)
            {
                return (JsonConverter)Activator.CreateInstance(
                    typeof(TrackableDictionaryTrackerJsonConverter<,>).MakeGenericType(dictionaryArguments))!;
            }

            var listArguments = GetTrackerArguments(objectType, typeof(IListTracker<>));
            if (listArguments != null)
                return (JsonConverter)Activator.CreateInstance(typeof(TrackableListTrackerJsonConverter<>).MakeGenericType(listArguments))!;

            var setArguments = GetTrackerArguments(objectType, typeof(ISetTracker<>));
            if (setArguments != null)
                return (JsonConverter)Activator.CreateInstance(typeof(TrackableSetTrackerJsonConverter<>).MakeGenericType(setArguments))!;

            throw new JsonSerializationException($"Cannot convert tracker type: {objectType}.");
        }

        private static Type[]? GetTrackerArguments(Type objectType, Type genericInterfaceType)
        {
            var trackerInterface = objectType.GetInterfaces()
                .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == genericInterfaceType);
            return trackerInterface?.GetGenericArguments();
        }
    }
}
