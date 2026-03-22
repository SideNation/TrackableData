using System;
using System.Collections;
using System.Collections.Generic;

namespace TrackableData
{
    public class TrackableList<T> : IList<T>, ITrackable<IList<T>>
    {
        internal readonly List<T> _list;

        public IListTracker<T>? Tracker { get; set; }

        public TrackableList()
        {
            _list = new List<T>();
        }

        public TrackableList(TrackableList<T> list)
        {
            _list = new List<T>(list._list);
        }

        public TrackableList(IEnumerable<T> collection)
        {
            _list = new List<T>(collection);
        }

        public TrackableList<T> Clone() => new TrackableList<T>(this);

        // ITrackable

        public bool Changed => Tracker is { HasChange: true };

        ITracker? ITrackable.Tracker
        {
            get => Tracker;
            set => Tracker = (IListTracker<T>?)value;
        }

        ITracker<IList<T>>? ITrackable<IList<T>>.Tracker
        {
            get => Tracker;
            set => Tracker = (IListTracker<T>?)value;
        }

        ITrackable ITrackable.Clone() => Clone();

        public ITrackable? GetChildTrackable(object name)
        {
            var index = name is int i ? i : (int)Convert.ChangeType(name, typeof(int));
            return index >= 0 && index < _list.Count ? _list[index] as ITrackable : null;
        }

        public IEnumerable<KeyValuePair<object, ITrackable>> GetChildTrackables(bool changedOnly = false)
        {
            if (!typeof(ITrackable).IsAssignableFrom(typeof(T)))
                yield break;

            for (var i = 0; i < _list.Count; i++)
            {
                var trackable = (ITrackable)_list[i]!;
                if (!changedOnly || trackable.Changed)
                    yield return new KeyValuePair<object, ITrackable>(i, trackable);
            }
        }

        // IList<T>

        public T this[int index]
        {
            get => _list[index];
            set
            {
                Tracker?.TrackModify(index, _list[index], value);
                _list[index] = value;
            }
        }

        public int IndexOf(T item) => _list.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (Tracker != null)
            {
                if (index == _list.Count)
                    Tracker.TrackPushBack(item);
                else if (index == 0)
                    Tracker.TrackPushFront(item);
                else
                    Tracker.TrackInsert(index, item);
            }
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (Tracker != null)
            {
                if (index == _list.Count - 1)
                    Tracker.TrackPopBack(_list[index]);
                else if (index == 0)
                    Tracker.TrackPopFront(_list[index]);
                else
                    Tracker.TrackRemove(index, _list[index]);
            }
            _list.RemoveAt(index);
        }

        // ICollection<T>

        public void Add(T item) => Insert(_list.Count, item);

        public void Clear()
        {
            if (Tracker != null)
            {
                for (var i = _list.Count - 1; i >= 0; i--)
                    Tracker.TrackPopBack(_list[i]);
            }
            _list.Clear();
        }

        public bool Contains(T item) => _list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            var index = _list.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        // IEnumerable

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();
    }
}
