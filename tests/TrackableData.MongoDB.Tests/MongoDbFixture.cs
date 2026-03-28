using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace TrackableData.MongoDB.Tests
{
    public class MongoDbFixture : IAsyncLifetime
    {
        // SSH 터널 설정: 환경변수 MONGODB_SSH_HOST 로 지정 (예: tteogi@35.237.3.35)
        // 환경변수가 없으면 SSH 터널을 시작하지 않고 localhost:27017로 직접 연결
        private Process _sshProcess;

        public IMongoDatabase Database { get; private set; }
        public MongoClient Client { get; private set; }

        public async Task InitializeAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                var sshHost = Environment.GetEnvironmentVariable("MONGODB_SSH_HOST");
                if (!string.IsNullOrEmpty(sshHost))
                {
                    await StartSshTunnelAsync(sshHost);
                }
                connectionString = "mongodb://localhost:27017";
            }

            Client = new MongoClient(connectionString);
            Database = Client.GetDatabase("trackable_test_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        private async Task StartSshTunnelAsync(string sshHost)
        {
            if (IsPortOpen(27017))
                return;

            _sshProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ssh",
                    Arguments = $"-L 27017:localhost:27017 {sshHost} -N -o StrictHostKeyChecking=no -o ConnectTimeout=10",
                    UseShellExecute = false,
                    RedirectStandardError = true
                }
            };
            _sshProcess.Start();

            for (var i = 0; i < 50; i++)
            {
                await Task.Delay(200);
                if (IsPortOpen(27017))
                    return;
            }

            throw new Exception($"SSH tunnel to {sshHost} failed to open port 27017 within 10 seconds.");
        }

        private static bool IsPortOpen(int port)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(IPAddress.Loopback, port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task DisposeAsync()
        {
            if (Database != null)
            {
                await Client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName);
            }

            if (_sshProcess != null && !_sshProcess.HasExited)
            {
                _sshProcess.Kill();
                _sshProcess.Dispose();
            }
        }

        public IMongoCollection<BsonDocument> GetCollection(string name)
        {
            return Database.GetCollection<BsonDocument>(name);
        }
    }
}
