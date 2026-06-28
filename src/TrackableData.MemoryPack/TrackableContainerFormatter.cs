using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // Serializes a whole trackable container as (memberName, value) pairs; each member value
    // (a poco / dictionary / list / set) is serialized by its declared member type, so the
    // matching member formatters must also be registered.
    public sealed class TrackableContainerFormatter<T>
        : MemoryPackFormatter<T>
        where T : ITrackableContainer<T>
    {
        private static readonly Type ContainerType = TrackableResolver.GetContainerTrackerType(typeof(T))!;
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

            var container = (T)Activator.CreateInstance(ContainerType)!;
            for (var i = 0; i < length; i++)
            {
                var name = reader.ReadValue<string>()!;
                var bytes = reader.ReadValue<byte[]>()!;

                var property = Properties.FirstOrDefault(p => p.Name == name);
                if (property == null)
                    throw new InvalidOperationException($"Cannot find property '{name}' on {typeof(T)}.");

                property.SetValue(container, MemoryPackValueSerializer.Deserialize(property.PropertyType, bytes));
            }
            value = container;
        }
    }
}
