using System;
using System.Buffers;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // Serializes a value of a runtime type into its own buffer so it can be embedded as a byte[]
    // inside another formatter. This avoids re-entering the thread-static MemoryPackSerializer
    // writer (which would corrupt the outer serialization).
    internal static class MemoryPackValueSerializer
    {
        public static byte[] Serialize(Type type, object? value)
        {
            var buffer = new ArrayBufferWriter<byte>();
            MemoryPackSerializer.Serialize<ArrayBufferWriter<byte>>(type, buffer, value);
            return buffer.WrittenSpan.ToArray();
        }

        public static object? Deserialize(Type type, byte[] bytes)
        {
            return MemoryPackSerializer.Deserialize(type, bytes);
        }
    }
}
