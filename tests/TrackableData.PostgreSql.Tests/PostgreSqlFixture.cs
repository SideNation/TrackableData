using System;
using System.Threading.Tasks;
using Npgsql;
using TrackableData.TestUtils;
using Xunit;

namespace TrackableData.PostgreSql.Tests
{
    public class PostgreSqlFixture : IAsyncLifetime
    {
        private SshTunnel _tunnel;

        public NpgsqlConnection Connection { get; private set; }
        public string DatabaseName { get; private set; }

        public async Task InitializeAsync()
        {
            _tunnel = await SshTunnel.GetOrCreateAsync(5432);

            var connectionString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

            // 테스트용 DB 생성
            DatabaseName = "trackable_test_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            using (var adminConn = new NpgsqlConnection(connectionString))
            {
                await adminConn.OpenAsync();
                using (var cmd = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\"", adminConn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = DatabaseName
            };
            Connection = new NpgsqlConnection(builder.ToString());
            await Connection.OpenAsync();
        }

        public async Task DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.CloseAsync();
                Connection.Dispose();
            }

            if (!string.IsNullOrEmpty(DatabaseName))
            {
                var connectionString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION_STRING");
                if (string.IsNullOrEmpty(connectionString))
                    connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

                try
                {
                    using (var adminConn = new NpgsqlConnection(connectionString))
                    {
                        await adminConn.OpenAsync();
                        using (var cmd = new NpgsqlCommand(
                            $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)", adminConn))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _tunnel?.Dispose();
        }
    }
}
