using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    public sealed class TrackableDictionaryTrackerFormatter<TKey, TValue>
        : MemoryPackFormatter<TrackableDictionaryTracker<TKey, TValue>>
        where TKey : notnull
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackableDictionaryTracker<TKey, TValue>? value)
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
                writer.WriteUnmanaged((byte)item.Value.Operation);
                writer.WriteValue(item.Key);
                writer.WriteValue(item.Value.NewValue);
                writer.WriteValue(item.Value.OldValue);
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackableDictionaryTracker<TKey, TValue>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var tracker = new TrackableDictionaryTracker<TKey, TValue>();
            for (var i = 0; i < length; i++)
            {
                reader.ReadUnmanaged(out byte op);
                var key = reader.ReadValue<TKey>()!;
                var newValue = reader.ReadValue<TValue>()!;
                var oldValue = reader.ReadValue<TValue>()!;

                switch ((TrackableDictionaryOperation)op)
                {
                    case TrackableDictionaryOperation.Add:
                        tracker.TrackAdd(key, newValue);
                        break;
                    case TrackableDictionaryOperation.Remove:
                        tracker.TrackRemove(key, oldValue);
                        break;
                    case TrackableDictionaryOperation.Modify:
                        tracker.TrackModify(key, oldValue, newValue);
                        break;
                }
            }
            value = tracker;
        }
    }
}
