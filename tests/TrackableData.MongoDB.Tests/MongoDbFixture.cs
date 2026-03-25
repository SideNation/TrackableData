using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class MongoDbFixture : IAsyncLifetime
    {
        private readonly MongoDbContainer _container = new MongoDbBuilder()
            .WithImage("mongo:8.0.21-rc0")
            .Build();

        public IMongoDatabase Database { get; private set; }
        public MongoClient Client { get; private set; }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            Client = new MongoClient(_container.GetConnectionString());
            Database = Client.GetDatabase("trackable_test");
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public IMongoCollection<BsonDocument> GetCollection(string name)
        {
            return Database.GetCollection<BsonDocument>(name);
        }
    }
}
