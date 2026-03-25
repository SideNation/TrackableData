using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public class TrackablePocoMongoDbMapper<T>
        where T : ITrackablePoco<T>
    {
        private readonly Type _trackableType;
        private readonly Dictionary<PropertyInfo, BsonMemberMap> _propertyToMemberMap;
        private readonly ILogger _logger;

        public TrackablePocoMongoDbMapper() : this(NullLogger.Instance) { }

        public TrackablePocoMongoDbMapper(ILogger logger)
        {
            _logger = logger;
            _trackableType = TrackableResolver.GetPocoTrackerType(typeof(T));
            TypeMapper.RegisterTrackablePocoMap(_trackableType);

            _propertyToMemberMap = new Dictionary<PropertyInfo, BsonMemberMap>();

            var classMap = BsonClassMap.LookupClassMap(_trackableType);
            foreach (var property in typeof(T).GetProperties())
            {
                var member = classMap.AllMemberMaps.FirstOrDefault(m => m.MemberInfo.Name == property.Name);
                if (member != null)
                    _propertyToMemberMap[property] = member;
            }
        }

        public BsonDocument ConvertToBsonDocument(T poco)
        {
            var bsonDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(bsonDocument))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                var serializer = BsonSerializer.LookupSerializer(_trackableType);
                serializer.Serialize(context, poco);
            }
            return bsonDocument;
        }

        public T ConvertToTrackablePoco(BsonDocument doc)
        {
            return (T)BsonSerializer.Deserialize(doc, _trackableType);
        }

        public UpdateDefinition<BsonDocument> BuildUpdatesForSave(
            UpdateDefinition<BsonDocument> update, TrackablePocoTracker<T> tracker, params object[] keyValues)
        {
            var keyNamespace = DocumentHelper.ToDotPathWithTrailer(keyValues);

            var setDocument = new BsonDocument();
            using (var setBsonWriter = new BsonDocumentWriter(setDocument))
            {
                var setContext = BsonSerializationContext.CreateRoot(setBsonWriter);
                setBsonWriter.WriteStartDocument();

                foreach (var change in tracker.ChangeMap)
                {
                    if (change.Value.NewValue != null)
                    {
                        if (!_propertyToMemberMap.TryGetValue(change.Key, out var memberMap))
                            continue;

                        setBsonWriter.WriteName(memberMap.ElementName);
                        memberMap.GetSerializer().Serialize(setContext, change.Value.NewValue);
                    }
                    else
                    {
                        update = update == null
                            ? Builders<BsonDocument>.Update.Unset(keyNamespace + change.Key.Name)
                            : update.Unset(keyNamespace + change.Key.Name);
                    }
                }

                setBsonWriter.WriteEndDocument();
            }

            foreach (var element in setDocument.Elements)
            {
                update = update == null
                    ? Builders<BsonDocument>.Update.Set(keyNamespace + element.Name, element.Value)
                    : update.Set(keyNamespace + element.Name, element.Value);
            }

            return update;
        }

        public async Task CreateAsync(IMongoCollection<BsonDocument> collection, T value, params object[] keyValues)
        {
            _logger.LogDebug("TrackablePocoMongoDbMapper<{Type}>.CreateAsync", typeof(T).Name);

            if (keyValues.Length == 0)
            {
                var bson = value.ToBsonDocument(_trackableType);
                await collection.InsertOneAsync(bson);
            }
            else if (keyValues.Length == 1)
            {
                var bson = ConvertToBsonDocument(value);
                bson["_id"] = BsonValue.Create(keyValues[0]);
                await collection.InsertOneAsync(bson);
            }
            else
            {
                var setPath = DocumentHelper.ToDotPath(keyValues.Skip(1));
                var bson = ConvertToBsonDocument(value);
                await collection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                    Builders<BsonDocument>.Update.Set(setPath, bson),
                    new UpdateOptions { IsUpsert = true });
            }
        }

        public Task<int> DeleteAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            _logger.LogDebug("TrackablePocoMongoDbMapper<{Type}>.DeleteAsync", typeof(T).Name);
            return DocumentHelper.DeleteAsync(collection, keyValues);
        }

        public async Task<T> LoadAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            _logger.LogDebug("TrackablePocoMongoDbMapper<{Type}>.LoadAsync", typeof(T).Name);

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
                return default;

            return (T)BsonSerializer.Deserialize(doc, _trackableType);
        }

        public Task<UpdateResult> SaveAsync(IMongoCollection<BsonDocument> collection,
                                            IPocoTracker<T> tracker,
                                            params object[] keyValues)
        {
            return SaveAsync(collection, (TrackablePocoTracker<T>)tracker, keyValues);
        }

        public Task<UpdateResult> SaveAsync(IMongoCollection<BsonDocument> collection,
                                            TrackablePocoTracker<T> tracker,
                                            params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            if (!tracker.HasChange)
                return Task.FromResult((UpdateResult)null);

            _logger.LogDebug("TrackablePocoMongoDbMapper<{Type}>.SaveAsync: {ChangeCount} changes",
                typeof(T).Name, tracker.ChangeMap.Count);

            return collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                BuildUpdatesForSave(null, tracker, keyValues.Skip(1).ToArray()));
        }
    }
}
