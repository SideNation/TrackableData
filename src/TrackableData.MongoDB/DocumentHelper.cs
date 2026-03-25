using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TrackableData.MongoDB
{
    public static class DocumentHelper
    {
        public static string ToDotPath(IEnumerable<object> keys)
        {
            return string.Join(".", keys.Select(x => x.ToString()));
        }

        public static string ToDotPathWithTrailer(IEnumerable<object> keys)
        {
            var path = string.Join(".", keys.Select(x => x.ToString()));
            return path.Length > 0 ? path + "." : path;
        }

        public static BsonValue QueryValue(BsonDocument doc, IEnumerable<object> keys)
        {
            if (doc == null)
                return null;

            BsonValue curDoc = doc;
            foreach (var key in keys)
            {
                if (!curDoc.IsBsonDocument)
                    return null;

                if (!curDoc.AsBsonDocument.TryGetValue(key.ToString(), out var subValue))
                    return null;

                curDoc = subValue;
            }

            return curDoc;
        }

        public static async Task<int> DeleteAsync(IMongoCollection<BsonDocument> collection, params object[] keyValues)
        {
            if (keyValues.Length == 0)
                throw new ArgumentException("At least 1 keyValue required.");

            if (keyValues.Length == 1)
            {
                var ret = await collection.DeleteOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]));
                return ret != null ? (int)ret.DeletedCount : 0;
            }
            else
            {
                var keyPath = ToDotPath(keyValues.Skip(1));
                var ret = await collection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", keyValues[0]),
                    Builders<BsonDocument>.Update.Unset(keyPath));
                return ret != null ? (int)ret.ModifiedCount : 0;
            }
        }
    }
}
