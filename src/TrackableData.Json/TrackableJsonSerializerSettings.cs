using Newtonsoft.Json;

namespace TrackableData.Json
{
    public static class TrackableJsonSerializerSettings
    {
        /// <summary>
        /// Convenience settings that auto-dispatch to the right tracker converter by reflecting the
        /// runtime type. This uses <c>Type.MakeGenericType</c> and is therefore NOT safe under
        /// Unity IL2CPP / AOT for value-type generic arguments. For AOT use
        /// <see cref="CreateForAot"/> plus the <see cref="TrackableJsonConverters"/> registrations.
        /// </summary>
        public static JsonSerializerSettings Create()
        {
            var settings = CreateForAot();
            settings.Converters.Add(new TrackerJsonConverter());
            return settings;
        }

        /// <summary>
        /// AOT / Unity IL2CPP-safe base settings. Register the concrete tracker converters you need
        /// with the <see cref="TrackableJsonConverters"/> extension methods (no runtime
        /// MakeGenericType is used).
        /// </summary>
        public static JsonSerializerSettings CreateForAot()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            };
        }
    }
}
