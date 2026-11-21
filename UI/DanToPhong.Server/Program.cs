using System;
using System.Net;
using System.Net.Sockets;
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
        private const string ConnectionString =
    "Server=localhost,1433;Database=DanToPhongDB;User Id=sa;Password=Tn123456;TrustServerCertificate=True;";
        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex HasLetterRegex = new(@"[A-Za-z]", RegexOptions.Compiled);
        private static readonly Regex HasDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

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
                // Server đang tắt theo tín hiệu dừng
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
                    await MessageFramer.SendAsync(stream, response, token).ConfigureAwait(false);
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
                try { stream.Dispose(); } catch { }
                try { session.Client.Close(); } catch { }
                try { session.Client.Dispose(); } catch { }
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
                switch (cmd)
                {
                    case Commands.Register:
                        return await HandleRegisterAsync(req, repo, token).ConfigureAwait(false);

                    case Commands.Login:
                        return await HandleLoginAsync(session, req, repo, token).ConfigureAwait(false);

                    case Commands.GetInfo:
                        return await HandleGetInfoAsync(session, repo, token).ConfigureAwait(false);

                    case Commands.Logout:
                        session.AuthenticatedUsername = null;
                        return new ResponsePacket
                        {
                            Success = true,
                            Code = ErrorCodes.Ok,
                            Message = "Logged out."
                        };

                    case Commands.ResetPassword:
                        return await HandleResetPasswordAsync(req, repo, token).ConfigureAwait(false);

                    default:
                        return new ResponsePacket
                        {
                            Success = false,
                            Code = ErrorCodes.InvalidInput,
                            Message = "Unsupported command."
                        };
                }
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

        private static async Task<ResponsePacket> HandleRegisterAsync(
            RequestPacket req,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string password = req.Password ?? string.Empty;
            string fullName = (req.FullName ?? string.Empty).Trim();
            string email = (req.Email ?? string.Empty).Trim();

            if (!UsernameRegex.IsMatch(username) ||
                !IsValidPassword(password) ||
                fullName.Length < 2 || fullName.Length > 100 ||
                (email.Length > 0 && !EmailRegex.IsMatch(email)))
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
                Message = result.Message
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

            session.AuthenticatedUsername = user.Username;

            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Message = "Login successful.",
                User = user
            };
        }

        private static async Task<ResponsePacket> HandleGetInfoAsync(
            ClientSession session,
            NguoiDungRepository repo,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(session.AuthenticatedUsername))
            {
                return new ResponsePacket
                {
                    Success = false,
                    Code = ErrorCodes.Unauthorized,
                    Message = "Session is not authenticated."
                };
            }

            UserInfoDto? user = await repo.LayThongTinAsync(session.AuthenticatedUsername, token).ConfigureAwait(false);
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