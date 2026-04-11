using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    public sealed class TrackableListTrackerFormatter<T>
        : MemoryPackFormatter<TrackableListTracker<T>>
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackableListTracker<T>? value)
#if NET7_0_OR_GREATER
            where TBufferWriter : default
#endif
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            writer.WriteCollectionHeader(value.ChangeList.Count);
            foreach (var item in value.ChangeList)
            {
                writer.WriteUnmanaged((byte)item.Operation);
                writer.WriteUnmanaged(item.Index);
                writer.WriteValue(item.NewValue);
                writer.WriteValue(item.OldValue);
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackableListTracker<T>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var tracker = new TrackableListTracker<T>();
            for (var i = 0; i < length; i++)
            {
                reader.ReadUnmanaged(out byte op);
                reader.ReadUnmanaged(out int index);
                var newVal = reader.ReadValue<T>()!;
                var oldVal = reader.ReadValue<T>()!;

                switch ((TrackableListOperation)op)
                {
                    case TrackableListOperation.Insert:
                        tracker.TrackInsert(index, newVal);
                        break;
                    case TrackableListOperation.Remove:
                        tracker.TrackRemove(index, oldVal);
                        break;
                    case TrackableListOperation.Modify:
                        tracker.TrackModify(index, oldVal, newVal);
                        break;
                    case TrackableListOperation.PushFront:
                        tracker.TrackPushFront(newVal);
                        break;
                    case TrackableListOperation.PushBack:
                        tracker.TrackPushBack(newVal);
                        break;
                    case TrackableListOperation.PopFront:
                        tracker.TrackPopFront(oldVal);
                        break;
                    case TrackableListOperation.PopBack:
                        tracker.TrackPopBack(oldVal);
                        break;
                }
            }
            value = tracker;
        }
    }
}
