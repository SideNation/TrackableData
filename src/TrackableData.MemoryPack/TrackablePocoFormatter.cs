using System;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // Serializes a whole trackable poco as (propertyName, value) pairs (data members only, taken
    // from the poco interface). Registered for BOTH the concrete generated type (for the natural
    // Serialize(new TrackablePerson()) call) and the poco interface (for poco members embedded in a
    // container, whose declared type is the interface) — TRegister is whichever it was registered
    // for; the concrete type to instantiate is resolved either way.
    public sealed class TrackablePocoFormatter<TRegister>
        : MemoryPackFormatter<TRegister>
    {
        private static readonly Type PocoInterface = ResolvePocoInterface();
        private static readonly Type ConcreteType = typeof(TRegister).IsInterface
            ? TrackableResolver.GetPocoTrackerType(PocoInterface)!
            : typeof(TRegister);
        private static readonly PropertyInfo[] Properties =
            PocoInterface.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        private static Type ResolvePocoInterface()
        {
            if (typeof(TRegister).IsInterface)
                return typeof(TRegister);
            foreach (var iface in typeof(TRegister).GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITrackablePoco<>))
                    return iface.GetGenericArguments()[0];
            }
            throw new InvalidOperationException($"{typeof(TRegister)} is not a trackable poco.");
        }

        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TRegister? value)
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
            scoped ref TRegister? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = default;
                return;
            }

            var poco = (TRegister)Activator.CreateInstance(ConcreteType)!;
            for (var i = 0; i < length; i++)
            {
                var name = reader.ReadValue<string>()!;
                var bytes = reader.ReadValue<byte[]>()!;

                var property = Properties.FirstOrDefault(p => p.Name == name);
                if (property == null)
                    throw new InvalidOperationException($"Cannot find property '{name}' on {PocoInterface}.");

                property.SetValue(poco, MemoryPackValueSerializer.Deserialize(property.PropertyType, bytes));
            }
            value = poco;
        }
    }
}
