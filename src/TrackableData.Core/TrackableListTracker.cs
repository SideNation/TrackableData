using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackableData
{
    public enum TrackableListOperation : byte
    {
        None = 0,
        Insert = 1,
        Remove = 2,
        Modify = 3,
        PushFront = 4,
        PushBack = 5,
        PopFront = 6,
        PopBack = 7,
    }

    public class TrackableListTracker<T> : IListTracker<T>
    {
        public struct Change
        {
            public TrackableListOperation Operation;
            public int Index;
            public T OldValue;
            public T NewValue;
        }

        public List<Change> ChangeList = new List<Change>();

        private readonly ITrackableLogger _logger;

        public TrackableListTracker() : this(NullTrackableLogger.Instance) { }

        public TrackableListTracker(ITrackableLogger logger)
        {
            _logger = logger;
        }

        public void TrackInsert(int index, T newValue) =>
            AddChange(new Change { Operation = TrackableListOperation.Insert, Index = index, NewValue = newValue });

        public void TrackRemove(int index, T oldValue) =>
            AddChange(new Change { Operation = TrackableListOperation.Remove, Index = index, OldValue = oldValue });

        public void TrackModify(int index, T oldValue, T newValue) =>
            AddChange(new Change { Operation = TrackableListOperation.Modify, Index = index, OldValue = oldValue, NewValue = newValue });

        public void TrackPushFront(T newValue) =>
            AddChange(new Change { Operation = TrackableListOperation.PushFront, NewValue = newValue });

        public void TrackPushBack(T newValue) =>
            AddChange(new Change { Operation = TrackableListOperation.PushBack, NewValue = newValue });

        public void TrackPopFront(T oldValue) =>
            AddChange(new Change { Operation = TrackableListOperation.PopFront, OldValue = oldValue });

        public void TrackPopBack(T oldValue) =>
            AddChange(new Change { Operation = TrackableListOperation.PopBack, OldValue = oldValue });

        private void AddChange(Change change)
        {
            ChangeList.Add(change);

            _logger.LogDebug("TrackableListTracker<{Type}> {Operation}: Index={Index}",
                typeof(T).Name, change.Operation, change.Index);

            if (HasChangeSet != null && ChangeList.Count == 1)
                HasChangeSet(this);
        }

        // ITracker

        public bool HasChange => ChangeList.Count > 0;

        public event TrackerHasChangeSet? HasChangeSet;

        public void Clear() => ChangeList.Clear();

        public void ApplyTo(object trackable) => ApplyTo((IList<T>)trackable);

        public void ApplyTo(IList<T> trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            foreach (var item in ChangeList)
            {
                switch (item.Operation)
                {
                    case TrackableListOperation.Insert:
                        trackable.Insert(item.Index, item.NewValue);
                        break;
                    case TrackableListOperation.Remove:
                        trackable.RemoveAt(item.Index);
                        break;
                    case TrackableListOperation.Modify:
                        trackable[item.Index] = item.NewValue;
                        break;
                    case TrackableListOperation.PushFront:
                        trackable.Insert(0, item.NewValue);
                        break;
                    case TrackableListOperation.PushBack:
                        trackable.Insert(trackable.Count, item.NewValue);
                        break;
                    case TrackableListOperation.PopFront:
                        trackable.RemoveAt(0);
                        break;
                    case TrackableListOperation.PopBack:
                        trackable.RemoveAt(trackable.Count - 1);
                        break;
                }
            }
        }

        public void ApplyTo(ITracker tracker) => ApplyTo((TrackableListTracker<T>)tracker);
        public void ApplyTo(ITracker<IList<T>> tracker) => ApplyTo((TrackableListTracker<T>)tracker);

        public void ApplyTo(TrackableListTracker<T> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            foreach (var item in ChangeList)
            {
                switch (item.Operation)
                {
                    case TrackableListOperation.Insert:
                        tracker.TrackInsert(item.Index, item.NewValue);
                        break;
                    case TrackableListOperation.Remove:
                        tracker.TrackRemove(item.Index, item.OldValue);
                        break;
                    case TrackableListOperation.Modify:
                        tracker.TrackModify(item.Index, item.OldValue, item.NewValue);
                        break;
                    case TrackableListOperation.PushFront:
                        tracker.TrackPushFront(item.NewValue);
                        break;
                    case TrackableListOperation.PushBack:
                        tracker.TrackPushBack(item.NewValue);
                        break;
                    case TrackableListOperation.PopFront:
                        tracker.TrackPopFront(item.OldValue);
                        break;
                    case TrackableListOperation.PopBack:
                        tracker.TrackPopBack(item.OldValue);
                        break;
                }
            }
        }

        public void RollbackTo(object trackable) => RollbackTo((IList<T>)trackable);

        public void RollbackTo(IList<T> trackable)
        {
            ThrowHelper.ThrowIfNull(trackable, nameof(trackable));

            for (var i = ChangeList.Count - 1; i >= 0; i--)
            {
                var item = ChangeList[i];
                switch (item.Operation)
                {
                    case TrackableListOperation.Insert:
                        trackable.RemoveAt(item.Index);
                        break;
                    case TrackableListOperation.Remove:
                        trackable.Insert(item.Index, item.OldValue);
                        break;
                    case TrackableListOperation.Modify:
                        trackable[item.Index] = item.OldValue;
                        break;
                    case TrackableListOperation.PushFront:
                        trackable.RemoveAt(0);
                        break;
                    case TrackableListOperation.PushBack:
                        trackable.RemoveAt(trackable.Count - 1);
                        break;
                    case TrackableListOperation.PopFront:
                        trackable.Insert(0, item.OldValue);
                        break;
                    case TrackableListOperation.PopBack:
                        trackable.Insert(trackable.Count, item.OldValue);
                        break;
                }
            }
        }

        public void RollbackTo(ITracker tracker) => RollbackTo((TrackableListTracker<T>)tracker);
        public void RollbackTo(ITracker<IList<T>> tracker) => RollbackTo((TrackableListTracker<T>)tracker);

        public void RollbackTo(TrackableListTracker<T> tracker)
        {
            ThrowHelper.ThrowIfNull(tracker, nameof(tracker));

            for (var i = ChangeList.Count - 1; i >= 0; i--)
            {
                var item = ChangeList[i];
                switch (item.Operation)
                {
                    case TrackableListOperation.Insert:
                        tracker.TrackRemove(item.Index, item.NewValue);
                        break;
                    case TrackableListOperation.Remove:
                        tracker.TrackInsert(item.Index, item.OldValue);
                        break;
                    case TrackableListOperation.Modify:
                        tracker.TrackModify(item.Index, item.NewValue, item.OldValue);
                        break;
                    case TrackableListOperation.PushFront:
                        tracker.TrackPopFront(item.NewValue);
                        break;
                    case TrackableListOperation.PushBack:
                        tracker.TrackPopBack(item.NewValue);
                        break;
                    case TrackableListOperation.PopFront:
                        tracker.TrackPushFront(item.OldValue);
                        break;
                    case TrackableListOperation.PopBack:
                        tracker.TrackPushBack(item.OldValue);
                        break;
                }
            }
        }

        public override string ToString()
        {
            return "[ " + string.Join(", ", ChangeList.Select(x => x.Operation switch
            {
                TrackableListOperation.Insert => $"+{x.Index}:{x.NewValue}",
                TrackableListOperation.Remove => $"-{x.Index}:{x.OldValue}",
                TrackableListOperation.Modify => $"={x.Index}:{x.OldValue}=>{x.NewValue}",
                TrackableListOperation.PushFront => $"+F:{x.NewValue}",
                TrackableListOperation.PushBack => $"+B:{x.NewValue}",
                TrackableListOperation.PopFront => $"-F:{x.OldValue}",
                TrackableListOperation.PopBack => $"-B:{x.OldValue}",
                _ => ""
            })) + " ]";
        }
    }
}
