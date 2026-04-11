using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public class TrackableDictionaryMongoDbMapper<TKey, TValue>
        where TKey : notnull
    {
        private readonly ITrackableLogger _logger;

        public TrackableDictionaryMongoDbMapper() : this(NullTrackableLogger.Instance) { }

        public TrackableDictionaryMongoDbMapper(ITrackableLogger logger)
        {
            _logger = logger;
            TypeMapper.RegisterMap(typeof(TValue));
        }

        public BsonDocument ConvertToBsonDocument(IDictionary<TKey, TValue> dictionary)
        {
            var doc = new BsonDocument();
            foreach (var item in dictionary)
                doc.Add(item.Key.ToString(), BsonDocumentWrapper.Create(item.Value));
            return doc;
        }

        public TrackableDictionary<TKey, TValue> ConvertToTrackableDictionary(BsonDocument doc)
        {
            var dictionary = new TrackableDictionary<TKey, TValue>();
            foreach (var element in doc.Elements)
            {
                TKey key;
                try
                {
                    key = (TKey)Convert.ChangeType(element.Name, typeof(TKey));
                }
                catch (FormatException)
                {
                    if (element.Name == "_id")
                        continue;
                    throw;
                }

                var value = element.Value.IsBsonDocument
                    ? (TValue)BsonSerializer.Deserialize(element.Value.AsBsonDocument, typeof(TValue))
                    : (TValue)Convert.ChangeType(element.Value, typeof(TValue));
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        public UpdateDefinition<BsonDocument> BuildUpdatesForSave(
            UpdateDefinition<BsonDocument> update,
            TrackableDictionaryTracker<TKey, TValue> tracker,
            params object[] keyValues)
        {
            var keyNamespace = DocumentHelper.ToDotPathWithTrailer(keyValues);
            foreach (var change in tracker.ChangeMap)
            {
                switch (change.Value.Operation)
                {
                    case TrackableDictionaryOperation.Add:
                    case TrackableDictionaryOperation.Modify:
                        update = update == null
                            ? Builders<BsonDocument>.Update.Set(keyNamespace + change.Key, change.Value.NewValue)
                            : update.Set(keyNamespace + change.Key, change.Value.NewValue);
                        break;

                    case TrackableDictionaryOperation.Remove:
                        update = update == null
                            ? Builders<BsonDocument>.Update.Unset(keyNamespace + change.Key)
                            : update.Unset(keyNamespace + change.Key);
                        break;
                }
            }
            return update;
        }

        public async Task CreateAsync(IMongoCollection<BsonDocument> collection, IDictionary<TKey, TValue> dictionary,
                                      params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            _logger.LogDebug("TrackableDictionaryMongoDbMapper<{TKey},{TValue}>.CreateAsync",
                typeof(TKey).Name, typeof(TValue).Name);

            if (keyValues.Length == 1)
            {
                var bson = ConvertToBsonDocument(dictionary);
                bson.InsertAt(0, new BsonElement("_id", BsonValue.Create(keyValues[0])));
                await collection.InsertOneAsync(bson);
            }
            else
            {
                var subKeys = keyValues.Skip(1).ToArray();
                var update = BuildUpdatesForCreate(null, dictionary, subKeys);
                await collection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                    update,
                    new UpdateOptions { IsUpsert = true });
            }
        }

        public UpdateDefinition<BsonDocument> BuildUpdatesForCreate(
            UpdateDefinition<BsonDocument> update, IDictionary<TKey, TValue> dictionary, params object[] keyValues)
        {
            var valuePath = DocumentHelper.ToDotPath(keyValues);
            var bson = ConvertToBsonDocument(dictionary);
            return update == null
                ? Builders<BsonDocument>.Update.Set(valuePath, bson)
                : update.Set(valuePath, bson);
        }

        public Task<int> DeleteAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            return DocumentHelper.DeleteAsync(collection, keyValues);
        }

        public async Task<TrackableDictionary<TKey, TValue>> LoadAsync(
            IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            _logger.LogDebug("TrackableDictionaryMongoDbMapper<{TKey},{TValue}>.LoadAsync",
                typeof(TKey).Name, typeof(TValue).Name);

            BsonDocument doc;

            if (keyValues.Length == 1)
            {
                doc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]))
                                      .FirstOrDefaultAsync();
            }
            else
            {
                var partialKeys = keyValues.Skip(1);
                var partialPath = DocumentHelper.ToDotPath(partialKeys);
                var partialDoc = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]))
                    .Project(Builders<BsonDocument>.Projection.Include(partialPath))
                    .FirstOrDefaultAsync();
                doc = DocumentHelper.QueryValue(partialDoc, partialKeys) as BsonDocument;
            }

            if (doc == null)
                return null;

            return ConvertToTrackableDictionary(doc);
        }

        public Task<UpdateResult> SaveAsync(IMongoCollection<BsonDocument> collection,
                                            TrackableDictionaryTracker<TKey, TValue> tracker,
                                            params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            if (!tracker.HasChange)
                return Task.FromResult((UpdateResult)null);

            _logger.LogDebug("TrackableDictionaryMongoDbMapper<{TKey},{TValue}>.SaveAsync: {Count} changes",
                typeof(TKey).Name, typeof(TValue).Name, tracker.ChangeMap.Count);

            return collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                BuildUpdatesForSave(null, tracker, keyValues.Skip(1).ToArray()),
                new UpdateOptions { IsUpsert = true });
        }
    }
}
