using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TrackableData
{
    public enum TrackableDictionaryOperation : byte
    {
        None = 0,
        Add = 1,
        Remove = 2,
        Modify = 3,
    }

    public class TrackableDictionaryTracker<TKey, TValue> : IDictionaryTracker<TKey, TValue>
        where TKey : notnull
    {
        public struct Change
        {
            public TrackableDictionaryOperation Operation;
            public TValue OldValue;
            public TValue NewValue;
        }

        public Dictionary<TKey, Change> ChangeMap = new Dictionary<TKey, Change>();

        private readonly ILogger _logger;

        public TrackableDictionaryTracker() : this(NullLogger.Instance) { }

        public TrackableDictionaryTracker(ILogger logger)
        {
            _logger = logger;
        }

        public bool GetChange(TKey key, out Change change) =>
            ChangeMap.TryGetValue(key, out change);

        public void TrackAdd(TKey key, TValue newValue)
        {
            GetChange(key, out var prevChange);

            switch (prevChange.Operation)
            {
                case TrackableDictionaryOperation.None:
                    SetChange(key, new Change
                    {
                        Operation = TrackableDictionaryOperation.Add,
                        NewValue = newValue
                    });
                    break;

                case TrackableDictionaryOperation.Add:
                    throw new InvalidOperationException("Add after add is impossible.");

                case TrackableDictionaryOperation.Modify:
                    prevChange.NewValue = newValue;
                    SetChange(key, prevChange);
                    break;

                case TrackableDictionaryOperation.Remove:
                    SetChange(key, new Change
                    {
                        Operation = TrackableDictionaryOperation.Modify,
                        OldValue = prevChange.OldValue,
                        NewValue = newValue
                    });
                    break;
            }
        }

        public void TrackRemove(TKey key, TValue oldValue)
        {
            GetChange(key, out var prevChange);

            switch (prevChange.Operation)
            {
                case TrackableDictionaryOperation.None:
                    SetChange(key, new Change
                    {
                        Operation = TrackableDictionaryOperation.Remove,
                        OldValue = oldValue
                    });
                    break;

                case TrackableDictionaryOperation.Add:
                    ChangeMap.Remove(key);
                    break;

                case TrackableDictionaryOperation.Modify:
                    SetChange(key, new Change
                    {
                        Operation = TrackableDictionaryOperation.Remove,
                        OldValue = prevChange.OldValue
                    });
                    break;

                case TrackableDictionaryOperation.Remove:
                    throw new InvalidOperationException("Remove after remove is impossible.");
            }
        }

        public void TrackModify(TKey key, TValue oldValue, TValue newValue)
        {
            GetChange(key, out var prevChange);

            switch (prevChange.Operation)
            {
                case TrackableDictionaryOperation.None:
                    SetChange(key, new Change
                    {
                        Operation = TrackableDictionaryOperation.Modify,
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                    break;

                case TrackableDictionaryOperation.Add:
                    prevChange.NewValue = newValue;
                    SetChange(key, prevChange);
                    break;

                case TrackableDictionaryOperation.Remove:
                    throw new InvalidOperationException("Modify after remove is impossible.");

                case TrackableDictionaryOperation.Modify:
                    SetChange(key, new Change
                    {
                        Operation = TrackableDictionaryOperation.Modify,
                        OldValue = prevChange.OldValue,
                        NewValue = newValue
                    });
                    break;
            }
        }

        private void SetChange(TKey key, Change change)
        {
            var hasChangedBefore = HasChange;
            ChangeMap[key] = change;

            _logger.LogDebug("TrackableDictionaryTracker<{TKey},{TValue}> {Operation}: Key={Key}",
                typeof(TKey).Name, typeof(TValue).Name, change.Operation, key);

            if (HasChangeSet != null && !hasChangedBefore)
                HasChangeSet(this);
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> AddItems =>
            ChangeMap.Where(i => i.Value.Operation == TrackableDictionaryOperation.Add)
                     .Select(i => new KeyValuePair<TKey, TValue>(i.Key, i.Value.NewValue));

        public IEnumerable<KeyValuePair<TKey, TValue>> ModifyItems =>
            ChangeMap.Where(i => i.Value.Operation == TrackableDictionaryOperation.Modify)
                     .Select(i => new KeyValuePair<TKey, TValue>(i.Key, i.Value.NewValue));

        public IEnumerable<KeyValuePair<TKey, TValue>> RemoveItems =>
            ChangeMap.Where(i => i.Value.Operation == TrackableDictionaryOperation.Remove)
                     .Select(i => new KeyValuePair<TKey, TValue>(i.Key, i.Value.OldValue));

        public IEnumerable<TKey> RemoveKeys =>
            ChangeMap.Where(i => i.Value.Operation == TrackableDictionaryOperation.Remove)
                     .Select(i => i.Key);

        // ITracker

        public bool HasChange => ChangeMap.Count > 0;

        public event TrackerHasChangeSet? HasChangeSet;

        public void Clear() => ChangeMap.Clear();

        public void ApplyTo(object trackable) => ApplyTo((IDictionary<TKey, TValue>)trackable);

        public void ApplyTo(IDictionary<TKey, TValue> trackable) => ApplyTo(trackable, false);

        public void ApplyTo(IDictionary<TKey, TValue> trackable, bool strict)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeMap)
            {
                switch (item.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                        if (strict)
                            trackable.Add(item.Key, item.Value.NewValue);
                        else
                            trackable[item.Key] = item.Value.NewValue;
                        break;
                    case TrackableDictionaryOperation.Remove:
                        trackable.Remove(item.Key);
                        break;
                    case TrackableDictionaryOperation.Modify:
                        trackable[item.Key] = item.Value.NewValue;
                        break;
                }
            }
        }

        public void ApplyTo(ITracker tracker) => ApplyTo((TrackableDictionaryTracker<TKey, TValue>)tracker);
        public void ApplyTo(ITracker<IDictionary<TKey, TValue>> tracker) => ApplyTo((TrackableDictionaryTracker<TKey, TValue>)tracker);

        public void ApplyTo(TrackableDictionaryTracker<TKey, TValue> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            foreach (var item in ChangeMap)
            {
                switch (item.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                        tracker.TrackAdd(item.Key, item.Value.NewValue);
                        break;
                    case TrackableDictionaryOperation.Remove:
                        tracker.TrackRemove(item.Key, item.Value.OldValue);
                        break;
                    case TrackableDictionaryOperation.Modify:
                        tracker.TrackModify(item.Key, item.Value.OldValue, item.Value.NewValue);
                        break;
                }
            }
        }

        public void RollbackTo(object trackable) => RollbackTo((IDictionary<TKey, TValue>)trackable);

        public void RollbackTo(IDictionary<TKey, TValue> trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeMap)
            {
                switch (item.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                        trackable.Remove(item.Key);
                        break;
                    case TrackableDictionaryOperation.Remove:
                        trackable[item.Key] = item.Value.OldValue;
                        break;
                    case TrackableDictionaryOperation.Modify:
                        trackable[item.Key] = item.Value.OldValue;
                        break;
                }
            }
        }

        public void RollbackTo(ITracker tracker) => RollbackTo((TrackableDictionaryTracker<TKey, TValue>)tracker);
        public void RollbackTo(ITracker<IDictionary<TKey, TValue>> tracker) => RollbackTo((TrackableDictionaryTracker<TKey, TValue>)tracker);

        public void RollbackTo(TrackableDictionaryTracker<TKey, TValue> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            foreach (var item in ChangeMap)
            {
                switch (item.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                        tracker.TrackRemove(item.Key, item.Value.NewValue);
                        break;
                    case TrackableDictionaryOperation.Remove:
                        tracker.TrackAdd(item.Key, item.Value.OldValue);
                        break;
                    case TrackableDictionaryOperation.Modify:
                        tracker.TrackModify(item.Key, item.Value.NewValue, item.Value.OldValue);
                        break;
                }
            }
        }

        public override string ToString()
        {
            return "{ " + string.Join(", ", ChangeMap.Select(x => x.Value.Operation switch
            {
                TrackableDictionaryOperation.Add => $"+{x.Key}:{x.Value.NewValue}",
                TrackableDictionaryOperation.Remove => $"-{x.Key}:{x.Value.OldValue}",
                TrackableDictionaryOperation.Modify => $"={x.Key}:{x.Value.OldValue}->{x.Value.NewValue}",
                _ => ""
            })) + " }";
        }
    }
}
