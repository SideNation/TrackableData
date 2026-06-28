using Newtonsoft.Json;

namespace TrackableData.Json
{
    /// <summary>
    /// AOT / Unity IL2CPP-safe registration of tracker converters. Each method statically
    /// instantiates a concrete-typed converter, so no runtime <c>Type.MakeGenericType</c> is needed
    /// (unlike <see cref="TrackerJsonConverter"/>, which reflects the runtime type and is therefore
    /// not safe under IL2CPP for value-type generic arguments).
    ///
    /// Register every tracker type you serialize, e.g.:
    /// <code>
    /// var settings = TrackableJsonSerializerSettings.CreateForAot()
    ///     .AddPocoTrackerConverter&lt;IPlayer&gt;()
    ///     .AddDictionaryTrackerConverter&lt;int, string&gt;()
    ///     .AddListTrackerConverter&lt;string&gt;()
    ///     .AddContainerTrackerConverter();
    /// </code>
    /// </summary>
    public static class TrackableJsonConverters
    {
        public static JsonSerializerSettings AddPocoTrackerConverter<T>(this JsonSerializerSettings settings)
        {
            settings.Converters.Add(new TrackablePocoTrackerJsonConverter<T>());
            return settings;
        }

        public static JsonSerializerSettings AddDictionaryTrackerConverter<TKey, TValue>(this JsonSerializerSettings settings)
            where TKey : notnull
        {
            settings.Converters.Add(new TrackableDictionaryTrackerJsonConverter<TKey, TValue>());
            return settings;
        }

        public static JsonSerializerSettings AddListTrackerConverter<T>(this JsonSerializerSettings settings)
        {
            settings.Converters.Add(new TrackableListTrackerJsonConverter<T>());
            return settings;
        }

        public static JsonSerializerSettings AddSetTrackerConverter<T>(this JsonSerializerSettings settings)
            where T : notnull
        {
            settings.Converters.Add(new TrackableSetTrackerJsonConverter<T>());
            return settings;
        }

        public static JsonSerializerSettings AddContainerTrackerConverter(this JsonSerializerSettings settings)
        {
            settings.Converters.Add(new TrackableContainerTrackerJsonConverter());
            return settings;
        }
    }
}
