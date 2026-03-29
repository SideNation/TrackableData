using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace TrackableData.Redis
{
    public class TrackableDictionaryRedisMapper<TKey, TValue>
    {
        private readonly ITrackableLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TrackableDictionaryRedisMapper()
            : this(NullTrackableLogger.Instance, null)
        {
        }

        public TrackableDictionaryRedisMapper(ITrackableLogger logger, JsonSerializerOptions jsonOptions = null)
        {
            _logger = logger;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions();
        }

        public async Task CreateAsync(IDatabase db, IDictionary<TKey, TValue> dictionary, RedisKey key)
        {
            _logger.LogDebug("TrackableDictionaryRedisMapper<{TKey},{TValue}>.CreateAsync: {Key}",
                typeof(TKey).Name, typeof(TValue).Name, key);

            await db.JSON().SetAsync(key, "$", dictionary, serializerOptions: _jsonOptions);
        }

        public async Task<TrackableDictionary<TKey, TValue>> LoadAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableDictionaryRedisMapper<{TKey},{TValue}>.LoadAsync: {Key}",
                typeof(TKey).Name, typeof(TValue).Name, key);

            var json = db.JSON();
            var result = await json.GetAsync(key, path: "$");
            if (result.IsNull)
                return null;

            var jsonStr = result.ToString();
            using (var doc = JsonDocument.Parse(jsonStr))
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    root = root[0];

                var dict = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(root.GetRawText(), _jsonOptions);
                var trackable = new TrackableDictionary<TKey, TValue>();
                if (dict != null)
                {
                    foreach (var kv in dict)
                        trackable.Add(kv.Key, kv.Value);
                }
                return trackable;
            }
        }

        public async Task SaveAsync(IDatabase db, TrackableDictionaryTracker<TKey, TValue> tracker, RedisKey key)
        {
            if (tracker.HasChange == false)
                return;

            _logger.LogDebug("TrackableDictionaryRedisMapper<{TKey},{TValue}>.SaveAsync: {Key}, {Count} changes",
                typeof(TKey).Name, typeof(TValue).Name, key, tracker.ChangeMap.Count);

            var json = db.JSON();
            foreach (var change in tracker.ChangeMap)
            {
                var dictKey = change.Key.ToString();
                var path = "$." + dictKey;

                switch (change.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                    case TrackableDictionaryOperation.Modify:
                        await json.SetAsync(key, path, change.Value.NewValue, serializerOptions: _jsonOptions);
                        break;

                    case TrackableDictionaryOperation.Remove:
                        await json.DelAsync(key, path);
                        break;
                }
            }
        }

        public async Task SaveAsync(IDatabase db, IDictionaryTracker<TKey, TValue> tracker, RedisKey key)
        {
            await SaveAsync(db, (TrackableDictionaryTracker<TKey, TValue>)tracker, key);
        }

        public async Task<int> DeleteAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableDictionaryRedisMapper<{TKey},{TValue}>.DeleteAsync: {Key}",
                typeof(TKey).Name, typeof(TValue).Name, key);
            return (int)await db.JSON().DelAsync(key);
        }
    }
}
