using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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

        public bool IsConnected => _client?.Connected == true && _stream != null;

        public NetworkClientService(string host = "127.0.0.1", int port = 9000)
        {
            _host = host;
            _port = port;
        }

        public async Task<bool> ConnectAsync(CancellationToken token = default)
        {
            if (IsConnected) return true;

            try
            {
                DisconnectInternal(raiseEvent: false);

                var client = new TcpClient();
                await client.ConnectAsync(_host, _port, token);

                _client = client;
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
            await _ioLock.WaitAsync(token);
            try
            {
                if (!IsConnected)
                {
                    bool connected = await ConnectAsync(token);
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

                await MessageFramer.SendAsync(_stream!, request, token);
                ResponsePacket? response = await MessageFramer.ReceiveAsync<ResponsePacket>(_stream!, token);

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
                RacerId = string.IsNullOrWhiteSpace(racerId) ? null : racerId
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
                CoinsDelta = delta
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
            CancellationToken token = default)
        {
            return SendAndReceiveAsync(new RequestPacket
            {
                Command = Commands.ResetPassword,
                Username = username,
                Email = email,
                Password = newPassword
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