using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MemoryPack;

namespace TrackableData.MemoryPack
{
    // A container tracker is a bag of named sub-trackers (one per container member). Only the
    // changed ones are written, each serialized by its concrete sub-tracker type. Generic over the
    // concrete container tracker type so MemoryPack resolves it for the natural
    // Serialize(container.Tracker) call.
    public sealed class TrackableContainerTrackerFormatter<TContainerTracker>
        : MemoryPackFormatter<TContainerTracker>
        where TContainerTracker : class, IContainerTracker, new()
    {
        private static readonly PropertyInfo[] TrackerProperties = typeof(TContainerTracker)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => typeof(ITracker).IsAssignableFrom(p.PropertyType))
            .ToArray();

        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref TContainerTracker? value)
#if NET7_0_OR_GREATER
            where TBufferWriter : default
#endif
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var changed = new List<PropertyInfo>();
            foreach (var property in TrackerProperties)
            {
                var subTracker = (ITracker?)property.GetValue(value);
                if (subTracker != null && subTracker.HasChange)
                    changed.Add(property);
            }

            writer.WriteCollectionHeader(changed.Count);
            foreach (var property in changed)
            {
                writer.WriteValue(property.Name);
                writer.WriteValue(MemoryPackValueSerializer.Serialize(property.PropertyType, property.GetValue(value)));
            }
        }

        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref TContainerTracker? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = default;
                return;
            }

            var tracker = new TContainerTracker();
            for (var i = 0; i < length; i++)
            {
                var name = reader.ReadValue<string>()!;
                var bytes = reader.ReadValue<byte[]>()!;

                var property = TrackerProperties.FirstOrDefault(p => p.Name == name);
                if (property == null)
                    throw new InvalidOperationException($"Cannot find tracker property '{name}' on {typeof(TContainerTracker)}.");

                property.SetValue(tracker, MemoryPackValueSerializer.Deserialize(property.PropertyType, bytes));
            }
            value = tracker;
        }
    }
}
