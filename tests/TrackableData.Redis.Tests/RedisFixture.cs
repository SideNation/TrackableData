using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using TrackableData.TestUtils;
using Xunit;

namespace TrackableData.Redis.Tests
{
    public class RedisFixture : IAsyncLifetime
    {
        private const string ConnectionStringEnvironmentVariableName = "REDIS_CONNECTION_STRING";

        private SshTunnel _tunnel;

        public ConnectionMultiplexer Connection { get; private set; }
        public IDatabase Db { get; private set; }
        public string KeyPrefix { get; private set; }

        public async Task InitializeAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariableName);
            if (string.IsNullOrEmpty(connectionString))
                connectionString = EnvFile.GetValue(ConnectionStringEnvironmentVariableName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _tunnel = await SshTunnel.GetOrCreateAsync(6379);
                connectionString = "localhost:6379";
            }

            Connection = await ConnectAsync(connectionString);
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

        private static Task<ConnectionMultiplexer> ConnectAsync(string connectionString)
        {
            var options = CreateConfigurationOptions(connectionString);
            return options != null
                ? ConnectionMultiplexer.ConnectAsync(options)
                : ConnectionMultiplexer.ConnectAsync(connectionString);
        }

        private static ConfigurationOptions CreateConfigurationOptions(string connectionString)
        {
            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme != "redis" && uri.Scheme != "rediss")
                return null;

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = uri.Scheme == "rediss",
            };
            options.EndPoints.Add(uri.Host, uri.Port > 0 ? uri.Port : 6379);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var separatorIndex = uri.UserInfo.IndexOf(':');
                if (separatorIndex >= 0)
                {
                    options.User = Uri.UnescapeDataString(uri.UserInfo.Substring(0, separatorIndex));
                    options.Password = Uri.UnescapeDataString(uri.UserInfo.Substring(separatorIndex + 1));
                }
                else
                {
                    options.Password = Uri.UnescapeDataString(uri.UserInfo);
                }
            }
            return options;
        }
    }
}
