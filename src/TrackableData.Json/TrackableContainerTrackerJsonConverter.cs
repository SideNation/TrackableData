using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    public sealed class TrackableContainerTrackerJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IContainerTracker).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);
            var tracker = (ITracker)Activator.CreateInstance(objectType)!;
            foreach (var property in jsonObject.Properties())
            {
                var trackerProperty = objectType.GetProperty(
                    property.Name + JsonChangeTokens.TrackerSuffix,
                    BindingFlags.Instance | BindingFlags.Public);
                if (trackerProperty == null)
                    throw new JsonSerializationException($"Cannot find tracker property for '{property.Name}' on {objectType}.");

                var subTracker = property.Value.ToObject(trackerProperty.PropertyType, serializer);
                trackerProperty.SetValue(tracker, subTracker);
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

            writer.WriteStartObject();
            foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!typeof(ITracker).IsAssignableFrom(property.PropertyType))
                    continue;

                var subTracker = (ITracker?)property.GetValue(value);
                if (subTracker == null || !subTracker.HasChange)
                    continue;

                var propertyName = property.Name.EndsWith(JsonChangeTokens.TrackerSuffix, StringComparison.Ordinal)
                    ? property.Name.Substring(0, property.Name.Length - JsonChangeTokens.TrackerSuffix.Length)
                    : property.Name;
                writer.WritePropertyName(propertyName);
                serializer.Serialize(writer, subTracker);
            }
            writer.WriteEndObject();
        }
    }
}
