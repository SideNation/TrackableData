using System;
using System.Collections;
using System.Collections.Generic;

namespace TrackableData
{
    public class TrackableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ITrackable<IDictionary<TKey, TValue>>
        where TKey : notnull
    {
        internal readonly Dictionary<TKey, TValue> _dictionary;

        public IDictionaryTracker<TKey, TValue>? Tracker { get; set; }

        public TrackableDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public TrackableDictionary(TrackableDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary._dictionary);
        }

        public TrackableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public TrackableDictionary<TKey, TValue> Clone()
        {
            return new TrackableDictionary<TKey, TValue>(this);
        }

        public bool Update(TKey key, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (!_dictionary.TryGetValue(key, out var value))
                return false;

            var newValue = updateValueFactory(key, value);
            _dictionary[key] = newValue;
            Tracker?.TrackModify(key, value, newValue);
            return true;
        }

        public TValue AddOrUpdate(TKey key,
                                  Func<TKey, TValue> addValueFactory,
                                  Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (!_dictionary.TryGetValue(key, out var value))
            {
                var addValue = addValueFactory(key);
                Add(key, addValue);
                return addValue;
            }

            var newValue = updateValueFactory(key, value);
            _dictionary[key] = newValue;
            Tracker?.TrackModify(key, value, newValue);
            return newValue;
        }

        // ITrackable

        public bool Changed => Tracker is { HasChange: true };

        ITracker? ITrackable.Tracker
        {
            get => Tracker;
            set => Tracker = (IDictionaryTracker<TKey, TValue>?)value;
        }

        ITracker<IDictionary<TKey, TValue>>? ITrackable<IDictionary<TKey, TValue>>.Tracker
        {
            get => Tracker;
            set => Tracker = (IDictionaryTracker<TKey, TValue>?)value;
        }

        ITrackable ITrackable.Clone() => Clone();

        public ITrackable? GetChildTrackable(object name)
        {
            var key = name is TKey k ? k : (TKey)Convert.ChangeType(name, typeof(TKey));
            return _dictionary.TryGetValue(key, out var value) ? value as ITrackable : null;
        }

        public IEnumerable<KeyValuePair<object, ITrackable>> GetChildTrackables(bool changedOnly = false)
        {
            if (!typeof(ITrackable).IsAssignableFrom(typeof(TValue)))
                yield break;

            foreach (var item in _dictionary)
            {
                var trackable = (ITrackable)item.Value!;
                if (!changedOnly || trackable.Changed)
                    yield return new KeyValuePair<object, ITrackable>(item.Key, trackable);
            }
        }

        // IDictionary<TKey, TValue>

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            Tracker?.TrackAdd(key, value);
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.TryGetValue(key, out var value))
            {
                _dictionary.Remove(key);
                Tracker?.TrackRemove(key, value);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value) =>
            _dictionary.TryGetValue(key, out value!);

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                if (_dictionary.TryGetValue(key, out var oldValue))
                {
                    _dictionary[key] = value;
                    Tracker?.TrackModify(key, oldValue, value);
                }
                else
                {
                    _dictionary.Add(key, value);
                    Tracker?.TrackAdd(key, value);
                }
            }
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        // ICollection<KeyValuePair<TKey, TValue>>

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Add(item);
            Tracker?.TrackAdd(item.Key, item.Value);
        }

        public void Clear()
        {
            if (Tracker != null)
            {
                foreach (var i in _dictionary)
                    Tracker.TrackRemove(i.Key, i.Value);
            }
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item))
            {
                Tracker?.TrackRemove(item.Key, item.Value);
                return true;
            }
            return false;
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        // IEnumerable

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();
    }
}
