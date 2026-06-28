using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public class TrackableContainerMongoDbMapper<T>
        where T : ITrackableContainer<T>
    {
        private readonly ITrackableLogger _logger;
        private readonly Type _trackableType;

        private class PropertyItem
        {
            public string Name;
            public PropertyInfo PropertyInfo;
            public PropertyInfo TrackerPropertyInfo;
            public object Mapper;
            public Action<T, BsonDocument> ExportToBson;
            public Action<BsonDocument, T> ImportFromBson;
            public Func<IContainerTracker<T>, object[], List<UpdateDefinition<BsonDocument>>> BuildSaveUpdates;
        }

        private readonly PropertyItem[] _items;

        public TrackableContainerMongoDbMapper()
            : this(NullTrackableLogger.Instance)
        {
        }

        public TrackableContainerMongoDbMapper(ITrackableLogger logger)
        {
            _logger = logger;

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
                var attr = property.GetCustomAttribute<TrackablePropertyAttribute>();
                if (attr != null && attr["mongodb.ignore"] != null)
                    continue;

                var item = new PropertyItem
                {
                    Name = property.Name,
                    PropertyInfo = property,
                    TrackerPropertyInfo = trackerType.GetProperty(property.Name + "Tracker")
                };

                if (item.TrackerPropertyInfo == null)
                    throw new ArgumentException($"Cannot find tracker for '{property.Name}'");

                if (TrackableResolver.IsTrackablePoco(property.PropertyType))
                    InvokeBuild("BuildPocoProperty", new[] { TrackableResolver.GetPocoType(property.PropertyType) }, item);
                else if (TrackableResolver.IsTrackableDictionary(property.PropertyType))
                    InvokeBuild("BuildDictionaryProperty", property.PropertyType.GetGenericArguments(), item);
                else if (TrackableResolver.IsTrackableList(property.PropertyType))
                    InvokeBuild("BuildListProperty", property.PropertyType.GetGenericArguments(), item);
                else if (TrackableResolver.IsTrackableSet(property.PropertyType))
                    InvokeBuild("BuildSetProperty", property.PropertyType.GetGenericArguments(), item);
                else
                    throw new InvalidOperationException("Cannot resolve property: " + property.Name);

                items.Add(item);
            }

            return items.ToArray();
        }

        private void InvokeBuild(string methodName, Type[] typeArgs, PropertyItem item)
        {
            typeof(TrackableContainerMongoDbMapper<T>)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(typeArgs)
                .Invoke(this, new object[] { item });
        }

        private void BuildPocoProperty<TPoco>(PropertyItem item)
            where TPoco : ITrackablePoco<TPoco>
        {
            var mapper = new TrackablePocoMongoDbMapper<TPoco>(_logger);
            item.Mapper = mapper;

            item.ExportToBson = (container, doc) =>
            {
                var value = (TPoco)item.PropertyInfo.GetValue(container);
                if (value != null)
                    doc.Add(item.Name, mapper.ConvertToBsonDocument(value));
            };
            item.ImportFromBson = (doc, container) =>
            {
                var sub = DocumentHelper.QueryValue(doc, new object[] { item.Name });
                var value = sub != null && sub.IsBsonDocument
                    ? mapper.ConvertToTrackablePoco(sub.AsBsonDocument)
                    : default(TPoco);
                item.PropertyInfo.SetValue(container, value);
            };
            item.BuildSaveUpdates = (tracker, keyValues) =>
            {
                var valueTracker = (TrackablePocoTracker<TPoco>)item.TrackerPropertyInfo.GetValue(tracker);
                if (valueTracker == null || !valueTracker.HasChange)
                    return null;

                var update = mapper.BuildUpdatesForSave(
                    null, valueTracker, keyValues.Concat(new object[] { item.Name }).ToArray());
                return update != null
                    ? new List<UpdateDefinition<BsonDocument>> { update }
                    : null;
            };
        }

        private void BuildDictionaryProperty<TKey, TValue>(PropertyItem item)
        {
            var mapper = new TrackableDictionaryMongoDbMapper<TKey, TValue>(_logger);
            item.Mapper = mapper;

            item.ExportToBson = (container, doc) =>
            {
                var value = (IDictionary<TKey, TValue>)item.PropertyInfo.GetValue(container);
                if (value != null)
                    doc.Add(item.Name, mapper.ConvertToBsonDocument(value));
            };
            item.ImportFromBson = (doc, container) =>
            {
                var sub = DocumentHelper.QueryValue(doc, new object[] { item.Name });
                var value = sub != null && sub.IsBsonDocument
                    ? mapper.ConvertToTrackableDictionary(sub.AsBsonDocument)
                    : new TrackableDictionary<TKey, TValue>();
                item.PropertyInfo.SetValue(container, value);
            };
            item.BuildSaveUpdates = (tracker, keyValues) =>
            {
                var valueTracker = (TrackableDictionaryTracker<TKey, TValue>)item.TrackerPropertyInfo.GetValue(tracker);
                if (valueTracker == null || !valueTracker.HasChange)
                    return null;

                var update = mapper.BuildUpdatesForSave(
                    null, valueTracker, keyValues.Concat(new object[] { item.Name }).ToArray());
                return update != null
                    ? new List<UpdateDefinition<BsonDocument>> { update }
                    : null;
            };
        }

        private void BuildListProperty<TValue>(PropertyItem item)
        {
            var mapper = new TrackableListMongoDbMapper<TValue>(_logger);
            item.Mapper = mapper;

            item.ExportToBson = (container, doc) =>
            {
                var value = (IList<TValue>)item.PropertyInfo.GetValue(container);
                if (value != null)
                    doc.Add(item.Name, mapper.ConvertToBsonArray(value));
            };
            item.ImportFromBson = (doc, container) =>
            {
                var sub = DocumentHelper.QueryValue(doc, new object[] { item.Name });
                var value = sub != null && sub.IsBsonArray
                    ? mapper.ConvertToTrackableList(sub.AsBsonArray)
                    : new TrackableList<TValue>();
                item.PropertyInfo.SetValue(container, value);
            };
            item.BuildSaveUpdates = (tracker, keyValues) =>
            {
                var valueTracker = (TrackableListTracker<TValue>)item.TrackerPropertyInfo.GetValue(tracker);
                if (valueTracker == null || !valueTracker.HasChange)
                    return null;

                return mapper.BuildUpdatesForSave(
                    valueTracker, keyValues.Concat(new object[] { item.Name }).ToArray());
            };
        }

        private void BuildSetProperty<TValue>(PropertyItem item)
            where TValue : notnull
        {
            var mapper = new TrackableSetMongoDbMapper<TValue>(_logger);
            item.Mapper = mapper;

            item.ExportToBson = (container, doc) =>
            {
                var value = (ICollection<TValue>)item.PropertyInfo.GetValue(container);
                if (value != null)
                    doc.Add(item.Name, mapper.ConvertToBsonArray(value));
            };
            item.ImportFromBson = (doc, container) =>
            {
                var sub = DocumentHelper.QueryValue(doc, new object[] { item.Name });
                var value = sub != null && sub.IsBsonArray
                    ? mapper.ConvertToTrackableSet(sub.AsBsonArray)
                    : new TrackableSet<TValue>();
                item.PropertyInfo.SetValue(container, value);
            };
            item.BuildSaveUpdates = (tracker, keyValues) =>
            {
                var valueTracker = (TrackableSetTracker<TValue>)item.TrackerPropertyInfo.GetValue(tracker);
                if (valueTracker == null || !valueTracker.HasChange)
                    return null;

                return mapper.BuildUpdatesForSave(
                    valueTracker, keyValues.Concat(new object[] { item.Name }).ToArray());
            };
        }

        public BsonDocument ConvertToBsonDocument(T container)
        {
            var bson = new BsonDocument();
            foreach (var item in _items)
                item.ExportToBson(container, bson);
            return bson;
        }

        public T ConvertToTrackableContainer(BsonDocument doc)
        {
            var container = (T)Activator.CreateInstance(_trackableType);
            foreach (var item in _items)
                item.ImportFromBson(doc, container);
            return container;
        }

        public async Task CreateAsync(IMongoCollection<BsonDocument> collection, T container, params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            _logger.LogDebug("TrackableContainerMongoDbMapper<{Type}>.CreateAsync", typeof(T).Name);

            var bson = ConvertToBsonDocument(container);
            if (keyValues.Length == 1)
            {
                bson.InsertAt(0, new BsonElement("_id", BsonValue.Create(keyValues[0])));
                await collection.InsertOneAsync(bson);
            }
            else
            {
                var setPath = DocumentHelper.ToDotPath(keyValues.Skip(1));
                await collection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                        Builders<BsonDocument>.Filter.Exists(setPath, false)),
                    Builders<BsonDocument>.Update.Set(setPath, bson),
                    new UpdateOptions { IsUpsert = true });
            }
        }

        public Task<int> DeleteAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            _logger.LogDebug("TrackableContainerMongoDbMapper<{Type}>.DeleteAsync", typeof(T).Name);
            return DocumentHelper.DeleteAsync(collection, keyValues);
        }

        public async Task<T> LoadAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            _logger.LogDebug("TrackableContainerMongoDbMapper<{Type}>.LoadAsync", typeof(T).Name);

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

            return doc != null ? ConvertToTrackableContainer(doc) : default(T);
        }

        public Task SaveAsync(IMongoCollection<BsonDocument> collection, ITracker tracker, params object[] keyValues)
        {
            return SaveAsync(collection, (IContainerTracker<T>)tracker, keyValues);
        }

        public async Task SaveAsync(IMongoCollection<BsonDocument> collection,
                                    IContainerTracker<T> tracker,
                                    params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            _logger.LogDebug("TrackableContainerMongoDbMapper<{Type}>.SaveAsync", typeof(T).Name);

            var filter = Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]);
            var partialKeys = keyValues.Skip(1).ToArray();

            foreach (var item in _items)
            {
                var updates = item.BuildSaveUpdates(tracker, partialKeys);
                if (updates == null)
                    continue;

                foreach (var update in updates)
                    await collection.UpdateOneAsync(filter, update);
            }
        }
    }
}
