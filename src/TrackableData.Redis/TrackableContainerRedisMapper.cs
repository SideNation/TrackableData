using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace TrackableData.Redis
{
    public class TrackableContainerRedisMapper<T>
        where T : ITrackableContainer<T>
    {
        private readonly ITrackableLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Type _trackableType;

        private class PropertyItem
        {
            public string Name;
            public string KeySuffix;
            public PropertyInfo PropertyInfo;
            public PropertyInfo TrackerPropertyInfo;
            public object Mapper;
            public Func<IDatabase, T, RedisKey, Task> CreateAsync;
            public Func<IDatabase, RedisKey, Task<int>> DeleteAsync;
            public Func<IDatabase, T, RedisKey, Task<bool>> LoadAndSetAsync;
            public Func<IDatabase, IContainerTracker<T>, RedisKey, Task> SaveAsync;
        }

        private readonly PropertyItem[] _items;

        public TrackableContainerRedisMapper()
            : this(NullTrackableLogger.Instance, null)
        {
        }

        public TrackableContainerRedisMapper(ITrackableLogger logger, JsonSerializerOptions jsonOptions = null)
        {
            _logger = logger;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions();

            _trackableType = TrackableResolver.GetContainerTrackerType(typeof(T));
            if (_trackableType == null)
                throw new ArgumentException($"Cannot find tracker type of '{typeof(T).Name}'");

            _items = ConstructPropertyItems();
        }

        private PropertyItem[] ConstructPropertyItems()
        {
            var trackerType = TrackerResolver.GetDefaultTracker(typeof(T));
            var items = new List<PropertyItem>();

            foreach (var property in typeof(T).GetProperties())
            {
                var keySuffix = ":" + property.Name;

                var attr = property.GetCustomAttribute<TrackablePropertyAttribute>();
                if (attr != null)
                {
                    if (attr["redis.ignore"] != null)
                        continue;
                    keySuffix = ":" + (attr["redis.keysuffix:"] ?? property.Name);
                }

                var item = new PropertyItem
                {
                    Name = property.Name,
                    KeySuffix = keySuffix,
                    PropertyInfo = property,
                    TrackerPropertyInfo = trackerType.GetProperty(property.Name + "Tracker")
                };

                if (item.TrackerPropertyInfo == null)
                    throw new ArgumentException($"Cannot find tracker for '{property.Name}'");

                if (TrackableResolver.IsTrackablePoco(property.PropertyType))
                {
                    typeof(TrackableContainerRedisMapper<T>)
                        .GetMethod("BuildPocoProperty", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(TrackableResolver.GetPocoType(property.PropertyType))
                        .Invoke(this, new object[] { item });
                }
                else if (TrackableResolver.IsTrackableDictionary(property.PropertyType))
                {
                    typeof(TrackableContainerRedisMapper<T>)
                        .GetMethod("BuildDictionaryProperty", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(property.PropertyType.GetGenericArguments())
                        .Invoke(this, new object[] { item });
                }
                else if (TrackableResolver.IsTrackableList(property.PropertyType))
                {
                    typeof(TrackableContainerRedisMapper<T>)
                        .GetMethod("BuildListProperty", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(property.PropertyType.GetGenericArguments())
                        .Invoke(this, new object[] { item });
                }
                else if (TrackableResolver.IsTrackableSet(property.PropertyType))
                {
                    typeof(TrackableContainerRedisMapper<T>)
                        .GetMethod("BuildSetProperty", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(property.PropertyType.GetGenericArguments())
                        .Invoke(this, new object[] { item });
                }
                else
                {
                    throw new InvalidOperationException("Cannot resolve property: " + property.Name);
                }

                items.Add(item);
            }
            return items.ToArray();
        }

        private void BuildPocoProperty<TPoco>(PropertyItem item) where TPoco : ITrackablePoco<TPoco>
        {
            var mapper = new TrackablePocoRedisMapper<TPoco>(_logger, _jsonOptions);
            item.Mapper = mapper;
            item.CreateAsync = async (db, container, key) =>
            {
                var value = (TPoco)item.PropertyInfo.GetValue(container);
                if (value != null)
                    await mapper.CreateAsync(db, value, key.Append(item.KeySuffix));
            };
            item.DeleteAsync = (db, key) => mapper.DeleteAsync(db, key.Append(item.KeySuffix));
            item.LoadAndSetAsync = async (db, container, key) =>
            {
                var value = await mapper.LoadAsync(db, key.Append(item.KeySuffix));
                item.PropertyInfo.SetValue(container, value);
                return value != null;
            };
            item.SaveAsync = async (db, tracker, key) =>
            {
                var t = (TrackablePocoTracker<TPoco>)item.TrackerPropertyInfo.GetValue(tracker);
                if (t != null && t.HasChange)
                    await mapper.SaveAsync(db, t, key.Append(item.KeySuffix));
            };
        }

        private void BuildDictionaryProperty<TKey, TValue>(PropertyItem item)
        {
            var mapper = new TrackableDictionaryRedisMapper<TKey, TValue>(_logger, _jsonOptions);
            item.Mapper = mapper;
            item.CreateAsync = async (db, container, key) =>
            {
                var value = (IDictionary<TKey, TValue>)item.PropertyInfo.GetValue(container);
                if (value != null)
                    await mapper.CreateAsync(db, value, key.Append(item.KeySuffix));
            };
            item.DeleteAsync = (db, key) => mapper.DeleteAsync(db, key.Append(item.KeySuffix));
            item.LoadAndSetAsync = async (db, container, key) =>
            {
                var value = await mapper.LoadAsync(db, key.Append(item.KeySuffix));
                item.PropertyInfo.SetValue(container, value ?? (object)new TrackableDictionary<TKey, TValue>());
                return true;
            };
            item.SaveAsync = async (db, tracker, key) =>
            {
                var t = (TrackableDictionaryTracker<TKey, TValue>)item.TrackerPropertyInfo.GetValue(tracker);
                if (t != null && t.HasChange)
                    await mapper.SaveAsync(db, t, key.Append(item.KeySuffix));
            };
        }

        private void BuildListProperty<TValue>(PropertyItem item)
        {
            var mapper = new TrackableListRedisMapper<TValue>(_logger, _jsonOptions);
            item.Mapper = mapper;
            item.CreateAsync = async (db, container, key) =>
            {
                var value = (IList<TValue>)item.PropertyInfo.GetValue(container);
                if (value != null)
                    await mapper.CreateAsync(db, value, key.Append(item.KeySuffix));
            };
            item.DeleteAsync = (db, key) => mapper.DeleteAsync(db, key.Append(item.KeySuffix));
            item.LoadAndSetAsync = async (db, container, key) =>
            {
                var value = await mapper.LoadAsync(db, key.Append(item.KeySuffix));
                item.PropertyInfo.SetValue(container, value ?? (object)new TrackableList<TValue>());
                return true;
            };
            item.SaveAsync = async (db, tracker, key) =>
            {
                var t = (TrackableListTracker<TValue>)item.TrackerPropertyInfo.GetValue(tracker);
                if (t != null && t.HasChange)
                    await mapper.SaveAsync(db, t, key.Append(item.KeySuffix));
            };
        }

        private void BuildSetProperty<TValue>(PropertyItem item)
        {
            var mapper = new TrackableSetRedisMapper<TValue>(_logger, _jsonOptions);
            item.Mapper = mapper;
            item.CreateAsync = async (db, container, key) =>
            {
                var value = (ICollection<TValue>)item.PropertyInfo.GetValue(container);
                if (value != null)
                    await mapper.CreateAsync(db, value, key.Append(item.KeySuffix));
            };
            item.DeleteAsync = (db, key) => mapper.DeleteAsync(db, key.Append(item.KeySuffix));
            item.LoadAndSetAsync = async (db, container, key) =>
            {
                var value = await mapper.LoadAsync(db, key.Append(item.KeySuffix));
                item.PropertyInfo.SetValue(container, value ?? (object)new TrackableSet<TValue>());
                return true;
            };
            item.SaveAsync = async (db, tracker, key) =>
            {
                var t = (TrackableSetTracker<TValue>)item.TrackerPropertyInfo.GetValue(tracker);
                if (t != null && t.HasChange)
                    await mapper.SaveAsync(db, t, key.Append(item.KeySuffix));
            };
        }

        public async Task CreateAsync(IDatabase db, T container, RedisKey key)
        {
            _logger.LogDebug("TrackableContainerRedisMapper<{Type}>.CreateAsync: {Key}", typeof(T).Name, key);
            foreach (var item in _items)
                await item.CreateAsync(db, container, key);
        }

        public async Task<int> DeleteAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableContainerRedisMapper<{Type}>.DeleteAsync: {Key}", typeof(T).Name, key);
            var count = 0;
            foreach (var item in _items)
                count += await item.DeleteAsync(db, key);
            return count;
        }

        public async Task<T> LoadAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableContainerRedisMapper<{Type}>.LoadAsync: {Key}", typeof(T).Name, key);
            var container = (T)Activator.CreateInstance(_trackableType);
            foreach (var item in _items)
            {
                await item.LoadAndSetAsync(db, container, key);
            }
            return container;
        }

        public async Task SaveAsync(IDatabase db, IContainerTracker<T> tracker, RedisKey key)
        {
            _logger.LogDebug("TrackableContainerRedisMapper<{Type}>.SaveAsync: {Key}", typeof(T).Name, key);
            foreach (var item in _items)
                await item.SaveAsync(db, tracker, key);
        }

        public async Task SaveAsync(IDatabase db, ITracker tracker, RedisKey key)
        {
            await SaveAsync(db, (IContainerTracker<T>)tracker, key);
        }
    }

    internal static class RedisKeyExtensions
    {
        public static RedisKey Append(this RedisKey key, string suffix)
        {
            return new RedisKey(key.ToString() + suffix);
        }
    }
}
