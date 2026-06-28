using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public class TrackableSetMongoDbMapper<T>
        where T : notnull
    {
        private readonly ITrackableLogger _logger;

        public TrackableSetMongoDbMapper() : this(NullTrackableLogger.Instance) { }

        public TrackableSetMongoDbMapper(ITrackableLogger logger)
        {
            _logger = logger;
        }

        public BsonArray ConvertToBsonArray(ICollection<T> set)
        {
            return new BsonArray(set.Select(v => BsonValueMapper.ToBsonValue(v)));
        }

        public TrackableSet<T> ConvertToTrackableSet(BsonArray bson)
        {
            var set = new TrackableSet<T>();
            foreach (var item in bson)
                set.Add(BsonValueMapper.ToValue<T>(item));
            return set;
        }

        public List<UpdateDefinition<BsonDocument>> BuildUpdatesForSave(
            TrackableSetTracker<T> tracker, params object[] keyValues)
        {
            var valuePath = DocumentHelper.ToDotPath(keyValues);
            var updates = new List<UpdateDefinition<BsonDocument>>();

            var addValues = tracker.AddValues.ToList();
            if (addValues.Count > 0)
            {
                updates.Add(Builders<BsonDocument>.Update.AddToSetEach(valuePath,
                    addValues.Select(v => BsonValueMapper.ToBsonValue(v))));
            }

            var removeValues = tracker.RemoveValues.ToList();
            if (removeValues.Count > 0)
            {
                updates.Add(Builders<BsonDocument>.Update.PullAll(valuePath,
                    removeValues.Select(v => BsonValueMapper.ToBsonValue(v))));
            }

            return updates;
        }

        public async Task CreateAsync(IMongoCollection<BsonDocument> collection, ICollection<T> set,
                                      params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            _logger.LogDebug("TrackableSetMongoDbMapper<{Type}>.CreateAsync", typeof(T).Name);

            var valuePath = DocumentHelper.ToDotPath(keyValues.Skip(1));
            var bsonArray = ConvertToBsonArray(set);
            await collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                Builders<BsonDocument>.Update.Set(valuePath, bsonArray),
                new UpdateOptions { IsUpsert = true });
        }

        public Task<int> DeleteAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            return DocumentHelper.DeleteAsync(collection, keyValues);
        }

        public async Task<TrackableSet<T>> LoadAsync(IMongoCollection<BsonDocument> collection,
                                                     params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            _logger.LogDebug("TrackableSetMongoDbMapper<{Type}>.LoadAsync", typeof(T).Name);

            var partialKeys = keyValues.Skip(1);
            var partialPath = DocumentHelper.ToDotPath(partialKeys);
            var doc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]))
                .Project(Builders<BsonDocument>.Projection.Include(partialPath))
                .FirstOrDefaultAsync();

            var value = DocumentHelper.QueryValue(doc, partialKeys);
            if (value == null || !value.IsBsonArray)
                return null;

            return ConvertToTrackableSet(value.AsBsonArray);
        }

        public async Task SaveAsync(IMongoCollection<BsonDocument> collection,
                                    TrackableSetTracker<T> tracker,
                                    params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            if (!tracker.HasChange)
                return;

            _logger.LogDebug("TrackableSetMongoDbMapper<{Type}>.SaveAsync: {Count} changes",
                typeof(T).Name, tracker.ChangeMap.Count);

            var filter = Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]);
            foreach (var update in BuildUpdatesForSave(tracker, keyValues.Skip(1).ToArray()))
            {
                await collection.UpdateOneAsync(filter, update);
            }
        }
    }
}
