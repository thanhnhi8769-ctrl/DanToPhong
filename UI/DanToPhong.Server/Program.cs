using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DanToPhong.Protocol;
using Microsoft.Data.SqlClient;

namespace DanToPhong.Server
{
    internal static class Program
    {
        private static readonly int Port = ReadPort();
        private static readonly bool UseTls = ReadUseTls();
        private const int MaxCoinsDeltaPerRequest = 5000;
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("DANTOPHONG_DB_CONNECTION") ?? string.Empty;

        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex CategoryRegex = new(@"^[A-Za-z_]{1,30}$", RegexOptions.Compiled);
        private static readonly Regex RoomCodeRegex = new(@"^[A-Za-z0-9]{3,12}$", RegexOptions.Compiled);
        private static readonly Regex TargetRegex = new(@"^[A-Za-z0-9_]{1,20}$", RegexOptions.Compiled);
        private static readonly Regex MapRegex = new(@"^[A-Za-z0-9_ \-]{1,50}$", RegexOptions.Compiled);

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ClientSession> ActiveSessions = new();
        private const int MaxConcurrentClients = 128;
        private static readonly SemaphoreSlim ClientCapacity = new(MaxConcurrentClients, MaxConcurrentClients);
        private const int MaxLoginAttemptsPerWindow = 5;
        private static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LoginLockDuration = TimeSpan.FromMinutes(15);

        private static X509Certificate2 LoadServerCertificate()
        {
            string path = Environment.GetEnvironmentVariable("DANTOPHONG_CERT_PATH") ?? string.Empty;
            string password = Environment.GetEnvironmentVariable("DANTOPHONG_CERT_PASSWORD") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException("TLS is enabled but DANTOPHONG_CERT_PATH is missing or unavailable.");
            X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(
                path, password, X509KeyStorageFlags.UserKeySet);
            if (!certificate.HasPrivateKey || DateTime.UtcNow < certificate.NotBefore.ToUniversalTime() || DateTime.UtcNow >= certificate.NotAfter.ToUniversalTime())
            {
                certificate.Dispose();
                throw new InvalidOperationException("TLS certificate is expired, not yet valid, or has no private key.");
            }
            return certificate;
        }

        private static int ReadPort()
        {
            string? raw = Environment.GetEnvironmentVariable("DANTOPHONG_PORT");
            if (string.IsNullOrWhiteSpace(raw))
                return 9000;
            if (!int.TryParse(raw, out int port) || port is < 1 or > 65535)
                throw new InvalidOperationException("DANTOPHONG_PORT must be an integer between 1 and 65535.");
            return port;
        }

        private static bool ReadUseTls()
        {
            string? raw = Environment.GetEnvironmentVariable("DANTOPHONG_USE_TLS");
            if (string.IsNullOrWhiteSpace(raw))
                return true;
            if (bool.TryParse(raw.Trim(), out bool enabled))
                return enabled;
            throw new InvalidOperationException("DANTOPHONG_USE_TLS must be 'true' or 'false'.");
        }

        // Giá phía server là nguồn duy nhất; không nhận loại vật phẩm/giá do client tự đặt.
        // Giá xe theo index trong VehicleCatalog. Thêm xe mới vào catalog thì thêm giá vào đây.
        private static readonly int[] ScooterPrices =
        {
    0, 100, 150, 180, 180, 180, 200, 200, 200, 220, 220, 250, 260,
    260, 280, 280, 280, 300, 300, 320, 320, 340, 360, 380, 420
};

        private static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("Set DANTOPHONG_DB_CONNECTION before starting the server.");

            var repo = new NguoiDungRepository(ConnectionString);
            var state = new ServerStateRepository(ConnectionString);
            using X509Certificate2? serverCertificate = UseTls ? LoadServerCertificate() : null;
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await DatabaseInitializer.EnsureAsync(ConnectionString, cts.Token);   // tự tạo DB + bảng nếu chưa có
                await repo.KiemTraKetNoiAsync(cts.Token);
                await state.PruneExpiredStateAsync(cts.Token);
                Console.WriteLine("[DB] Connected to DanToPhongDB successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DB] Startup failed; refusing to listen without persistent security state. ({ex.Message})");
                throw;
            }

            IPAddress listenAddress = UseTls ? IPAddress.Any : IPAddress.Loopback;
            var listener = new TcpListener(listenAddress, Port);
            listener.Start();
            Console.WriteLine($"[TCP] Server listening on {listenAddress}:{Port}; TLS={(UseTls ? "enabled" : "disabled")}.");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                    string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

                    if (!ClientCapacity.Wait(0))
                    {
                        Console.WriteLine("[TCP] Connection rejected: client capacity reached.");
                        client.Dispose();
                        continue;
                    }
                    ConfigureTcpKeepAlive(client);

                    var session = new ClientSession
                    {
                        Client = client,
                        RemoteEndPoint = remote
                    };
                    ActiveSessions[session.SessionId] = session;

                    Console.WriteLine($"[{session.SessionId}] Connected from {remote}");
                    _ = Task.Run(() => HandleClientAsync(session, repo, state, serverCertificate, cts.Token));
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                listener.Stop();
                ClientSession[] sessions = ActiveSessions.Values.ToArray();
                foreach (ClientSession active in sessions)
                {
                    try { active.Client.Dispose(); } catch { }
                }
                await Task.WhenAll(sessions.Select(session => session.Completion.Task)).ConfigureAwait(false);
                Console.WriteLine("[TCP] Server stopped.");
            }
        }

        private static async Task HandleClientAsync(
            ClientSession session,
            NguoiDungRepository repo,
            ServerStateRepository state,
            X509Certificate2? serverCertificate,
            CancellationToken token)
        {
            Stream? stream = null;
            SslStream? tlsStream = null;

            try
            {
                stream = session.Client.GetStream();
                if (UseTls)
                {
                    tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
                    using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(10));
                    await tlsStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = serverCertificate ?? throw new InvalidOperationException("TLS certificate is unavailable."),
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificateRequired = false
                    }, handshakeTimeout.Token).ConfigureAwait(false);
                    stream = tlsStream;
                }

                while (!token.IsCancellationRequested && session.Client.Connected)
                {
                    using var idleTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    idleTimeout.CancelAfter(TimeSpan.FromSeconds(75));
                    RequestPacket? request = await MessageFramer.ReceiveAsync<RequestPacket>(stream, idleTimeout.Token).ConfigureAwait(false);
                    if (request == null)
                        break;

                    ResponsePacket response = await DispatchAsync(session, request, repo, state, token).ConfigureAwait(false);

                    await session.SendLock.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        await MessageFramer.SendAsync(stream, response, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        session.SendLock.Release();
                    }

                    if (response.Success &&
                        string.Equals(request.Command?.Trim(), Commands.GameStream, StringComparison.OrdinalIgnoreCase))
                    {
                        await StreamGameStateAsync(session, stream, token).ConfigureAwait(false);
                        break;
                    }
                }
            }

            catch (OperationCanceledException)
            {
            }
            catch (IOException ex) when (!token.IsCancellationRequested)
            {
                Console.WriteLine($"[{session.SessionId}] Connection I/O error: {ex.Message}");
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{session.SessionId}] Session error: {ex.Message}");
            }
            finally
            {
                try
                {
                    Console.WriteLine($"[{session.SessionId}] Disconnected ({session.AuthenticatedUsername ?? "guest"})");
                    if (session.AuthenticatedUserId.HasValue &&
                        !ActiveSessions.Values.Any(s => s != session && s.AuthenticatedUserId == session.AuthenticatedUserId))
                    {
                        RoomRegistry.RemoveByOwner(session.AuthenticatedUserId.Value);
                        TcpGameRoomRegistry.DisconnectUser(session.AuthenticatedUserId.Value);
                    }
                }
                finally
                {
                    try { tlsStream?.Dispose(); } catch { }
                    try { stream?.Dispose(); } catch { }
                    try { session.Client.Close(); } catch { }
                    try { session.Client.Dispose(); } catch { }
                    try { session.SendLock.Dispose(); } catch { }
                    session.Completion.TrySetResult(true);
                    ActiveSessions.TryRemove(session.SessionId, out _);
                    ClientCapacity.Release();
                }
            }
        }

        private static async Task StreamGameStateAsync(ClientSession session, Stream stream, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return;

            int userId = session.AuthenticatedUserId.Value;
            using var mailbox = new GameClientMailbox();
            using var streamCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task producer = ProduceGameStateAsync(mailbox, userId, streamCts.Token);
            Task sender = SendGameStateAsync(session, stream, mailbox, streamCts.Token);
            Task firstCompleted = await Task.WhenAny(producer, sender).ConfigureAwait(false);
            if (firstCompleted.IsFaulted || firstCompleted.IsCanceled)
                streamCts.Cancel();
            await Task.WhenAll(producer, sender).ConfigureAwait(false);
        }

        private static async Task ProduceGameStateAsync(
            GameClientMailbox mailbox,
            int userId,
            CancellationToken token)
        {
            long lastEventId = 0;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<GameEventDto> events = TcpGameRoomRegistry.ReadEventsAfter(userId, ref lastEventId);
                    foreach (GameEventDto gameEvent in events)
                    {
                        await mailbox.QueueImportantEventAsync(new ResponsePacket
                        {
                            Success = true,
                            Code = ErrorCodes.Ok,
                            Command = Commands.GameEvent,
                            GameEvent = gameEvent
                        }, token).ConfigureAwait(false);
                    }

                    ResponsePacket snapshot = TcpGameRoomRegistry.State(userId);
                    if (!snapshot.Success || !snapshot.IsRacing)
                        return;

                    if (!mailbox.TryQueueSnapshot(snapshot))
                        throw new InvalidOperationException("The game snapshot mailbox is closed.");

                    await Task.Delay(TimeSpan.FromMilliseconds(50), token).ConfigureAwait(false);
                }
            }
            finally
            {
                mailbox.Complete();
            }
        }

        private static async Task SendGameStateAsync(
            ClientSession session,
            Stream stream,
            GameClientMailbox mailbox,
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (mailbox.TryReadImportantEvent(out ResponsePacket message) ||
                    mailbox.TryReadSnapshot(out message))
                {
                    await session.SendLock.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        await MessageFramer.SendAsync(stream, message, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        session.SendLock.Release();
                    }
                    continue;
                }

                if (mailbox.IsCompleted)
                    return;

                await mailbox.WaitForDataAsync(token).ConfigureAwait(false);
            }
        }

        private static async Task<ResponsePacket> DispatchAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            ServerStateRepository state,
            CancellationToken token)
        {
            string command = (req.Command ?? string.Empty).Trim().ToUpperInvariant();
            bool mustDeduplicate = command is Commands.UpdateCoins or Commands.BuyItem;
            if (!mustDeduplicate) return await DispatchCoreAsync(session, req, repo, state, token).ConfigureAwait(false);

            if (!session.AuthenticatedUserId.HasValue || !Guid.TryParse(req.RequestId, out Guid requestGuid))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "A valid request id is required." };
            string fingerprint = command == Commands.UpdateCoins
                ? $"delta:{req.CoinsDelta?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}"
                : $"item:{req.Category?.Trim()}:{req.ItemIndex?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}:{req.Price?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}";
            string fingerprintHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fingerprint)));
            int userId = session.AuthenticatedUserId.Value;
            ResponsePacket response = await state.ExecuteIdempotentAsync(
                userId, command, requestGuid, fingerprintHash,
                async (conn, tx) =>
                {
                    if (command == Commands.UpdateCoins)
                    {
                        if (!req.CoinsDelta.HasValue || req.CoinsDelta.Value >= 0 || req.CoinsDelta.Value < -MaxCoinsDeltaPerRequest)
                            return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Client coin grants are disabled; rewards must be confirmed by the server." };
                        return await repo.CongTruXuTrongGiaoDichAsync(conn, tx, userId, req.CoinsDelta.Value, token).ConfigureAwait(false);
                    }

                    string category = (req.Category ?? string.Empty).Trim();
                    int itemIndex = req.ItemIndex ?? -1;
                    if (!CategoryRegex.IsMatch(category) || !string.Equals(category, "Scooter", StringComparison.Ordinal) ||
                        itemIndex < 0 || itemIndex >= ScooterPrices.Length)
                        return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid item." };

                    return await repo.MuaTrongGiaoDichAsync(conn, tx, userId, category, itemIndex, ScooterPrices[itemIndex], token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);

            repo.XoaCacheKhoSauGiaoDich(userId);
            if (response.Inventory != null)
                response.Inventory = await repo.LayKhoAsync(userId, token).ConfigureAwait(false);
            return response;
        }

        private static async Task<ResponsePacket> DispatchCoreAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            ServerStateRepository state,
            CancellationToken token)
        {
            string cmd = (req.Command ?? string.Empty).Trim().ToUpperInvariant();

            try
            {
                return cmd switch
                {
                    Commands.Ping => session.AuthenticatedUserId.HasValue
                        ? new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "PONG" }
                        : Unauth(),
                    Commands.Register => await HandleRegisterAsync(req, repo, state, token).ConfigureAwait(false),
                    Commands.Login => await HandleLoginAsync(session, req, repo, state, token).ConfigureAwait(false),
                    Commands.GetInfo => await HandleGetInfoAsync(session, repo, token).ConfigureAwait(false),
                    Commands.Logout => await HandleLogoutAsync(session, state, token).ConfigureAwait(false),
                    Commands.ResetPassword => await HandleResetPasswordAsync(req, repo, state, token).ConfigureAwait(false),
                    Commands.RequestResetCode => await HandleRequestResetCodeAsync(req, repo, state, token).ConfigureAwait(false),
                    Commands.RequestRegistrationCode => await HandleRequestRegistrationCodeAsync(req, repo, state, token).ConfigureAwait(false),
                    Commands.JoinRoom => await HandleJoinRoomAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.ChangeAvatar => await HandleChangeAvatarAsync(session, req, repo, token).ConfigureAwait(false),

                    Commands.AuthToken => await HandleAuthTokenAsync(session, req, state, token).ConfigureAwait(false),
                    Commands.GetInventory => await HandleGetInventoryAsync(session, repo, token).ConfigureAwait(false),
                    Commands.Equip => await HandleEquipAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.SetMap => await HandleSetMapAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.UploadAvatar => await HandleUploadAvatarAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.GetAvatar => await HandleGetAvatarAsync(session, repo, token).ConfigureAwait(false),
                    Commands.SubmitReview => await HandleSubmitReviewAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.GetReviews => await HandleGetReviewsAsync(session, repo, token).ConfigureAwait(false),
                    Commands.Leaderboard => await HandleLeaderboardAsync(session, repo, token).ConfigureAwait(false),
                    Commands.UpsertRoom => await HandleUpsertRoomAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.RemoveRoom => HandleRemoveRoom(session, req),
                    Commands.ListRooms => HandleListRooms(session),
                    Commands.Invite => await HandleInviteAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.GetInvites => HandleGetInvites(session),
                    Commands.GetState => await HandleGetStateAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.CreateGameRoom => HandleCreateGameRoom(session, req),
                    Commands.JoinGameRoom => HandleJoinGameRoom(session, req),
                    Commands.LeaveGameRoom => HandleLeaveGameRoom(session),
                    Commands.GameRoomState => HandleGameRoomState(session),
                    Commands.StartGameRoom => HandleStartGameRoom(session, req),
                    Commands.GameInput => await HandleGameInputAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.GameStream => HandleGameStream(session),
                    Commands.GameSpectate => HandleGameSpectate(session, req),
                    Commands.GameReconnect => HandleGameReconnect(session, req),
                    Commands.ChatSend => HandleChatSend(session, req),
                    Commands.ChatPoll => HandleChatPoll(session, req),
                    Commands.GameEvent => await HandleGameEventAsync(session, req, repo, token).ConfigureAwait(false),

                    _ => new ResponsePacket
                    {
                        Success = false,
                        Code = ErrorCodes.InvalidInput,
                        Message = "Unsupported command."
                    }
                };
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL] Error ({cmd}): {ex.Message}");
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.ServerError,
                    Message = "Database error occurred. Please try again later."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Error ({cmd}): {ex.Message}");
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.ServerError,
                    Message = "Unexpected server error occurred."
                };
            }
        }

        private static ResponsePacket Unauth() => new()
        {
            Success = false,
            Code = ErrorCodes.Unauthorized,
            Message = "Session is not authenticated."
        };

        private static ResponsePacket HandleCreateGameRoom(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue || string.IsNullOrWhiteSpace(session.AuthenticatedUsername))
                return Unauth();
            if (!string.IsNullOrWhiteSpace(request.MapName) && !MapRegex.IsMatch(request.MapName.Trim()))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid map name." };
            return TcpGameRoomRegistry.Create(session.AuthenticatedUserId.Value, session.AuthenticatedUsername, request);
        }

        private static ResponsePacket HandleJoinGameRoom(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue || string.IsNullOrWhiteSpace(session.AuthenticatedUsername))
                return Unauth();
            if (!RoomCodeRegex.IsMatch((request.RoomId ?? string.Empty).Trim()))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid room code." };
            return TcpGameRoomRegistry.Join(session.AuthenticatedUserId.Value, session.AuthenticatedUsername, request.RoomId!);
        }

        private static ResponsePacket HandleLeaveGameRoom(ClientSession session)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            return TcpGameRoomRegistry.Leave(session.AuthenticatedUserId.Value);
        }

        private static ResponsePacket HandleGameRoomState(ClientSession session)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            return TcpGameRoomRegistry.State(session.AuthenticatedUserId.Value);
        }

        private static ResponsePacket HandleStartGameRoom(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            if (!string.IsNullOrWhiteSpace(request.MapName) && !MapRegex.IsMatch(request.MapName.Trim()))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid map name." };
            return TcpGameRoomRegistry.Start(session.AuthenticatedUserId.Value, request);
        }

        private static async Task<ResponsePacket> HandleGameInputAsync(
            ClientSession session,
            RequestPacket request,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();

            int userId = session.AuthenticatedUserId.Value;
            ResponsePacket response = TcpGameRoomRegistry.SubmitInput(userId, request);
            await AwardWinnerIfNeededAsync(userId, repo, token).ConfigureAwait(false);
            return response;
        }

        private static ResponsePacket HandleGameSpectate(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            if (!RoomCodeRegex.IsMatch((request.RoomId ?? string.Empty).Trim()))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid room code." };
            return TcpGameRoomRegistry.Spectate(session.AuthenticatedUserId.Value, request.RoomId!);
        }

        private static ResponsePacket HandleGameReconnect(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            return TcpGameRoomRegistry.Reconnect(session.AuthenticatedUserId.Value, request.RoomId);
        }

        private static ResponsePacket HandleGameStream(ClientSession session)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();

            ResponsePacket state = TcpGameRoomRegistry.State(session.AuthenticatedUserId.Value);
            if (!state.Success || !state.IsRacing)
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Join a running game before subscribing to snapshots."
                };

            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Command = Commands.GameStream,
                RoomId = state.RoomId,
                Message = "Snapshot stream started."
            };
        }

        private static ResponsePacket HandleChatSend(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            return TcpGameRoomRegistry.SendChat(session.AuthenticatedUserId.Value, request.ChatMessage);
        }

        private static ResponsePacket HandleChatPoll(ClientSession session, RequestPacket request)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();
            return TcpGameRoomRegistry.PollChat(session.AuthenticatedUserId.Value, request.AfterMessageId ?? 0);
        }

        private static async Task<ResponsePacket> HandleGameEventAsync(
            ClientSession session,
            RequestPacket request,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return Unauth();

            int userId = session.AuthenticatedUserId.Value;
            ResponsePacket response = TcpGameRoomRegistry.SubmitEvent(userId, request);
            await AwardWinnerIfNeededAsync(userId, repo, token).ConfigureAwait(false);
            return response;
        }

        private static async Task AwardWinnerIfNeededAsync(
            int requestingUserId,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (TcpGameRoomRegistry.TryBeginWinnerReward(
                    requestingUserId, out string roomCode, out Guid matchId, out int winnerUserId))
            {
                try
                {
                    await repo.AwardRaceWinnerAsync(
                        matchId, winnerUserId, TcpGameRoomRegistry.WinnerRewardCoins, token).ConfigureAwait(false);
                    TcpGameRoomRegistry.CompleteWinnerReward(roomCode, succeeded: true);
                }
                catch
                {
                    TcpGameRoomRegistry.CompleteWinnerReward(roomCode, succeeded: false);
                    throw;
                }
            }
        }

        private static async Task<ResponsePacket> HandleLogoutAsync(ClientSession session, ServerStateRepository state, CancellationToken token)
        {
            int? userId = session.AuthenticatedUserId;
            if (userId.HasValue)
            {
                await state.RevokeUserTokensAsync(userId.Value, token).ConfigureAwait(false);
                foreach (ClientSession active in ActiveSessions.Values.Where(s => s.AuthenticatedUserId == userId.Value && s != session))
                {
                    active.AuthenticatedUserId = null;
                    active.AuthenticatedUsername = null;
                    active.AuthToken = null;
                    try { active.Client.Close(); } catch { }
                }
                RoomRegistry.RemoveByOwner(userId.Value);
                TcpGameRoomRegistry.RemoveUser(userId.Value);
            }
            session.AuthenticatedUserId = null;
            session.AuthenticatedUsername = null;
            session.AuthToken = null;
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "Logged out."
            };
        }

        private static async Task<ResponsePacket> HandleRegisterAsync(
            RequestPacket req,
            NguoiDungRepository repo,
            ServerStateRepository state,
            CancellationToken token)
        {
            string? racerId = req.RacerId?.Trim();
            string username = (req.Username ?? string.Empty).Trim();
            string password = req.Password ?? string.Empty;
            string fullName = (req.FullName ?? string.Empty).Trim();
            string email = (req.Email ?? string.Empty).Trim();
            string? gender = req.Gender?.Trim();
            string? avatarPath = req.AvatarPath?.Trim();

            if (!UsernameRegex.IsMatch(username) ||
                !AccountInputValidation.IsValidPassword(password) ||
                fullName.Length < 2 || fullName.Length > 100 ||
                !AccountInputValidation.IsValidEmail(email) ||
                (racerId != null && racerId.Length > 30) ||
                (gender != null && gender.Length > 20) ||
                (avatarPath != null && avatarPath.Length > 260))
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Invalid registration details."
                };
            }

            string verificationKey = $"register|{username}|{email.ToLowerInvariant()}";

            if (req.Birthday.HasValue)
            {
                DateTime bday = req.Birthday.Value.Date;
                if (bday < new DateTime(1920, 1, 1) || bday > DateTime.Today)
                {
                    return new ResponsePacket
                    {
                        Success = false,
                        Code = ErrorCodes.InvalidInput,
                        Message = "Invalid birthday."
                    };
                }
            }

            if (!await state.VerifyAndConsumeCodeAsync(verificationKey, req.VerificationCode, token).ConfigureAwait(false))
                return new ResponsePacket { Success = false, Code = ErrorCodes.AuthFailed, Message = "Verification code is invalid or expired." };
            var result = await repo.DangKyAsync(req, token).ConfigureAwait(false);
            return new ResponsePacket
            {
                Success = result.Success,
                Code = result.Code,
                Message = result.Message,
                User = result.Success
                    ? new UserInfoDto
                    {
                        Username = username,
                        FullName = fullName,
                        RacerId = result.RacerId,
                        Coins = NguoiDungRepository.StartingCoins
                    }
                    : null
            };
        }

        private static async Task<ResponsePacket> HandleLoginAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            ServerStateRepository state,
            CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string password = req.Password ?? string.Empty;

            if (!UsernameRegex.IsMatch(username) ||
                string.IsNullOrEmpty(password) ||
                password.Length > AccountInputValidation.MaxPasswordLength)
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "Invalid username or password."
                };
            }

            string remoteAddress = session.Client.Client.RemoteEndPoint is IPEndPoint endpoint
                ? endpoint.Address.ToString()
                : "unknown";
            string limiterKey = $"{username.ToUpperInvariant()}|{remoteAddress}";
            if (!await state.TryBeginLoginAttemptAsync(limiterKey, MaxLoginAttemptsPerWindow, LoginAttemptWindow, LoginLockDuration, token).ConfigureAwait(false))
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "Invalid username or password."
                };
            }

            UserInfoDto? user;
            try
            {
                user = await repo.DangNhapAsync(username, password, token).ConfigureAwait(false);
            }
            catch
            {
                await state.ReleaseLoginAttemptAsync(limiterKey, token).ConfigureAwait(false);
                throw;
            }
            if (user == null)
            {
                await state.RecordFailedLoginAsync(limiterKey, MaxLoginAttemptsPerWindow, LoginLockDuration, token).ConfigureAwait(false);
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "Invalid username or password."
                };
            }

            await state.ClearLoginAttemptsAsync(limiterKey, token).ConfigureAwait(false);

            // Chỉ lưu hash của token; game nhận token gốc đúng một lần ở response.
            string authToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            await state.SaveTokenAsync(authToken, user.UserId, user.Username, DateTime.UtcNow.AddHours(6), token).ConfigureAwait(false);
            session.AuthenticatedUserId = user.UserId;
            session.AuthenticatedUsername = user.Username;
            session.AuthToken = authToken;

            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "Login successful.",
                User = user,
                Token = authToken
            };
        }

        private static void ConfigureTcpKeepAlive(TcpClient client)
        {
            try
            {
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[TCP] Keep-alive unavailable: {ex.SocketErrorCode}");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("[TCP] Keep-alive is not supported on this platform.");
            }
        }

        private static async Task<ResponsePacket> HandleAuthTokenAsync(ClientSession session, RequestPacket req, ServerStateRepository state, CancellationToken token)
        {
            if (!string.IsNullOrEmpty(req.Token))
            {
                var stored = await state.ValidateTokenAsync(req.Token, token).ConfigureAwait(false);
                if (stored.HasValue)
                {
                    session.AuthenticatedUserId = stored.Value.UserId;
                    session.AuthenticatedUsername = stored.Value.Username;
                    session.AuthToken = req.Token;
                    return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "OK" };
                }
            }

            return new ResponsePacket
            {
                Success = false,
                Code = ErrorCodes.Unauthorized,
                Message = "Invalid token."
            };
        }

        private static async Task<ResponsePacket> HandleRequestResetCodeAsync(
            RequestPacket req, NguoiDungRepository repo, ServerStateRepository state, CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string email = (req.Email ?? string.Empty).Trim();
            if (!UsernameRegex.IsMatch(username) || !AccountInputValidation.IsValidEmail(email))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid username or email." };

            string key = $"reset|{username}|{email.ToLowerInvariant()}";
            if (!await state.TryReserveVerificationAsync(key, DateTime.UtcNow.AddSeconds(60), token).ConfigureAwait(false))
                return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "If the account matches, a code has been sent to the registered email." };

            UserInfoDto? user = await repo.LayThongTinAsync(username, token).ConfigureAwait(false);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email) &&
                string.Equals(user.Email.Trim(), email, StringComparison.OrdinalIgnoreCase))
            {
                string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
                try
                {
                    await EmailVerificationService.SendCodeAsync(email, code, passwordReset: true).ConfigureAwait(false);
                    await state.SaveVerificationCodeAsync(key, code, DateTime.UtcNow.AddMinutes(10), token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SMTP] Reset code send failed: " + ex.Message);
                    await ReleaseVerificationReservationAsync(state, key).ConfigureAwait(false);
                    return new ResponsePacket { Success = false, Code = ErrorCodes.ServerError, Message = "Could not send the verification email. Please try again later." };
                }
            }

            // Same response for unknown usernames/emails to avoid exposing account membership.
            return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "If the account matches, a code has been sent to the registered email." };
        }

        private static async Task<ResponsePacket> HandleRequestRegistrationCodeAsync(
            RequestPacket req, NguoiDungRepository repo, ServerStateRepository state, CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string email = (req.Email ?? string.Empty).Trim();
            if (!UsernameRegex.IsMatch(username) || !AccountInputValidation.IsValidEmail(email))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid username or email." };

            string key = $"register|{username}|{email.ToLowerInvariant()}";
            string responseMessage = "If the username is available, a code has been sent to the email.";
            if (!await state.TryReserveVerificationAsync(key, DateTime.UtcNow.AddSeconds(60), token).ConfigureAwait(false))
                return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = responseMessage };

            if (!await repo.UsernameExistsAsync(username, token).ConfigureAwait(false))
            {
                string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
                try
                {
                    await EmailVerificationService.SendCodeAsync(email, code, passwordReset: false).ConfigureAwait(false);
                    await state.SaveVerificationCodeAsync(key, code, DateTime.UtcNow.AddMinutes(10), token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SMTP] Registration code send failed: " + ex.Message);
                    await ReleaseVerificationReservationAsync(state, key).ConfigureAwait(false);
                    return new ResponsePacket { Success = false, Code = ErrorCodes.ServerError, Message = "Could not send the verification email. Please try again later." };
                }
            }
            return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = responseMessage };
        }

        private static async Task ReleaseVerificationReservationAsync(ServerStateRepository state, string key)
        {
            try
            {
                await state.ReleaseVerificationReservationAsync(key, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[OTP] Failed to release verification reservation: " + ex.Message);
            }
        }

        private static async Task<ResponsePacket> HandleGetInfoAsync(
            ClientSession session,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue || string.IsNullOrWhiteSpace(session.AuthenticatedUsername))
                return Unauth();

            UserInfoDto? user = await repo.LayThongTinTheoIdAsync(session.AuthenticatedUserId.Value, token)
                .ConfigureAwait(false);
            if (user == null)
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "User not found."
                };
            }

            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "User info loaded.",
                User = user
            };
        }

        private static async Task<ResponsePacket> HandleGetInventoryAsync(
            ClientSession session,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();
            return await InvResp(session, repo, token, true, ErrorCodes.Ok, "OK").ConfigureAwait(false);
        }

        private static async Task<ResponsePacket> HandleEquipAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            string cat = (req.Category ?? "").Trim();
            int idx = req.ItemIndex ?? -1;
            if (!CategoryRegex.IsMatch(cat) || idx < 0 || idx > 999)
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid item." };

            bool ok = await repo.TrangBiAsync(session.AuthenticatedUserId.Value, cat, idx, token).ConfigureAwait(false);
            return await InvResp(session, repo, token, ok, ok ? ErrorCodes.Ok : ErrorCodes.InvalidInput,
                ok ? "Equipped." : "You do not own this item.").ConfigureAwait(false);
        }

        private static async Task<ResponsePacket> HandleSetMapAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            string map = (req.MapName ?? "").Trim();
            if (!MapRegex.IsMatch(map))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid map." };

            await repo.DatMapAsync(session.AuthenticatedUserId.Value, map, token).ConfigureAwait(false);
            return await InvResp(session, repo, token, true, ErrorCodes.Ok, "Map saved.").ConfigureAwait(false);
        }

        private static async Task<ResponsePacket> HandleSubmitReviewAsync(
            ClientSession session, RequestPacket req, NguoiDungRepository repo, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            int stars = req.Stars ?? 0;
            string? comment = req.Comment?.Trim();
            if (stars < 1 || stars > 5 || (comment != null && comment.Length > 300))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid review." };
            if (string.IsNullOrEmpty(comment)) comment = null;

            int uid = session.AuthenticatedUserId.Value;
            bool ok = await repo.GuiDanhGiaAsync(uid, stars, comment, token).ConfigureAwait(false);

            return new ResponsePacket
            {
                Success = ok,
                Code = ok ? ErrorCodes.Ok : ErrorCodes.AlreadyReviewed,
                Message = ok ? "Review saved." : "You have already reviewed.",
                ReviewSummary = await repo.LayTomTatDanhGiaAsync(uid, token).ConfigureAwait(false)
            };
        }

        private static async Task<ResponsePacket> HandleGetReviewsAsync(
            ClientSession session, NguoiDungRepository repo, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "OK",
                ReviewSummary = await repo.LayTomTatDanhGiaAsync(session.AuthenticatedUserId.Value, token).ConfigureAwait(false)
            };
        }

        private static async Task<ResponsePacket> HandleLeaderboardAsync(
            ClientSession session, NguoiDungRepository repo, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "OK",
                Leaderboard = await repo.LayBangXepHangAsync(20, token).ConfigureAwait(false)
            };
        }

        private static async Task<ResponsePacket> HandleUpsertRoomAsync(
            ClientSession session, RequestPacket req, NguoiDungRepository repo, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            string code = (req.RoomId ?? "").Trim();
            int cur = req.PlayerCount ?? 0, max = req.MaxPlayers ?? 0;
            if (!RoomCodeRegex.IsMatch(code) || cur < 0 || cur > 16 || max < 1 || max > 16)
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid room." };

            int uid = session.AuthenticatedUserId.Value;
            string? host = RoomRegistry.HostNameIfOwned(code, uid);
            if (host == null)
            {
                UserInfoDto? u = await repo.LayThongTinTheoIdAsync(uid, token).ConfigureAwait(false);
                host = u == null ? "?" : string.IsNullOrEmpty(u.RacerId) ? u.Username : $"{u.Username} #{u.RacerId}";
            }

            if (!RoomRegistry.Upsert(code, uid, host, cur, max, req.IsRacing ?? false))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Room code is already registered by another host." };
            return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "OK" };
        }

        private static ResponsePacket HandleRemoveRoom(ClientSession session, RequestPacket req)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();
            string code = (req.RoomId ?? "").Trim();
            if (RoomCodeRegex.IsMatch(code)) RoomRegistry.Remove(code, session.AuthenticatedUserId.Value);
            return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "OK" };
        }

        private static ResponsePacket HandleListRooms(ClientSession session)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();
            return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "OK", Rooms = RoomRegistry.List() };
        }

        private static async Task<ResponsePacket> HandleInviteAsync(
            ClientSession session, RequestPacket req, NguoiDungRepository repo, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            string key = (req.TargetId ?? "").Trim().TrimStart('#');
            string code = (req.RoomId ?? "").Trim();
            if (!TargetRegex.IsMatch(key) || !RoomCodeRegex.IsMatch(code))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid player ID or room." };

            if (!RoomRegistry.Exists(code))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Room is not registered yet. Try again in a few seconds." };

            UserInfoDto? target = await repo.TimNguoiChoiAsync(key, token).ConfigureAwait(false);
            if (target == null)
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Player not found." };
            if (target.UserId == session.AuthenticatedUserId.Value)
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "You cannot invite yourself." };

            UserInfoDto? me = await repo.LayThongTinTheoIdAsync(session.AuthenticatedUserId.Value, token).ConfigureAwait(false);
            string from = me == null ? "Someone" : string.IsNullOrEmpty(me.RacerId) ? me.Username : $"{me.Username} #{me.RacerId}";

            bool ok = RoomRegistry.AddInvite(target.UserId, code, from);
            return new ResponsePacket
            {
                Success = ok,
                Code = ok ? ErrorCodes.Ok : ErrorCodes.InvalidInput,
                Message = ok ? $"Invite sent to {target.Username}." : "That player has too many pending invites."
            };
        }

        private static ResponsePacket HandleGetInvites(ClientSession session)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "OK",
                Invites = RoomRegistry.TakeNewInvites(session.AuthenticatedUserId.Value)
            };
        }

        // LAYTRANGTHAI: kho + lời mời trong 1 response.
        // Mode == "room" (đang ở trong phòng) -> KHÔNG lấy lời mời, để chúng nằm lại cho tới khi người chơi ra khỏi phòng.
        private static async Task<ResponsePacket> HandleGetStateAsync(
            ClientSession session, RequestPacket req, NguoiDungRepository repo, CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            bool inRoom = string.Equals(req.Mode?.Trim(), "room", StringComparison.OrdinalIgnoreCase);

            // Đọc kho trước: nếu DB lỗi thì lời mời chưa bị lấy đi
            ResponsePacket resp = await InvResp(session, repo, token, true, ErrorCodes.Ok, "OK").ConfigureAwait(false);
            if (!inRoom)
                resp.Invites = RoomRegistry.TakeNewInvites(session.AuthenticatedUserId.Value);
            return resp;
        }

        private const int MaxAvatarBytes = 40 * 1024;

        private static async Task<ResponsePacket> HandleUploadAvatarAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            byte[] data;
            try { data = Convert.FromBase64String(req.AvatarBase64 ?? ""); }
            catch (FormatException)
            {
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid avatar data." };
            }

            bool isJpeg = data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8;
            bool isPng = data.Length > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
            if (data.Length == 0 || data.Length > MaxAvatarBytes || (!isJpeg && !isPng))
            {
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Avatar must be a PNG/JPEG up to 40KB." };
            }

            bool ok = await repo.CapNhatAvatarDuLieuAsync(session.AuthenticatedUserId.Value, data, token).ConfigureAwait(false);
            return new ResponsePacket
            {
                Success = ok,
                Code = ok ? ErrorCodes.Ok : ErrorCodes.AuthFailed,
                Message = ok ? "Avatar updated." : "User not found."
            };
        }

        private static async Task<ResponsePacket> HandleGetAvatarAsync(
            ClientSession session,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            byte[]? data = await repo.LayAvatarDuLieuAsync(session.AuthenticatedUserId.Value, token).ConfigureAwait(false);
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = data == null ? "No avatar." : "OK",
                AvatarBase64 = data == null ? "" : Convert.ToBase64String(data)
            };
        }

        private static async Task<ResponsePacket> InvResp(
            ClientSession session, NguoiDungRepository repo, CancellationToken token,
            bool ok, string code, string msg)
        {
            InventoryDto inv = await repo.LayKhoAsync(session.AuthenticatedUserId!.Value, token).ConfigureAwait(false);
            return new ResponsePacket { Success = ok, Code = code, Message = msg, Inventory = inv };
        }

        private static async Task<ResponsePacket> HandleJoinRoomAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            string mode = string.IsNullOrWhiteSpace(req.Mode) ? "single" : req.Mode.Trim();
            string roomId = (req.RoomId ?? string.Empty).Trim();

            UserInfoDto? user = await repo.LayThongTinTheoIdAsync(session.AuthenticatedUserId.Value, token)
                .ConfigureAwait(false);
            if (user == null)
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "User not found."
                };
            }

            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "Room ready.",
                Mode = mode,
                RoomId = roomId,
                User = user
            };
        }

        private static async Task<ResponsePacket> HandleChangeAvatarAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            string? avatarPath = req.AvatarPath?.Trim();
            if (avatarPath != null && avatarPath.Length > 260)
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Avatar path is too long."
                };
            }

            var result = await repo.CapNhatAvatarAsync(session.AuthenticatedUserId.Value, avatarPath, token)
                .ConfigureAwait(false);

            return new ResponsePacket
            {
                Success = result.Success,
                Code = result.Code,
                Message = result.Message,
                User = result.User
            };
        }

        private static async Task<ResponsePacket> HandleResetPasswordAsync(
            RequestPacket req,
            NguoiDungRepository repo,
            ServerStateRepository state,
            CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string email = (req.Email ?? string.Empty).Trim();
            string newPassword = req.Password ?? string.Empty;

            if (!UsernameRegex.IsMatch(username) ||
                !AccountInputValidation.IsValidEmail(email) ||
                !AccountInputValidation.IsValidPassword(newPassword))
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Invalid recovery information or password format."
                };
            }

            string key = $"reset|{username}|{email.ToLowerInvariant()}";
            if (!await state.VerifyAndConsumeCodeAsync(key, req.VerificationCode, token).ConfigureAwait(false))
                return new ResponsePacket { Success = false, Code = ErrorCodes.AuthFailed, Message = "Verification code is invalid or expired." };

            var result = await repo.DoiMatKhauAsync(username, email, newPassword, token).ConfigureAwait(false);
            if (result.Success)
            {
                UserInfoDto? user = await repo.LayThongTinAsync(username, token).ConfigureAwait(false);
                if (user != null)
                {
                    RoomRegistry.RemoveByOwner(user.UserId);
                    foreach (ClientSession active in ActiveSessions.Values.Where(s => s.AuthenticatedUserId == user.UserId))
                    {
                        active.AuthenticatedUserId = null;
                        active.AuthenticatedUsername = null;
                        active.AuthToken = null;
                        try { active.Client.Close(); } catch { }
                    }
                }
            }
            return new ResponsePacket { Success = result.Success, Code = result.Code, Message = result.Message };
        }

    }
}
