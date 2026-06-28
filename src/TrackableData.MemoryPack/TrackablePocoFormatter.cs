using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // Serializes a whole trackable poco as (propertyName, value) pairs; each value is serialized
    // by its declared property type via MemoryPackSerializer.Serialize(Type, value).
    public sealed class TrackablePocoFormatter<T>
        : MemoryPackFormatter<T>
        where T : ITrackablePoco<T>
    {
        private static readonly Type TrackableType = TrackableResolver.GetPocoTrackerType(typeof(T))!;
        private static readonly PropertyInfo[] Properties =
            typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref T? value)
#if NET7_0_OR_GREATER
            where TBufferWriter : default
#endif
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            writer.WriteCollectionHeader(Properties.Length);
            foreach (var property in Properties)
            {
                writer.WriteValue(property.Name);
                writer.WriteValue(MemoryPackValueSerializer.Serialize(property.PropertyType, property.GetValue(value)));
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref T? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = default;
                return;
            }

            var poco = (T)Activator.CreateInstance(TrackableType)!;
            for (var i = 0; i < length; i++)
            {
                var name = reader.ReadValue<string>()!;
                var bytes = reader.ReadValue<byte[]>()!;

                var property = Properties.FirstOrDefault(p => p.Name == name);
                if (property == null)
                    throw new InvalidOperationException($"Cannot find property '{name}' on {typeof(T)}.");

                property.SetValue(poco, MemoryPackValueSerializer.Deserialize(property.PropertyType, bytes));
            }
            value = poco;
        }
    }
}
