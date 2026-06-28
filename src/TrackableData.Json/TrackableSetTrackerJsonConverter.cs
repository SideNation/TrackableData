using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    public sealed class TrackableSetTrackerJsonConverter<T> : JsonConverter
        where T : notnull
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TrackableSetTracker<T>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);
            var tracker = new TrackableSetTracker<T>();
            foreach (var property in jsonObject.Properties())
            {
                var isAdd = property.Name == JsonChangeTokens.AddPropertyName;
                var isRemove = property.Name == JsonChangeTokens.RemovePropertyName;
                if (!isAdd && !isRemove)
                    throw new JsonSerializationException($"Unknown set tracker operation: {property.Name}.");

                if (property.Value is not JArray values)
                    throw new JsonSerializationException("Set tracker operation value must be a JSON array.");

                foreach (var value in values)
                {
                    var item = value.ToObject<T>(serializer)!;
                    if (isAdd)
                        tracker.TrackAdd(item);
                    else
                        tracker.TrackRemove(item);
                }
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

            var tracker = (TrackableSetTracker<T>)value;
            writer.WriteStartObject();
            if (tracker.AddValues.Any())
            {
                writer.WritePropertyName(JsonChangeTokens.AddPropertyName);
                serializer.Serialize(writer, tracker.AddValues);
            }

            if (tracker.RemoveValues.Any())
            {
                writer.WritePropertyName(JsonChangeTokens.RemovePropertyName);
                serializer.Serialize(writer, tracker.RemoveValues);
            }
            writer.WriteEndObject();
        }
    }
}
