using System;
using System.Collections;
using System.Collections.Generic;

namespace TrackableData
{
    public class TrackableSet<T> : ISet<T>, ITrackable<ICollection<T>>
    {
        internal readonly HashSet<T> _set;

        public ISetTracker<T>? Tracker { get; set; }

        public TrackableSet()
        {
            _set = new HashSet<T>();
        }

        public TrackableSet(TrackableSet<T> set)
        {
            _set = new HashSet<T>(set._set);
        }

        public TrackableSet(IEnumerable<T> collection)
        {
            _set = new HashSet<T>(collection);
        }

        public TrackableSet<T> Clone() => new TrackableSet<T>(this);

        // ITrackable

        public bool Changed => Tracker is { HasChange: true };

        ITracker? ITrackable.Tracker
        {
            get => Tracker;
            set => Tracker = (ISetTracker<T>?)value;
        }

        ITracker<ICollection<T>>? ITrackable<ICollection<T>>.Tracker
        {
            get => Tracker;
            set => Tracker = (ISetTracker<T>?)value;
        }

        ITrackable ITrackable.Clone() => Clone();

        public ITrackable? GetChildTrackable(object name) => null;

        public IEnumerable<KeyValuePair<object, ITrackable>> GetChildTrackables(bool changedOnly = false)
        {
            yield break;
        }

        // ISet<T>

        public bool Add(T item)
        {
            if (_set.Add(item))
            {
                Tracker?.TrackAdd(item);
                return true;
            }
            return false;
        }

        public void UnionWith(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other, nameof(other));
            foreach (var item in other)
                Add(item);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other, nameof(other));
            if (_set.Count == 0 || other == (object)this)
                return;

            var removes = new HashSet<T>(this);
            removes.ExceptWith(other);
            foreach (var item in removes)
                Remove(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other, nameof(other));
            if (_set.Count == 0)
                return;

            if (other == (object)this)
            {
                Clear();
                return;
            }

            foreach (var element in other)
                Remove(element);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other, nameof(other));

            if (_set.Count == 0)
            {
                UnionWith(other);
                return;
            }

            if (other == (object)this)
            {
                Clear();
                return;
            }

            foreach (var item in other)
            {
                if (_set.Contains(item))
                    Remove(item);
                else
                    Add(item);
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);
        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        // ICollection<T>

        void ICollection<T>.Add(T item)
        {
            if (_set.Add(item))
                Tracker?.TrackAdd(item);
        }

        public void Clear()
        {
            if (Tracker != null)
            {
                foreach (var i in _set)
                    Tracker.TrackRemove(i);
            }
            _set.Clear();
        }

        public bool Contains(T item) => _set.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)_set).CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            if (_set.Remove(item))
            {
                Tracker?.TrackRemove(item);
                return true;
            }
            return false;
        }

        public int Count => _set.Count;
        public bool IsReadOnly => false;

        // IEnumerable

        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_set).GetEnumerator();
    }
}
