using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    public sealed class TrackableSetTrackerFormatter<T>
        : MemoryPackFormatter<TrackableSetTracker<T>>
        where T : notnull
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackableSetTracker<T>? value)
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
                writer.WriteUnmanaged(item.Value == TrackableSetOperation.Add);
                writer.WriteValue(item.Key);
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackableSetTracker<T>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var tracker = new TrackableSetTracker<T>();
            for (var i = 0; i < length; i++)
            {
                reader.ReadUnmanaged(out bool isAdd);
                var val = reader.ReadValue<T>()!;
                if (isAdd)
                    tracker.TrackAdd(val);
                else
                    tracker.TrackRemove(val);
            }
            value = tracker;
        }
    }
}
