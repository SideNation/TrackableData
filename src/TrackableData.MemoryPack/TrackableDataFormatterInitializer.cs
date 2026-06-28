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
        /// Registers formatters for a trackable poco and its tracker. Pass the generated concrete
        /// type and its poco interface, e.g. RegisterPocoFormatter&lt;TrackablePerson, IPerson&gt;().
        /// The whole-poco formatter is registered for the concrete type so MemoryPackSerializer
        /// resolves it for the natural Serialize(new TrackablePerson()) call; the tracker formatter
        /// covers TrackablePocoTracker&lt;IPerson&gt; (cast poco.Tracker to it, like dict/list/set).
        /// Registration is AOT-safe; the formatters read members by reflection at runtime
        /// (IL2CPP-compatible as long as the generated type is preserved, which it is since
        /// application code references it).
        /// </summary>
        public static void RegisterPocoFormatter<TTrackable, TPoco>()
            where TTrackable : class, TPoco, new()
            where TPoco : ITrackablePoco<TPoco>
        {
            // Register for the concrete type (standalone Serialize(new TrackableXxx())) and the
            // interface (poco members of a container are typed by their interface).
            MemoryPackFormatterProvider.Register(new TrackablePocoFormatter<TTrackable>());
            MemoryPackFormatterProvider.Register(new TrackablePocoFormatter<TPoco>());
            MemoryPackFormatterProvider.Register(new TrackablePocoTrackerFormatter<TPoco>());
        }

        /// <summary>
        /// Registers formatters for a trackable container and its tracker. Pass the generated
        /// concrete container and container-tracker types, e.g.
        /// RegisterContainerFormatter&lt;TrackableGame, TrackableGameTracker&gt;(). Both are
        /// registered for their concrete types so MemoryPackSerializer resolves them for the natural
        /// Serialize(container) and Serialize(container.Tracker) calls. The formatters for the
        /// container's member types must be registered as well. Registration is AOT-safe; the
        /// formatters read members by reflection at runtime (IL2CPP-compatible with preservation).
        /// </summary>
        public static void RegisterContainerFormatter<TContainer, TContainerTracker>()
            where TContainer : class, new()
            where TContainerTracker : class, IContainerTracker, new()
        {
            MemoryPackFormatterProvider.Register(new TrackableContainerFormatter<TContainer>());
            MemoryPackFormatterProvider.Register(new TrackableContainerTrackerFormatter<TContainerTracker>());
        }
    }
}
