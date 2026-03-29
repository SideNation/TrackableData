using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace TrackableData.Redis
{
    public class TrackableListRedisMapper<T>
    {
        private readonly ITrackableLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TrackableListRedisMapper()
            : this(NullTrackableLogger.Instance, null)
        {
        }

        public TrackableListRedisMapper(ITrackableLogger logger, JsonSerializerOptions jsonOptions = null)
        {
            _logger = logger;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions();
        }

        public async Task CreateAsync(IDatabase db, IList<T> list, RedisKey key)
        {
            _logger.LogDebug("TrackableListRedisMapper<{Type}>.CreateAsync: {Key}", typeof(T).Name, key);
            await db.JSON().SetAsync(key, "$", list, serializerOptions: _jsonOptions);
        }

        public async Task<TrackableList<T>> LoadAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableListRedisMapper<{Type}>.LoadAsync: {Key}", typeof(T).Name, key);

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

                var items = JsonSerializer.Deserialize<List<T>>(root.GetRawText(), _jsonOptions);
                var trackable = new TrackableList<T>();
                if (items != null)
                {
                    foreach (var item in items)
                        trackable.Add(item);
                }
                return trackable;
            }
        }

        public async Task SaveAsync(IDatabase db, TrackableListTracker<T> tracker, RedisKey key)
        {
            if (tracker.HasChange == false)
                return;

            _logger.LogDebug("TrackableListRedisMapper<{Type}>.SaveAsync: {Key}, {Count} changes",
                typeof(T).Name, key, tracker.ChangeList.Count);

            var json = db.JSON();
            foreach (var change in tracker.ChangeList)
            {
                switch (change.Operation)
                {
                    case TrackableListOperation.Insert:
                        await json.ArrInsertAsync(key, "$", change.Index, change.NewValue);
                        break;

                    case TrackableListOperation.Remove:
                        // Remove by index: pop at index, then trim if needed
                        // JSON.ARRPOP supports index-based removal
                        await json.ArrPopAsync(key, "$", change.Index);
                        break;

                    case TrackableListOperation.Modify:
                        await json.SetAsync(key, "$[" + change.Index + "]",
                            change.NewValue, serializerOptions: _jsonOptions);
                        break;

                    case TrackableListOperation.PushFront:
                        await json.ArrInsertAsync(key, "$", 0, change.NewValue);
                        break;

                    case TrackableListOperation.PushBack:
                        await json.ArrAppendAsync(key, "$", change.NewValue);
                        break;

                    case TrackableListOperation.PopFront:
                        await json.ArrPopAsync(key, "$", 0);
                        break;

                    case TrackableListOperation.PopBack:
                        await json.ArrPopAsync(key, "$", -1);
                        break;
                }
            }
        }

        public async Task SaveAsync(IDatabase db, IListTracker<T> tracker, RedisKey key)
        {
            await SaveAsync(db, (TrackableListTracker<T>)tracker, key);
        }

        public async Task<int> DeleteAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableListRedisMapper<{Type}>.DeleteAsync: {Key}", typeof(T).Name, key);
            return (int)await db.JSON().DelAsync(key);
        }
    }
}
