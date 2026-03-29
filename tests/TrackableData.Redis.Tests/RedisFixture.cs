using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using TrackableData.TestUtils;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class RedisFixture : IAsyncLifetime
    {
        private SshTunnel _tunnel;

        public ConnectionMultiplexer Connection { get; private set; }
        public IDatabase Db { get; private set; }
        public string KeyPrefix { get; private set; }

        public async Task InitializeAsync()
        {
            _tunnel = await SshTunnel.GetOrCreateAsync(6379);

            var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = "localhost:6379";

            Connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            Db = Connection.GetDatabase();
            KeyPrefix = "test:" + Guid.NewGuid().ToString("N").Substring(0, 8) + ":";
        }

        public RedisKey Key(string name)
        {
            return new RedisKey(KeyPrefix + name);
        }

        public async Task DisposeAsync()
        {
            // Clean up test keys
            if (Connection != null && !string.IsNullOrEmpty(KeyPrefix))
            {
                try
                {
                    var server = Connection.GetServer(Connection.GetEndPoints()[0]);
                    foreach (var key in server.Keys(pattern: KeyPrefix + "*"))
                    {
                        await Db.KeyDeleteAsync(key);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                Connection.Dispose();
            }

            _tunnel?.Dispose();
        }
    }
}
