using System;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // Serializes a whole trackable container (the generated concrete type) as (memberName, value)
    // pairs; each member value (poco / dictionary / list / set) is serialized by its declared
    // member type, so the matching member formatters must also be registered. Generic over the
    // concrete container type so MemoryPack resolves it for the natural Serialize(container) call.
    public sealed class TrackableContainerFormatter<TContainer>
        : MemoryPackFormatter<TContainer>
        where TContainer : class, new()
    {
        private static readonly PropertyInfo[] Properties =
            ResolveContainerInterface().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        private static Type ResolveContainerInterface()
        {
            foreach (var iface in typeof(TContainer).GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITrackableContainer<>))
                    return iface.GetGenericArguments()[0];
            }
            throw new InvalidOperationException($"{typeof(TContainer)} is not a trackable container.");
        }

        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TContainer? value)
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
            scoped ref TContainer? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = default;
                return;
            }

            var container = new TContainer();
            for (var i = 0; i < length; i++)
            {
                var name = reader.ReadValue<string>()!;
                var bytes = reader.ReadValue<byte[]>()!;

                var property = Properties.FirstOrDefault(p => p.Name == name);
                if (property == null)
                    throw new InvalidOperationException($"Cannot find property '{name}' on {typeof(TContainer)}.");

                property.SetValue(container, MemoryPackValueSerializer.Deserialize(property.PropertyType, bytes));
            }
            value = container;
        }
    }
}
