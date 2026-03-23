using MemoryPack;

namespace TrackableData.MemoryPack
{
    /// <summary>
    /// Provides static methods to register TrackableData formatters with MemoryPack.
    /// Call RegisterFormatters methods at application startup to enable serialization.
    /// AOT-safe: all registrations are explicit with concrete types.
    /// </summary>
    public static class TrackableDataFormatterInitializer
    {
        /// <summary>
        /// Registers formatters for TrackableDictionary and its tracker.
        /// </summary>
        public static void RegisterDictionaryFormatter<TKey, TValue>()
            where TKey : notnull
        {
            MemoryPackFormatterProvider.Register(new TrackableDictionaryFormatter<TKey, TValue>());
            MemoryPackFormatterProvider.Register(new TrackableDictionaryTrackerFormatter<TKey, TValue>());
        }

        /// <summary>
        /// Registers formatters for TrackableList and its tracker.
        /// </summary>
        public static void RegisterListFormatter<T>()
        {
            MemoryPackFormatterProvider.Register(new TrackableListFormatter<T>());
            MemoryPackFormatterProvider.Register(new TrackableListTrackerFormatter<T>());
        }

        /// <summary>
        /// Registers formatters for TrackableSet and its tracker.
        /// </summary>
        public static void RegisterSetFormatter<T>()
            where T : notnull
        {
            MemoryPackFormatterProvider.Register(new TrackableSetFormatter<T>());
            MemoryPackFormatterProvider.Register(new TrackableSetTrackerFormatter<T>());
        }
    }
}
