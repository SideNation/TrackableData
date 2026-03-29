using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace TrackableData.Redis
{
    public class TrackableSetRedisMapper<T>
    {
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TrackableSetRedisMapper()
            : this(NullLogger.Instance, null)
        {
        }

        public TrackableSetRedisMapper(ILogger logger, JsonSerializerOptions jsonOptions = null)
        {
            _logger = logger;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions();
        }

        public async Task CreateAsync(IDatabase db, ICollection<T> set, RedisKey key)
        {
            _logger.LogDebug("TrackableSetRedisMapper<{Type}>.CreateAsync: {Key}", typeof(T).Name, key);
            var list = set.ToList();
            await db.JSON().SetAsync(key, "$", list, serializerOptions: _jsonOptions);
        }

        public async Task<TrackableSet<T>> LoadAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableSetRedisMapper<{Type}>.LoadAsync: {Key}", typeof(T).Name, key);

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
                var trackable = new TrackableSet<T>();
                if (items != null)
                {
                    foreach (var item in items)
                        trackable.Add(item);
                }
                return trackable;
            }
        }

        public async Task SaveAsync(IDatabase db, TrackableSetTracker<T> tracker, RedisKey key)
        {
            if (tracker.HasChange == false)
                return;

            _logger.LogDebug("TrackableSetRedisMapper<{Type}>.SaveAsync: {Key}, {Count} changes",
                typeof(T).Name, key, tracker.ChangeMap.Count);

            // Set doesn't have native add/remove by value in JSON arrays.
            // Load current, apply changes, save back.
            var json = db.JSON();
            var current = await LoadAsync(db, key);
            if (current == null)
                current = new TrackableSet<T>();

            foreach (var change in tracker.ChangeMap)
            {
                switch (change.Value)
                {
                    case TrackableSetOperation.Add:
                        current.Add(change.Key);
                        break;
                    case TrackableSetOperation.Remove:
                        current.Remove(change.Key);
                        break;
                }
            }

            var list = current.ToList();
            await json.SetAsync(key, "$", list, serializerOptions: _jsonOptions);
        }

        public async Task SaveAsync(IDatabase db, ISetTracker<T> tracker, RedisKey key)
        {
            await SaveAsync(db, (TrackableSetTracker<T>)tracker, key);
        }

        public async Task<int> DeleteAsync(IDatabase db, RedisKey key)
        {
            _logger.LogDebug("TrackableSetRedisMapper<{Type}>.DeleteAsync: {Key}", typeof(T).Name, key);
            return (int)await db.JSON().DelAsync(key);
        }
    }
}
