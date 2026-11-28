using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;
using DanToPhong.Protocol;

namespace UI.Services
{
    public sealed class NetworkClientService : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _udpPort;
        private readonly bool _useTls;
        private readonly string _certificateThumbprintSha256;
        private readonly SemaphoreSlim _ioLock = new(1, 1);
        private readonly CancellationTokenSource _heartbeatCts = new();
        private readonly System.Threading.Timer _heartbeatTimer;
        private readonly object _heartbeatGate = new();
        private Task _heartbeatTask = Task.CompletedTask;
        private readonly object _disposeGate = new();
        private readonly TaskCompletionSource<bool> _requestsDrained =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeRequests;

        private TcpClient? _client;
        private Stream? _stream;
        private string? _lastConnectionError;
        private volatile bool _disposed;

        // MỚI: launcher truyền cho game Unity
        public string Host => _host;
        public int Port => _port;
        public int UdpPort => _udpPort;
        public bool UseTls => _useTls;
        public string CertificateThumbprintSha256 => _certificateThumbprintSha256;
        public string Token { get; private set; } = string.Empty;

        public event Action<bool, string>? ConnectionStatusChanged;

        public bool IsConnected
        {
            get
            {
                try
                {
                    return _client?.Connected == true && _stream != null &&
                           (!_client.Client.Poll(1, SelectMode.SelectRead) || _client.Client.Available > 0);
                }
                catch { return false; }
            }
        }

        public NetworkClientService(string? host = null, int? port = null)
        {
            string? fileHost = null;
            int? filePort = null;
            int? fileUdpPort = null;
            bool? fileUseTls = null;
            string? fileCertificateThumbprintSha256 = null;
            string configPath = Path.Combine(AppContext.BaseDirectory, "server.json");
            if (File.Exists(configPath))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                ServerConfig? config = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(configPath), options);
                fileHost = config?.Host;
                filePort = config?.Port;
                fileUdpPort = config?.UdpPort;
                fileUseTls = config?.UseTls;
                fileCertificateThumbprintSha256 = config?.CertificateThumbprintSha256;
            }

            string? envHost = Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_HOST");
            _host = (host ?? (string.IsNullOrWhiteSpace(fileHost) ? envHost : fileHost) ?? string.Empty).Trim();
            string? envPort = Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_PORT");
            if (port.HasValue)
            {
                _port = port.Value;
            }
            else if (filePort.HasValue)
            {
                _port = filePort.Value;
            }
            else if (!string.IsNullOrWhiteSpace(envPort))
            {
                if (!int.TryParse(envPort, out int parsedPort) || parsedPort is < 1 or > 65535)
                    throw new InvalidOperationException("DANTOPHONG_SERVER_PORT must be an integer between 1 and 65535.");
                _port = parsedPort;
            }
            else
            {
                _port = filePort ?? 9000;
            }
            string? envUdpPort = Environment.GetEnvironmentVariable("DANTOPHONG_UDP_PORT");
            if (fileUdpPort.HasValue)
                _udpPort = fileUdpPort.Value;
            else if (string.IsNullOrWhiteSpace(envUdpPort))
                _udpPort = 9000;
            else if (!int.TryParse(envUdpPort, out int parsedUdpPort) || parsedUdpPort is < 1 or > 65535)
                throw new InvalidOperationException("DANTOPHONG_UDP_PORT must be an integer between 1 and 65535.");
            else
                _udpPort = parsedUdpPort;
            string? envTls = Environment.GetEnvironmentVariable("DANTOPHONG_USE_TLS");
            if (fileUseTls.HasValue)
                _useTls = fileUseTls.Value;
            else if (string.IsNullOrWhiteSpace(envTls))
                _useTls = true;
            else if (bool.TryParse(envTls, out bool parsedTls))
                _useTls = parsedTls;
            else
                throw new InvalidOperationException("DANTOPHONG_USE_TLS must be 'true' or 'false'.");

            _certificateThumbprintSha256 = _useTls
                ? CertificatePinValidator.NormalizeSha256Thumbprint(fileCertificateThumbprintSha256)
                : string.Empty;
            if (_useTls && string.IsNullOrEmpty(_certificateThumbprintSha256))
                throw new InvalidOperationException("TLS requires certificateThumbprintSha256 in server.json.");

            if (string.IsNullOrWhiteSpace(_host) || _port is < 1 or > 65535)
                throw new InvalidOperationException("Thiếu cấu hình DANTOPHONG_SERVER_HOST/PORT hoặc file server.json.");

            _heartbeatTimer = new System.Threading.Timer(OnHeartbeatTimer, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        }

        private sealed class ServerConfig
        {
            public string? Host { get; set; }
            public int? Port { get; set; }
            public int? UdpPort { get; set; }
            public bool? UseTls { get; set; }
            public string? CertificateThumbprintSha256 { get; set; }
        }

        private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(12);

        private static void ConfigureTcpKeepAlive(TcpClient client)
        {
            try
            {
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Trace.TraceWarning($"TCP keep-alive unavailable: {ex.SocketErrorCode}");
            }
            catch (PlatformNotSupportedException)
            {
                System.Diagnostics.Trace.TraceWarning("TCP keep-alive is not supported on this platform.");
            }
        }

        private void OnHeartbeatTimer(object? state)
        {
            if (_disposed || string.IsNullOrEmpty(Token) || !IsConnected) return;
            lock (_heartbeatGate)
            {
                if (_disposed || !_heartbeatTask.IsCompleted) return;
                _heartbeatTask = SendHeartbeatAsync(_heartbeatCts.Token);
            }
        }

        private async Task SendHeartbeatAsync(CancellationToken token)
        {
            try
            {
                ResponsePacket response = await SendAndReceiveAsync(new RequestPacket { Command = Commands.Ping }, token);
                if (!response.Success || response.Message != "PONG")
                    System.Diagnostics.Trace.TraceWarning("Server heartbeat was not acknowledged.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Server heartbeat failed: {ex.GetType().Name}");
            }
        }

        public async Task<bool> ConnectAsync(CancellationToken token = default)
        {
            if (_disposed) return false;
            if (IsConnected) return true;

            try
            {
                DisconnectInternal(raiseEvent: false);

                var client = new TcpClient();
                ConfigureTcpKeepAlive(client);
                _client = client;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(NetworkTimeout);
                await client.ConnectAsync(_host, _port, timeout.Token);

                Stream stream = client.GetStream();
                _stream = stream;
                if (_useTls)
                {
                    var ssl = new SslStream(stream, leaveInnerStreamOpen: false,
                        (_, certificate, chain, errors) => CertificatePinValidator.Validate(
                            certificate, chain, errors, _certificateThumbprintSha256));
                    _stream = ssl;
                    await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = _host,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    }, timeout.Token);
                    stream = ssl;
                }
                _stream = stream;
                _lastConnectionError = null;
                if (_disposed)
                {
                    DisconnectInternal(raiseEvent: false);
                    return false;
                }

                ConnectionStatusChanged?.Invoke(true, _useTls ? "Connected (TLS)" : "Connected");
                return true;
            }
            catch (Exception ex)
            {
                DisconnectInternal(raiseEvent: false);
                _lastConnectionError = DescribeConnectionFailure(ex);
                ConnectionStatusChanged?.Invoke(false, $"Offline ({_lastConnectionError})");
                return false;
            }
        }

        private static string DescribeConnectionFailure(Exception exception)
        {
            if (exception is AuthenticationException)
                return $"TLS handshake or certificate validation failed: {exception.Message}";
            if (exception is OperationCanceledException)
                return "Connection timed out or was cancelled.";
            if (exception is SocketException socketException)
                return $"Socket error {socketException.SocketErrorCode}: {socketException.Message}";
            if (exception is IOException { InnerException: SocketException innerSocketException })
                return $"Socket error {innerSocketException.SocketErrorCode}: {innerSocketException.Message}";
            return $"{exception.GetType().Name}: {exception.Message}";
        }

        private async Task<ResponsePacket> SendAndReceiveAsync(RequestPacket request, CancellationToken token = default)
        {
            if (!TryStartRequest())
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.ServerError,
                    Message = "Client is disposed."
                };
            }

            bool lockAcquired = false;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(NetworkTimeout);
                await _ioLock.WaitAsync(timeout.Token);
                lockAcquired = true;

                if (!IsConnected)
                {
                    bool connected = await ConnectAsync(timeout.Token);
                    if (!connected || _stream == null)
                    {
                        return new ResponsePacket
                        {
                            Success = false,
                            Code = ErrorCodes.ServerError,
                            Message = $"Cannot connect to server: {_lastConnectionError ?? "connection failed; check the host, port and TLS settings."}"
                        };
                    }
                }

                await MessageFramer.SendAsync(_stream!, request, timeout.Token);
                ResponsePacket? response = await MessageFramer.ReceiveAsync<ResponsePacket>(_stream!, timeout.Token);

                if (response == null)
                {
                    DisconnectInternal(raiseEvent: true);
                    return new ResponsePacket
                    {
                        Success = false,
                        Code = ErrorCodes.ServerError,
                        Message = "Server closed the connection."
                    };
                }

                return response;
            }
            catch (Exception ex)
            {
                DisconnectInternal(raiseEvent: true);
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.ServerError,
                    Message = $"Network error: {ex.Message}"
                };
            }
            finally
            {
                if (lockAcquired)
                    _ioLock.Release();
                EndRequest();
            }
        }

        private bool TryStartRequest()
        {
            lock (_disposeGate)
            {
                if (_disposed) return false;
                _activeRequests++;
                return true;
            }
        }

        private void EndRequest()
        {
            bool signalDrained;
            lock (_disposeGate)
            {
                _activeRequests--;
                signalDrained = _disposed && _activeRequests == 0;
            }

            if (signalDrained)
                _requestsDrained.TrySetResult(true);
        }

        public async Task<ResponsePacket> LoginAsync(string username, string password, CancellationToken token = default)
        {
            Token = string.Empty;
            ResponsePacket resp = await SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.Login,
                Username = username,
                Password = password
            }, token);

            if (resp.Success && !string.IsNullOrEmpty(resp.Token))
                Token = resp.Token;

            return resp;
        }

        public Task<ResponsePacket> RegisterAsync(
            string username,
            string password,
            string fullName,
            string? email,
            DateTime? birthday,
            string? gender = null,
            string? racerId = null,
            string verificationCode = "",
            CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.Register,
                Username = username,
                Password = password,
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Birthday = birthday,
                Gender = string.IsNullOrWhiteSpace(gender) ? null : gender,
                RacerId = string.IsNullOrWhiteSpace(racerId) ? null : racerId,
                VerificationCode = verificationCode
            }, token);
        }

        public Task<ResponsePacket> GetInfoAsync(CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.GetInfo
            }, token);
        }

        public Task<ResponsePacket> UpdateCoinsAsync(int delta, CancellationToken token = default)
        {
            if (delta >= 0 || delta < -5000)
                return Task.FromResult(new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Client coin grants are disabled; rewards must be confirmed by the server."
                });

            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.UpdateCoins,
                CoinsDelta = delta,
                RequestId = Guid.NewGuid().ToString("N")
            }, token);
        }

        public Task<ResponsePacket> UploadAvatarAsync(
            byte[] data,
            string currentPassword,
            CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.UploadAvatar,
                AvatarBase64 = Convert.ToBase64String(data),
                CurrentPassword = currentPassword
            }, token);
        }

        public Task<ResponsePacket> GetAvatarAsync(CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.GetAvatar
            }, token);
        }

        public Task<ResponsePacket> SubmitReviewAsync(int stars, string? comment, CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.SubmitReview,
                Stars = stars,
                Comment = comment
            }, token);
        }

        public Task<ResponsePacket> GetReviewsAsync(CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket { Command = Commands.GetReviews }, token);
        }

        public Task<ResponsePacket> GetLeaderboardAsync(CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket { Command = Commands.Leaderboard }, token);
        }

        public Task<ResponsePacket> GetRoomsAsync(CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket { Command = Commands.ListRooms }, token);
        }

        public Task<ResponsePacket> GetInvitesAsync(CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket { Command = Commands.GetInvites }, token);
        }

        public async Task<ResponsePacket> LogoutAsync(CancellationToken token = default)
        {
            ResponsePacket resp = await SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.Logout
            }, token);

            Token = string.Empty;
            DisconnectInternal(raiseEvent: true);
            return resp;
        }

        public Task<ResponsePacket> ResetPasswordAsync(
            string username,
            string email,
            string newPassword,
            string verificationCode,
            CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.ResetPassword,
                Username = username,
                Email = email,
                Password = newPassword,
                VerificationCode = verificationCode
            }, token);
        }

        public async Task<ResponsePacket> UpdateAccountSettingsAsync(
            string currentPassword,
            string? newPassword,
            string? email,
            CancellationToken token = default)
        {
            ResponsePacket response = await SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.UpdateAccountSettings,
                CurrentPassword = currentPassword,
                NewPassword = newPassword,
                Email = email
            }, token);

            if (response.Success && !string.IsNullOrWhiteSpace(response.Token))
                Token = response.Token;

            return response;
        }

        public Task<ResponsePacket> RequestResetCodeAsync(string username, string email, CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.RequestResetCode,
                Username = username,
                Email = email
            }, token);
        }

        public Task<ResponsePacket> RequestRegistrationCodeAsync(string username, string email, CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.RequestRegistrationCode,
                Username = username,
                Email = email
            }, token);
        }

        public void Disconnect()
        {
            DisconnectInternal(raiseEvent: true);
        }

        private void DisconnectInternal(bool raiseEvent)
        {
            try { _stream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
            try { _client?.Dispose(); } catch { }

            _stream = null;
            _client = null;

            if (raiseEvent)
                ConnectionStatusChanged?.Invoke(false, "Disconnected");
        }

        public void Dispose()
        {
            lock (_disposeGate)
            {
                if (_disposed) return;
                _disposed = true;
                if (_activeRequests == 0)
                    _requestsDrained.TrySetResult(true);
            }

            _heartbeatTimer.Dispose();
            _heartbeatCts.Cancel();
            DisconnectInternal(raiseEvent: false);
            Task heartbeatTask;
            lock (_heartbeatGate) heartbeatTask = _heartbeatTask;
            _ = CompleteDisposalAsync(heartbeatTask);
        }

        private async Task CompleteDisposalAsync(Task heartbeatTask)
        {
            try
            {
                await Task.WhenAll(heartbeatTask, _requestsDrained.Task).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Network client shutdown cleanup failed: {ex.GetType().Name}");
            }
            finally
            {
                _ioLock.Dispose();
                _heartbeatCts.Dispose();
            }
        }
    }
}
