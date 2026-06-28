using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    public sealed class TrackablePocoTrackerJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TrackablePocoTracker<T>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);
            var tracker = new TrackablePocoTracker<T>();
            foreach (var property in jsonObject.Properties())
            {
                var propertyInfo = typeof(T).GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo == null)
                    throw new JsonSerializationException($"Cannot find property '{property.Name}' on {typeof(T)}.");

                var newValue = property.Value.ToObject(propertyInfo.PropertyType, serializer);
                tracker.TrackSet(propertyInfo, null, newValue);
            }

            return tracker;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var tracker = (TrackablePocoTracker<T>)value;
            writer.WriteStartObject();
            foreach (var item in tracker.ChangeMap)
            {
                writer.WritePropertyName(item.Key.Name);
                serializer.Serialize(writer, item.Value.NewValue);
            }
            writer.WriteEndObject();
        }
    }
}
