using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    public sealed class TrackableDictionaryTrackerJsonConverter<TKey, TValue> : JsonConverter
        where TKey : notnull
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TrackableDictionaryTracker<TKey, TValue>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);
            var tracker = new TrackableDictionaryTracker<TKey, TValue>();
            foreach (var property in jsonObject.Properties())
            {
                if (property.Name.Length == 0)
                    throw new JsonSerializationException("Dictionary tracker property name cannot be empty.");

                var operation = property.Name[0];
                var key = JsonKeyConverter.FromPropertyName<TKey>(property.Name.Substring(1), serializer);
                switch (operation)
                {
                    case JsonChangeTokens.Add:
                        tracker.TrackAdd(key, property.Value.ToObject<TValue>(serializer)!);
                        break;
                    case JsonChangeTokens.Remove:
                        tracker.TrackRemove(key, default!);
                        break;
                    case JsonChangeTokens.Modify:
                        tracker.TrackModify(key, default!, property.Value.ToObject<TValue>(serializer)!);
                        break;
                    default:
                        throw new JsonSerializationException($"Unknown dictionary tracker operation: {operation}.");
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

            var tracker = (TrackableDictionaryTracker<TKey, TValue>)value;
            writer.WriteStartObject();
            foreach (var item in tracker.ChangeMap)
            {
                var key = JsonKeyConverter.ToPropertyName(item.Key, serializer);
                switch (item.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                        writer.WritePropertyName(JsonChangeTokens.Add + key);
                        serializer.Serialize(writer, item.Value.NewValue);
                        break;
                    case TrackableDictionaryOperation.Remove:
                        writer.WritePropertyName(JsonChangeTokens.Remove + key);
                        writer.WriteValue(0);
                        break;
                    case TrackableDictionaryOperation.Modify:
                        writer.WritePropertyName(JsonChangeTokens.Modify + key);
                        serializer.Serialize(writer, item.Value.NewValue);
                        break;
                }
            }
            writer.WriteEndObject();
        }
    }
}
