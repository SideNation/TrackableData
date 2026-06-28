using Newtonsoft.Json;

namespace TrackableData.Json
{
    public static class TrackableJsonSerializerSettings
    {
        public static JsonSerializerSettings Create()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            };
            settings.Converters.Add(new TrackerJsonConverter());
            return settings;
        }
    }
}
