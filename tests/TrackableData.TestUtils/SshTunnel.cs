using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TrackableData.TestUtils
{
    public class SshTunnel : IDisposable
    {
        private static readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);
        private static SshTunnel _instance;
        private static int _refCount;

        private readonly Process _process;
        private readonly int[] _ports;

        private SshTunnel(Process process, int[] ports)
        {
            _process = process;
            _ports = ports;
        }

        /// <summary>
        /// SSH 터널을 시작하거나 기존 터널을 재사용합니다.
        /// 환경변수 TEST_SSH_HOST (예: tteogi@35.237.3.35) 로 SSH 호스트를 지정합니다.
        /// </summary>
        /// <param name="ports">포워딩할 포트 목록 (예: 27017, 5432)</param>
        public static async Task<SshTunnel> GetOrCreateAsync(params int[] ports)
        {
            var sshHost = Environment.GetEnvironmentVariable("TEST_SSH_HOST");
            if (string.IsNullOrEmpty(sshHost))
                return null;

            await Lock.WaitAsync();
            try
            {
                if (_instance != null && !_instance._process.HasExited)
                {
                    _refCount++;
                    return _instance;
                }

                // 모든 포트가 이미 열려있으면 터널 불필요
                if (AllPortsOpen(ports))
                {
                    _refCount++;
                    _instance = new SshTunnel(null, ports);
                    return _instance;
                }

                var args = BuildSshArguments(sshHost, ports);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ssh",
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardError = true
                    }
                };
                process.Start();

                for (var i = 0; i < 50; i++)
                {
                    await Task.Delay(200);
                    if (AllPortsOpen(ports))
                    {
                        _refCount++;
                        _instance = new SshTunnel(process, ports);
                        return _instance;
                    }
                }

                process.Kill();
                process.Dispose();
                throw new Exception(
                    $"SSH tunnel to {sshHost} failed to open ports [{string.Join(", ", ports)}] within 10 seconds.");
            }
            finally
            {
                Lock.Release();
            }
        }

        private static string BuildSshArguments(string sshHost, int[] ports)
        {
            var portForwards = "";
            foreach (var port in ports)
            {
                portForwards += $"-L {port}:localhost:{port} ";
            }
            return $"{portForwards}{sshHost} -N -o StrictHostKeyChecking=no -o ConnectTimeout=10";
        }

        private static bool AllPortsOpen(int[] ports)
        {
            foreach (var port in ports)
            {
                if (!IsPortOpen(port))
                    return false;
            }
            return true;
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

        public void Dispose()
        {
            Lock.Wait();
            try
            {
                _refCount--;
                if (_refCount <= 0 && _process != null && !_process.HasExited)
                {
                    _process.Kill();
                    _process.Dispose();
                    _instance = null;
                    _refCount = 0;
                }
            }
            finally
            {
                Lock.Release();
            }
        }
    }
}
