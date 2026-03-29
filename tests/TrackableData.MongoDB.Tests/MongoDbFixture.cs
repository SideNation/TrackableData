using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using TrackableData.TestUtils;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class MongoDbFixture : IAsyncLifetime
    {
        private SshTunnel _tunnel;

        public IMongoDatabase Database { get; private set; }
        public MongoClient Client { get; private set; }

        public async Task InitializeAsync()
        {
            _tunnel = await SshTunnel.GetOrCreateAsync(27017);

            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = "mongodb://localhost:27017";

            Client = new MongoClient(connectionString);
            Database = Client.GetDatabase("trackable_test_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        public async Task DisposeAsync()
        {
            if (Database != null)
            {
                await Client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName);
            }

            _tunnel?.Dispose();
        }

        public IMongoCollection<BsonDocument> GetCollection(string name)
        {
            return Database.GetCollection<BsonDocument>(name);
        }
    }
}
