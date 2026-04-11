using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    public sealed class TrackableSetFormatter<T>
        : MemoryPackFormatter<TrackableSet<T>>
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackableSet<T>? value)
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
            foreach (var item in value)
            {
                writer.WriteValue(item);
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackableSet<T>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var set = new TrackableSet<T>();
            for (var i = 0; i < length; i++)
            {
                set.Add(reader.ReadValue<T>()!);
            }
            value = set;
        }
    }
}
