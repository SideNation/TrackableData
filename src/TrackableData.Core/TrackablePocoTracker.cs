using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TrackableData
{
    public class TrackablePocoTracker<T> : IPocoTracker<T>
    {
        public struct Change
        {
            public object? OldValue;
            public object? NewValue;
        }

        public Dictionary<PropertyInfo, Change> ChangeMap = new Dictionary<PropertyInfo, Change>();

        private readonly ITrackableLogger _logger;

        public TrackablePocoTracker() : this(NullTrackableLogger.Instance) { }

        public TrackablePocoTracker(ITrackableLogger logger)
        {
            _logger = logger;
        }

        public void TrackSet(PropertyInfo pi, object? oldValue, object? newValue)
        {
            var hasChangedBefore = HasChange;

            if (ChangeMap.TryGetValue(pi, out var change))
                ChangeMap[pi] = new Change { OldValue = change.OldValue, NewValue = newValue };
            else
                ChangeMap[pi] = new Change { OldValue = oldValue, NewValue = newValue };

            _logger.LogDebug("TrackablePocoTracker<{Type}> TrackSet: {Property} {OldValue} -> {NewValue}",
                typeof(T).Name, pi.Name, oldValue, newValue);

            if (HasChangeSet != null && !hasChangedBefore)
                HasChangeSet(this);
        }

        // ITracker

        public bool HasChange => ChangeMap.Count > 0;

        public event TrackerHasChangeSet? HasChangeSet;

        public void Clear() => ChangeMap.Clear();

        public void ApplyTo(object trackable) => ApplyTo((T)trackable);

        public void ApplyTo(T trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeMap)
            {
                var setter = item.Key.GetSetMethod()?.GetBaseDefinition();
                setter?.Invoke(trackable, new[] { item.Value.NewValue });
            }
        }

        public void ApplyTo(ITracker tracker) => ApplyTo((TrackablePocoTracker<T>)tracker);
        public void ApplyTo(ITracker<T> tracker) => ApplyTo((TrackablePocoTracker<T>)tracker);

        public void ApplyTo(TrackablePocoTracker<T> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            foreach (var item in ChangeMap)
                tracker.TrackSet(item.Key, item.Value.OldValue, item.Value.NewValue);
        }

        public void RollbackTo(object trackable) => RollbackTo((T)trackable);

        public void RollbackTo(T trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeMap)
            {
                var setter = item.Key.GetSetMethod()?.GetBaseDefinition();
                setter?.Invoke(trackable, new[] { item.Value.OldValue });
            }
        }

        public void RollbackTo(ITracker tracker) => RollbackTo((TrackablePocoTracker<T>)tracker);
        public void RollbackTo(ITracker<T> tracker) => RollbackTo((TrackablePocoTracker<T>)tracker);

        public void RollbackTo(TrackablePocoTracker<T> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            if (this == tracker)
            {
                ChangeMap.Clear();
            }
            else
            {
                foreach (var item in ChangeMap)
                    tracker.TrackSet(item.Key, item.Value.NewValue, item.Value.OldValue);
            }
        }

        public override string ToString()
        {
            return "{ " + string.Join(", ", ChangeMap.Select(
                x => $"{x.Key.Name}:{x.Value.OldValue}->{x.Value.NewValue}")) + " }";
        }
    }
}
