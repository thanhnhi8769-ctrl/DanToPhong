using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using DanToPhong.Protocol;

namespace UI.Services
{
    public sealed class NetworkClientService : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly SemaphoreSlim _ioLock = new(1, 1);

        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _disposed;

        // MỚI: launcher truyền cho game Unity
        public string Host => _host;
        public int Port => _port;
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
            string configPath = Path.Combine(AppContext.BaseDirectory, "server.json");
            if (File.Exists(configPath))
            {
                using JsonDocument config = JsonDocument.Parse(File.ReadAllText(configPath));
                if (config.RootElement.TryGetProperty("host", out JsonElement h)) fileHost = h.GetString();
                if (config.RootElement.TryGetProperty("port", out JsonElement p) && p.TryGetInt32(out int parsed)) filePort = parsed;
            }
            _host = host ?? Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_HOST") ?? fileHost ?? string.Empty;
            string? envPort = Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_PORT");
            _port = port ?? (int.TryParse(envPort, out int parsedPort) ? parsedPort : filePort ?? 9000);
            if (string.IsNullOrWhiteSpace(_host) || _port is < 1 or > 65535)
                throw new InvalidOperationException("Thiếu cấu hình DANTOPHONG_SERVER_HOST/PORT hoặc file server.json.");
        }

        private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(12);

        public async Task<bool> ConnectAsync(CancellationToken token = default)
        {
            if (IsConnected) return true;

            try
            {
                DisconnectInternal(raiseEvent: false);

                var client = new TcpClient();
                _client = client;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(NetworkTimeout);
                await client.ConnectAsync(_host, _port, timeout.Token);

                _stream = client.GetStream();

                ConnectionStatusChanged?.Invoke(true, "Connected");
                return true;
            }
            catch (Exception ex)
            {
                DisconnectInternal(raiseEvent: false);
                ConnectionStatusChanged?.Invoke(false, $"Offline ({ex.Message})");
                return false;
            }
        }

        private async Task<ResponsePacket> SendAndReceiveAsync(RequestPacket request, CancellationToken token = default)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(NetworkTimeout);
            await _ioLock.WaitAsync(timeout.Token);
            try
            {
                if (!IsConnected)
                {
                    bool connected = await ConnectAsync(timeout.Token);
                    if (!connected || _stream == null)
                    {
                        return new ResponsePacket
                        {
                            Success = false,
                            Code = ErrorCodes.ServerError,
                            Message = "Cannot connect to server."
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
                _ioLock.Release();
            }
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
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.UpdateCoins,
                CoinsDelta = delta,
                RequestId = Guid.NewGuid().ToString("N")
            }, token);
        }

        public Task<ResponsePacket> UploadAvatarAsync(byte[] data, CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.UploadAvatar,
                AvatarBase64 = Convert.ToBase64String(data)
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
            if (_disposed) return;
            _disposed = true;
            DisconnectInternal(raiseEvent: false);
            _ioLock.Dispose();
        }
    }
}
