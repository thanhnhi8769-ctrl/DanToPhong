using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        private const int Port = 9000;
        private const int MaxCoinsDeltaPerRequest = 5000;
        private const string ConnectionString =
            "Server=localhost,1433;Database=DanToPhongDB;User Id=sa;Password=Tn123456;TrustServerCertificate=True;";

        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex HasLetterRegex = new(@"[A-Za-z]", RegexOptions.Compiled);
        private static readonly Regex HasDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex CategoryRegex = new(@"^[A-Za-z_]{1,30}$", RegexOptions.Compiled);
        private static readonly Regex RoomCodeRegex = new(@"^[A-Za-z0-9]{3,12}$", RegexOptions.Compiled);
        private static readonly Regex TargetRegex = new(@"^[A-Za-z0-9_]{1,20}$", RegexOptions.Compiled);
        private static readonly Regex MapRegex = new(@"^[A-Za-z0-9_ \-]{1,50}$", RegexOptions.Compiled);

        // Token do launcher giữ, truyền cho game Unity. Chỉ nằm trong RAM server.
        private static readonly ConcurrentDictionary<string, (int UserId, string Username, DateTime Exp)> Tokens = new();

        // Bảng giá phía server. Nếu có key ở đây thì dùng giá này; chưa có thì tạm dùng giá client gửi (đã kẹp 0..5000).
        // Điền theo VehicleCatalog, ví dụ: { ("Scooter", 1), 300 }, { ("Scooter", 2), 500 }
        // Giá xe theo index trong VehicleCatalog. Thêm xe mới vào catalog thì thêm giá vào đây.
        private static readonly int[] ScooterPrices =
        {
    0, 100, 150, 180, 180, 180, 200, 200, 200, 220, 220, 250, 260,
    260, 280, 280, 280, 300, 300, 320, 320, 340, 360, 380, 420
};

        private static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var repo = new NguoiDungRepository(ConnectionString);
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await repo.KiemTraKetNoiAsync(cts.Token);
                Console.WriteLine("[DB] Connected to DanToPhongDB successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Warning: Cannot connect to DanToPhongDB at startup. ({ex.Message})");
            }

            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Console.WriteLine($"[TCP] Server listening on port {Port}...");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                    string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

                    var session = new ClientSession
                    {
                        Client = client,
                        RemoteEndPoint = remote
                    };

                    Console.WriteLine($"[{session.SessionId}] Connected from {remote}");
                    _ = Task.Run(() => HandleClientAsync(session, repo, cts.Token), cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                listener.Stop();
                Console.WriteLine("[TCP] Server stopped.");
            }
        }

        private static async Task HandleClientAsync(
            ClientSession session,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            NetworkStream stream = session.Client.GetStream();

            try
            {
                while (!token.IsCancellationRequested && session.Client.Connected)
                {
                    RequestPacket? request = await MessageFramer.ReceiveAsync<RequestPacket>(stream, token).ConfigureAwait(false);
                    if (request == null)
                        break;

                    ResponsePacket response = await DispatchAsync(session, request, repo, token).ConfigureAwait(false);

                    await session.SendLock.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        await MessageFramer.SendAsync(stream, response, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        session.SendLock.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{session.SessionId}] Session error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[{session.SessionId}] Disconnected ({session.AuthenticatedUsername ?? "guest"})");
                if (session.AuthenticatedUserId.HasValue)
                    RoomRegistry.RemoveByOwner(session.AuthenticatedUserId.Value);
                try { stream.Dispose(); } catch { }
                try { session.Client.Close(); } catch { }
                try { session.Client.Dispose(); } catch { }
                session.SendLock.Dispose();
            }
        }

        private static async Task<ResponsePacket> DispatchAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            string cmd = (req.Command ?? string.Empty).Trim().ToUpperInvariant();

            try
            {
                return cmd switch
                {
                    Commands.Register => await HandleRegisterAsync(req, repo, token).ConfigureAwait(false),
                    Commands.Login => await HandleLoginAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.GetInfo => await HandleGetInfoAsync(session, repo, token).ConfigureAwait(false),
                    Commands.Logout => HandleLogout(session),
                    Commands.ResetPassword => await HandleResetPasswordAsync(req, repo, token).ConfigureAwait(false),
                    Commands.UpdateCoins => await HandleUpdateCoinsAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.JoinRoom => await HandleJoinRoomAsync(session, req, repo, token).ConfigureAwait(false),
                    Commands.ChangeAvatar => await HandleChangeAvatarAsync(session, req, repo, token).ConfigureAwait(false),

                    Commands.AuthToken => HandleAuthToken(session, req),
                    Commands.GetInventory => await HandleGetInventoryAsync(session, repo, token).ConfigureAwait(false),
                    Commands.BuyItem => await HandleBuyAsync(session, req, repo, token).ConfigureAwait(false),
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

        private static ResponsePacket HandleLogout(ClientSession session)
        {
            session.AuthenticatedUserId = null;
            session.AuthenticatedUsername = null;
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
                !IsValidPassword(password) ||
                fullName.Length < 2 || fullName.Length > 100 ||
                (email.Length > 0 && !EmailRegex.IsMatch(email)) ||
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
            CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string password = req.Password ?? string.Empty;

            if (!UsernameRegex.IsMatch(username) || string.IsNullOrEmpty(password))
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "Invalid username or password."
                };
            }

            UserInfoDto? user = await repo.DangNhapAsync(username, password, token).ConfigureAwait(false);
            if (user == null)
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.AuthFailed,
                    Message = "Invalid username or password."
                };
            }

            session.AuthenticatedUserId = user.UserId;
            session.AuthenticatedUsername = user.Username;

            // dọn token hết hạn, rồi cấp token mới cho game Unity
            foreach (var kv in Tokens)
                if (kv.Value.Exp < DateTime.UtcNow) Tokens.TryRemove(kv.Key, out _);

            string authToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            Tokens[authToken] = (user.UserId, user.Username, DateTime.UtcNow.AddHours(6));

            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "Login successful.",
                User = user,
                Token = authToken
            };
        }

        private static ResponsePacket HandleAuthToken(ClientSession session, RequestPacket req)
        {
            if (!string.IsNullOrEmpty(req.Token) &&
                Tokens.TryGetValue(req.Token, out var t) &&
                t.Exp > DateTime.UtcNow)
            {
                session.AuthenticatedUserId = t.UserId;
                session.AuthenticatedUsername = t.Username;
                return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "OK" };
            }

            return new ResponsePacket
            {
                Success = false,
                Code = ErrorCodes.Unauthorized,
                Message = "Invalid token."
            };
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

        private static async Task<ResponsePacket> HandleUpdateCoinsAsync(
            ClientSession session,
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (!session.AuthenticatedUserId.HasValue) return Unauth();

            if (!req.CoinsDelta.HasValue || req.CoinsDelta.Value == 0)
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Invalid coins delta."
                };
            }

            int delta = Math.Clamp(req.CoinsDelta.Value, -MaxCoinsDeltaPerRequest, MaxCoinsDeltaPerRequest);

            var result = await repo.CongTruXuAsync(session.AuthenticatedUserId.Value, delta, token)
                .ConfigureAwait(false);

            return new ResponsePacket
            {
                Success = result.Success,
                Code = result.Code,
                Message = result.Message,
                User = result.User,
                Inventory = await repo.LayKhoAsync(session.AuthenticatedUserId.Value, token).ConfigureAwait(false)
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

        private static async Task<ResponsePacket> HandleBuyAsync(
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

            int price;
            if (cat == "Scooter")
            {
                if (idx >= ScooterPrices.Length)
                    return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Invalid item." };
                price = ScooterPrices[idx];   // luôn dùng giá server, bỏ qua giá client gửi
            }
            else
            {
                price = Math.Clamp(req.Price ?? 0, 0, MaxCoinsDeltaPerRequest);
            }

            var (ok, code) = await repo.MuaAsync(session.AuthenticatedUserId.Value, cat, idx, price, token)
                .ConfigureAwait(false);

            return await InvResp(session, repo, token, ok, code, ok ? "Purchased." : "Purchase failed.")
                .ConfigureAwait(false);
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

            RoomRegistry.Upsert(code, uid, host, cur, max, req.IsRacing ?? false);
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
            CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string email = (req.Email ?? string.Empty).Trim();
            string newPassword = req.Password ?? string.Empty;

            if (!UsernameRegex.IsMatch(username) ||
                !EmailRegex.IsMatch(email) ||
                !IsValidPassword(newPassword))
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.InvalidInput,
                    Message = "Invalid recovery information or password format."
                };
            }

            var result = await repo.DoiMatKhauAsync(username, email, newPassword, token).ConfigureAwait(false);
            return new ResponsePacket
            {
                Success = result.Success,
                Code = result.Code,
                Message = result.Message
            };
        }

        private static bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   HasLetterRegex.IsMatch(password) &&
                   HasDigitRegex.IsMatch(password);
        }
    }
}