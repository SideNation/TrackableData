using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    public sealed class TrackableDictionaryFormatter<TKey, TValue>
        : MemoryPackFormatter<TrackableDictionary<TKey, TValue>>
        where TKey : notnull
    {
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TrackableDictionary<TKey, TValue>? value)
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
                writer.WriteValue(item.Key);
                writer.WriteValue(item.Value);
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TrackableDictionary<TKey, TValue>? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            var dict = new TrackableDictionary<TKey, TValue>();
            for (var i = 0; i < length; i++)
            {
                var key = reader.ReadValue<TKey>()!;
                var val = reader.ReadValue<TValue>()!;
                dict.Add(key, val);
            }
            value = dict;
        }
    }
}
