using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackableData.Json
{
    internal static class JsonKeyConverter
    {
        public static string ToPropertyName<TKey>(TKey key, JsonSerializer serializer)
            where TKey : notnull
        {
            if (key is string stringKey)
                return stringKey;

            var token = JToken.FromObject(key, serializer);
            return token.Type == JTokenType.String
                ? token.Value<string>()!
                : token.ToString(Formatting.None);
        }

        public static TKey FromPropertyName<TKey>(string propertyName, JsonSerializer serializer)
            where TKey : notnull
        {
            if (typeof(TKey) == typeof(string))
                return (TKey)(object)propertyName;

            try
            {
                var token = JToken.Parse(propertyName);
                return token.ToObject<TKey>(serializer)!;
            }
            catch (JsonReaderException)
            {
                var converter = TypeDescriptor.GetConverter(typeof(TKey));
                if (converter.CanConvertFrom(typeof(string)))
                    return (TKey)converter.ConvertFromInvariantString(propertyName)!;

                throw new JsonSerializationException($"Cannot convert dictionary key '{propertyName}' to {typeof(TKey)}.");
            }
        }
    }
}
