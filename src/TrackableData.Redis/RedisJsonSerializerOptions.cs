using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace TrackableData.Redis
{
    internal static class RedisJsonSerializerOptions
    {
        public static JsonSerializerOptions Default { get; } = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = false,
        };

        public static JsonSerializerOptions GetDefaultOr(JsonSerializerOptions jsonOptions)
        {
            return jsonOptions ?? Default;
        }
    }
}
