using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    public sealed class TrackableListFormatter<T>
        : MemoryPackFormatter<TrackableList<T>>
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackableList<T>? value)
#if NET7_0_OR_GREATER
            where TBufferWriter : default
#endif
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            writer.WriteCollectionHeader(value.Count);
            for (var i = 0; i < value.Count; i++)
            {
                writer.WriteValue(value[i]);
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackableList<T>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var list = new TrackableList<T>();
            for (var i = 0; i < length; i++)
            {
                list.Add(reader.ReadValue<T>()!);
            }
            value = list;
        }
    }
}
