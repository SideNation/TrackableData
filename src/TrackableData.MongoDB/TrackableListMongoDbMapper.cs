using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public class TrackableListMongoDbMapper<T>
    {
        private readonly ITrackableLogger _logger;

        public TrackableListMongoDbMapper() : this(NullTrackableLogger.Instance) { }

        public TrackableListMongoDbMapper(ITrackableLogger logger)
        {
            _logger = logger;
        }

        public BsonArray ConvertToBsonArray(IList<T> list)
        {
            return new BsonArray(list.Select(v => BsonValueMapper.ToBsonValue(v)));
        }

        public TrackableList<T> ConvertToTrackableList(BsonArray bson)
        {
            var list = new TrackableList<T>();
            foreach (var item in bson)
                list.Add(BsonValueMapper.ToValue<T>(item));
            return list;
        }

        public List<UpdateDefinition<BsonDocument>> BuildUpdatesForSave(
            TrackableListTracker<T> tracker, params object[] keyValues)
        {
            var valuePath = DocumentHelper.ToDotPath(keyValues);
            var updates = new List<UpdateDefinition<BsonDocument>>();

            foreach (var change in tracker.ChangeList)
            {
                switch (change.Operation)
                {
                    case TrackableListOperation.Insert:
                        updates.Add(Builders<BsonDocument>.Update.PushEach(valuePath,
                            new[] { BsonValueMapper.ToBsonValue(change.NewValue) },
                            position: change.Index));
                        break;

                    case TrackableListOperation.Modify:
                        updates.Add(Builders<BsonDocument>.Update.Set(valuePath + "." + change.Index,
                            BsonValueMapper.ToBsonValue(change.NewValue)));
                        break;

                    case TrackableListOperation.PushFront:
                        updates.Add(Builders<BsonDocument>.Update.PushEach(valuePath,
                            new[] { BsonValueMapper.ToBsonValue(change.NewValue) },
                            position: 0));
                        break;

                    case TrackableListOperation.PushBack:
                        updates.Add(Builders<BsonDocument>.Update.Push(valuePath,
                            BsonValueMapper.ToBsonValue(change.NewValue)));
                        break;

                    case TrackableListOperation.PopFront:
                        updates.Add(Builders<BsonDocument>.Update.PopFirst(valuePath));
                        break;

                    case TrackableListOperation.PopBack:
                        updates.Add(Builders<BsonDocument>.Update.PopLast(valuePath));
                        break;

                    case TrackableListOperation.Remove:
                        throw new NotSupportedException("Remove at arbitrary index is not supported in MongoDB list.");
                }
            }

            return updates;
        }

        public async Task CreateAsync(IMongoCollection<BsonDocument> collection, IList<T> list,
            params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            _logger.LogDebug("TrackableListMongoDbMapper<{Type}>.CreateAsync", typeof(T).Name);

            var valuePath = DocumentHelper.ToDotPath(keyValues.Skip(1));
            var bsonArray = ConvertToBsonArray(list);
            await collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                Builders<BsonDocument>.Update.Set(valuePath, bsonArray),
                new UpdateOptions { IsUpsert = true });
        }

        public Task<int> DeleteAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            return DocumentHelper.DeleteAsync(collection, keyValues);
        }

        public async Task<TrackableList<T>> LoadAsync(IMongoCollection<BsonDocument> collection,
            params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            _logger.LogDebug("TrackableListMongoDbMapper<{Type}>.LoadAsync", typeof(T).Name);

            var partialKeys = keyValues.Skip(1);
            var partialPath = DocumentHelper.ToDotPath(partialKeys);
            var doc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]))
                .Project(Builders<BsonDocument>.Projection.Include(partialPath))
                .FirstOrDefaultAsync();

            var value = DocumentHelper.QueryValue(doc, partialKeys);
            if (value == null || !value.IsBsonArray)
                return null;

            return ConvertToTrackableList(value.AsBsonArray);
        }

        public async Task SaveAsync(IMongoCollection<BsonDocument> collection,
                                    TrackableListTracker<T> tracker,
                                    params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            if (!tracker.HasChange)
                return;

            _logger.LogDebug("TrackableListMongoDbMapper<{Type}>.SaveAsync: {Count} changes",
                typeof(T).Name, tracker.ChangeList.Count);

            var filter = Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]);
            foreach (var update in BuildUpdatesForSave(tracker, keyValues.Skip(1).ToArray()))
            {
                await collection.UpdateOneAsync(filter, update);
            }
        }
    }
}
