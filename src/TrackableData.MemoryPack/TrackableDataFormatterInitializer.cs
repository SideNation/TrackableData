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

        /// <summary>
        /// Registers formatters for a trackable poco (TrackableData generated type) and its tracker.
        /// Registration is AOT-safe (concrete types); the formatters read members by reflection at
        /// runtime, which is IL2CPP-compatible as long as the generated type is preserved (it is,
        /// since application code references it).
        /// </summary>
        public static void RegisterPocoFormatter<T>()
            where T : ITrackablePoco<T>
        {
            MemoryPackFormatterProvider.Register(new TrackablePocoFormatter<T>());
            MemoryPackFormatterProvider.Register(new TrackablePocoTrackerFormatter<T>());
        }

        /// <summary>
        /// Registers formatters for a trackable container and its tracker. The formatters for the
        /// container's member types must be registered as well. Registration is AOT-safe; the
        /// formatters read members by reflection at runtime (IL2CPP-compatible with preservation).
        /// </summary>
        public static void RegisterContainerFormatter<T>()
            where T : ITrackableContainer<T>
        {
            MemoryPackFormatterProvider.Register(new TrackableContainerFormatter<T>());
            MemoryPackFormatterProvider.Register(new TrackableContainerTrackerFormatter<T>());
        }
    }
}
