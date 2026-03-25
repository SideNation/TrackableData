using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public class TrackableListMongoDbMapper<T>
    {
        private readonly ILogger _logger;

        public TrackableListMongoDbMapper() : this(NullLogger.Instance) { }

        public TrackableListMongoDbMapper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CreateAsync(IMongoCollection<BsonDocument> collection, IList<T> list,
            params object[] keyValues)
        {
            if (keyValues.Length < 2)
                throw new ArgumentException("At least 2 keyValues required.");

            _logger.LogDebug("TrackableListMongoDbMapper<{Type}>.CreateAsync", typeof(T).Name);

            var valuePath = DocumentHelper.ToDotPath(keyValues.Skip(1));
            var bsonArray = new BsonArray(list.Select(v => BsonValue.Create(v)));
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

            var list = new TrackableList<T>();
            foreach (var item in value.AsBsonArray)
            {
                list.Add((T)Convert.ChangeType(item, typeof(T)));
            }
            return list;
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

            var valuePath = DocumentHelper.ToDotPath(keyValues.Skip(1));
            var filter = Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]);

            foreach (var change in tracker.ChangeList)
            {
                switch (change.Operation)
                {
                    case TrackableListOperation.Insert:
                        await collection.UpdateOneAsync(filter,
                            Builders<BsonDocument>.Update.PushEach(valuePath,
                                new[] { BsonValue.Create(change.NewValue) },
                                position: change.Index));
                        break;

                    case TrackableListOperation.Modify:
                        await collection.UpdateOneAsync(filter,
                            Builders<BsonDocument>.Update.Set(valuePath + "." + change.Index,
                                BsonValue.Create(change.NewValue)));
                        break;

                    case TrackableListOperation.PushFront:
                        await collection.UpdateOneAsync(filter,
                            Builders<BsonDocument>.Update.PushEach(valuePath,
                                new[] { BsonValue.Create(change.NewValue) },
                                position: 0));
                        break;

                    case TrackableListOperation.PushBack:
                        await collection.UpdateOneAsync(filter,
                            Builders<BsonDocument>.Update.Push(valuePath,
                                BsonValue.Create(change.NewValue)));
                        break;

                    case TrackableListOperation.PopFront:
                        await collection.UpdateOneAsync(filter,
                            Builders<BsonDocument>.Update.PopFirst(valuePath));
                        break;

                    case TrackableListOperation.PopBack:
                        await collection.UpdateOneAsync(filter,
                            Builders<BsonDocument>.Update.PopLast(valuePath));
                        break;

                    case TrackableListOperation.Remove:
                        throw new NotSupportedException("Remove at arbitrary index is not supported in MongoDB list.");
                }
            }
        }
    }
}
