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
    public class TrackablePocoRedisMapper<T>
        where T : ITrackablePoco<T>
    {
        private readonly ITrackableLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Type _trackableType;

        private class PropertyItem
        {
            public string Name;
            public string JsonPath;
            public PropertyInfo PropertyInfo;
        }

        private readonly PropertyItem[] _properties;

        public TrackablePocoRedisMapper()
            : this(NullTrackableLogger.Instance, null)
        {
        }

        public TrackablePocoRedisMapper(ITrackableLogger logger, JsonSerializerOptions jsonOptions = null)
        {
            _logger = logger;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions();

            _trackableType = TrackableResolver.GetPocoTrackerType(typeof(T));
            if (_trackableType == null)
                throw new ArgumentException($"Cannot find trackable type for '{typeof(T).Name}'");

            var properties = new List<PropertyItem>();
            foreach (var pi in typeof(T).GetProperties())
            {
                var fieldName = pi.Name;
                var attr = pi.GetCustomAttribute<TrackablePropertyAttribute>();
                if (attr != null)
                {
                    if (attr["redis.ignore"] != null)
                        continue;
                    fieldName = attr["redis.field:"] ?? fieldName;
                }

                properties.Add(new PropertyItem
                {
                    Name = fieldName,
                    JsonPath = "$." + fieldName,
                    PropertyInfo = pi,
                });
            }
            _properties = properties.ToArray();
        }

        public async Task CreateAsync(IDatabase db, T value, RedisKey key)
        {
            _logger.LogDebug("TrackablePocoRedisMapper<{Type}>.CreateAsync: {Key}", typeof(T).Name, key);

            var json = db.JSON();
            var dict = new Dictionary<string, object>();
            foreach (var prop in _properties)
            {
                var val = prop.PropertyInfo.GetValue(value);
                if (val != null)
                    dict[prop.Name] = val;
            }
            await json.SetAsync(key, "$", dict, serializerOptions: _jsonOptions);
        }

        public async Task<T> LoadAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackablePocoRedisMapper<{Type}>.LoadAsync: {Key}", typeof(T).Name, key);

            var json = db.JSON();
            var result = await json.GetAsync(key, path: "$");
            if (result.IsNull)
                return default(T);

            var poco = (T)Activator.CreateInstance(_trackableType);
            var jsonStr = result.ToString();

            // JSON.GET with $ path returns an array wrapper: [{...}]
            using (var doc = JsonDocument.Parse(jsonStr))
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    root = root[0];

                foreach (var prop in _properties)
                {
                    if (root.TryGetProperty(prop.Name, out var element) &&
                        element.ValueKind != JsonValueKind.Null)
                    {
                        var val = JsonSerializer.Deserialize(element.GetRawText(), prop.PropertyInfo.PropertyType, _jsonOptions);
                        prop.PropertyInfo.SetValue(poco, val);
                    }
                }
            }
            return poco;
        }

        public async Task SaveAsync(IDatabase db, TrackablePocoTracker<T> tracker, RedisKey key)
        {
            if (tracker.HasChange == false)
                return;

            _logger.LogDebug("TrackablePocoRedisMapper<{Type}>.SaveAsync: {Key}, {Count} changes",
                typeof(T).Name, key, tracker.ChangeMap.Count);

            var json = db.JSON();
            foreach (var change in tracker.ChangeMap)
            {
                PropertyItem prop = null;
                foreach (var p in _properties)
                {
                    if (p.PropertyInfo == change.Key)
                    {
                        prop = p;
                        break;
                    }
                }
                if (prop == null)
                    continue;

                var newValue = change.Value.NewValue;
                if (newValue != null)
                {
                    await json.SetAsync(key, prop.JsonPath, newValue, serializerOptions: _jsonOptions);
                }
                else
                {
                    await json.SetAsync(key, prop.JsonPath, "null");
                }
            }
        }

        public async Task SaveAsync(IDatabase db, IPocoTracker<T> tracker, RedisKey key)
        {
            await SaveAsync(db, (TrackablePocoTracker<T>)tracker, key);
        }

        public async Task<int> DeleteAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackablePocoRedisMapper<{Type}>.DeleteAsync: {Key}", typeof(T).Name, key);
            return (int)await db.JSON().DelAsync(key);
        }
    }
}
