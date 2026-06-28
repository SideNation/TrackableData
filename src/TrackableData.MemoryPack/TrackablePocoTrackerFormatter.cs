using System;
using System.Buffers;
using System.Reflection;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // A poco tracker holds heterogeneous (object) per-property values, so each changed value is
    // serialized by its declared property type via MemoryPackSerializer.Serialize(Type, value).
    public sealed class TrackablePocoTrackerFormatter<T>
        : MemoryPackFormatter<TrackablePocoTracker<T>>
        where T : ITrackablePoco<T>
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackablePocoTracker<T>? value)
#if NET7_0_OR_GREATER
            where TBufferWriter : default
#endif
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            writer.WriteCollectionHeader(value.ChangeMap.Count);
            foreach (var item in value.ChangeMap)
            {
                writer.WriteValue(item.Key.Name);
                writer.WriteValue(MemoryPackValueSerializer.Serialize(item.Key.PropertyType, item.Value.NewValue));
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackablePocoTracker<T>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var tracker = new TrackablePocoTracker<T>();
            for (var i = 0; i < length; i++)
            {
                var name = reader.ReadValue<string>()!;
                var bytes = reader.ReadValue<byte[]>()!;

                var property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    throw new InvalidOperationException($"Cannot find property '{name}' on {typeof(T)}.");

                var newValue = MemoryPackValueSerializer.Deserialize(property.PropertyType, bytes);
                tracker.TrackSet(property, null, newValue);
            }
            value = tracker;
        }
    }
}
