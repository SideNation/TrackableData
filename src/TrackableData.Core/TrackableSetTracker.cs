using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TrackableData
{
    public enum TrackableSetOperation : byte
    {
        None = 0,
        Add = 1,
        Remove = 2,
    }

    public class TrackableSetTracker<T> : ISetTracker<T>
        where T : notnull
    {
        public Dictionary<T, TrackableSetOperation> ChangeMap = new Dictionary<T, TrackableSetOperation>();

        private readonly ILogger _logger;

        public TrackableSetTracker() : this(NullLogger.Instance) { }

        public TrackableSetTracker(ILogger logger)
        {
            _logger = logger;
        }

        public bool GetChange(T value, out TrackableSetOperation operation) =>
            ChangeMap.TryGetValue(value, out operation);

        public void TrackAdd(T value)
        {
            GetChange(value, out var prevOperation);

            switch (prevOperation)
            {
                case TrackableSetOperation.None:
                    SetChange(value, TrackableSetOperation.Add);
                    break;
                case TrackableSetOperation.Add:
                    throw new InvalidOperationException("Add after add is impossible.");
                case TrackableSetOperation.Remove:
                    ChangeMap.Remove(value);
                    break;
            }
        }

        public void TrackRemove(T value)
        {
            GetChange(value, out var prevOperation);

            switch (prevOperation)
            {
                case TrackableSetOperation.None:
                    SetChange(value, TrackableSetOperation.Remove);
                    break;
                case TrackableSetOperation.Add:
                    ChangeMap.Remove(value);
                    break;
                case TrackableSetOperation.Remove:
                    throw new InvalidOperationException("Remove after remove is impossible.");
            }
        }

        private void SetChange(T value, TrackableSetOperation operation)
        {
            var hasChangedBefore = HasChange;
            ChangeMap[value] = operation;

            _logger.LogDebug("TrackableSetTracker<{Type}> {Operation}: Value={Value}",
                typeof(T).Name, operation, value);

            if (HasChangeSet != null && !hasChangedBefore)
                HasChangeSet(this);
        }

        public IEnumerable<T> AddValues =>
            ChangeMap.Where(i => i.Value == TrackableSetOperation.Add).Select(i => i.Key);

        public IEnumerable<T> RemoveValues =>
            ChangeMap.Where(i => i.Value == TrackableSetOperation.Remove).Select(i => i.Key);

        // ITracker

        public bool HasChange => ChangeMap.Count > 0;

        public event TrackerHasChangeSet? HasChangeSet;

        public void Clear() => ChangeMap.Clear();

        public void ApplyTo(object trackable) => ApplyTo((ICollection<T>)trackable);

        public void ApplyTo(ICollection<T> trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeMap)
            {
                switch (item.Value)
                {
                    case TrackableSetOperation.Add:
                        trackable.Add(item.Key);
                        break;
                    case TrackableSetOperation.Remove:
                        trackable.Remove(item.Key);
                        break;
                }
            }
        }

        public void ApplyTo(ITracker tracker) => ApplyTo((TrackableSetTracker<T>)tracker);
        public void ApplyTo(ITracker<ICollection<T>> tracker) => ApplyTo((TrackableSetTracker<T>)tracker);

        public void ApplyTo(TrackableSetTracker<T> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            foreach (var item in ChangeMap)
            {
                switch (item.Value)
                {
                    case TrackableSetOperation.Add:
                        tracker.TrackAdd(item.Key);
                        break;
                    case TrackableSetOperation.Remove:
                        tracker.TrackRemove(item.Key);
                        break;
                }
            }
        }

        public void RollbackTo(object trackable) => RollbackTo((ICollection<T>)trackable);

        public void RollbackTo(ICollection<T> trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeMap)
            {
                switch (item.Value)
                {
                    case TrackableSetOperation.Add:
                        trackable.Remove(item.Key);
                        break;
                    case TrackableSetOperation.Remove:
                        trackable.Add(item.Key);
                        break;
                }
            }
        }

        public void RollbackTo(ITracker tracker) => RollbackTo((TrackableSetTracker<T>)tracker);
        public void RollbackTo(ITracker<ICollection<T>> tracker) => RollbackTo((TrackableSetTracker<T>)tracker);

        public void RollbackTo(TrackableSetTracker<T> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            foreach (var item in ChangeMap)
            {
                switch (item.Value)
                {
                    case TrackableSetOperation.Add:
                        tracker.TrackRemove(item.Key);
                        break;
                    case TrackableSetOperation.Remove:
                        tracker.TrackAdd(item.Key);
                        break;
                }
            }
        }

        public override string ToString()
        {
            return "{ " + string.Join(", ", ChangeMap.Select(x => x.Value switch
            {
                TrackableSetOperation.Add => $"+{x.Key}",
                TrackableSetOperation.Remove => $"-{x.Key}",
                _ => ""
            })) + " }";
        }
    }
}
