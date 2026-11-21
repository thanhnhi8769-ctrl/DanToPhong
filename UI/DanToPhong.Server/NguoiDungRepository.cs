using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DanToPhong.Protocol;
using Microsoft.Data.SqlClient;

namespace DanToPhong.Server
{
    public sealed class NguoiDungRepository
    {
        private readonly string _connectionString;

        public NguoiDungRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> KiemTraKetNoiAsync(CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            return true;
        }

        public async Task<(bool Success, string Code, string Message)> DangKyAsync(
            RequestPacket req, CancellationToken token = default)
        {
            string username = (req.Username ?? string.Empty).Trim();
            string password = req.Password ?? string.Empty;
            string fullName = (req.FullName ?? string.Empty).Trim();
            string? email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            DateTime? birthday = req.Birthday?.Date;

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string checkSql = "SELECT COUNT(1) FROM dbo.NguoiDung WHERE Username = @Username;";
            await using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username;
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(token).ConfigureAwait(false));
                if (count > 0)
                    return (false, ErrorCodes.UserExists, "Tên đăng nhập đã tồn tại.");
            }

            var (hash, salt) = MatKhau.BamMatKhau(password);

            const string insertSql = @"
                INSERT INTO dbo.NguoiDung (Username, PasswordHash, PasswordSalt, FullName, Email, Birthday)
                VALUES (@Username, @PasswordHash, @PasswordSalt, @FullName, @Email, @Birthday);";

            try
            {
                await using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username;
                insertCmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 128).Value = hash;
                insertCmd.Parameters.Add("@PasswordSalt", SqlDbType.NVarChar, 64).Value = salt;
                insertCmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 50).Value = fullName;
                insertCmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = (object?)email ?? DBNull.Value;
                insertCmd.Parameters.Add("@Birthday", SqlDbType.Date).Value = (object?)birthday ?? DBNull.Value;

                await insertCmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                return (true, ErrorCodes.Ok, "Đăng ký tài khoản thành công.");
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                return (false, ErrorCodes.UserExists, "Tên đăng nhập đã tồn tại.");
            }
        }

        public async Task<UserInfoDto?> DangNhapAsync(string username, string password, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string selectSql = @"
                SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, Email, Birthday, Coins, NgayTao, LanDangNhapCuoi
                FROM dbo.NguoiDung
                WHERE Username = @Username;";

            int userId;
            string dbUsername, storedHash, storedSalt, fullName;
            string? email;
            DateTime? birthday, lastLogin;
            int coins;
            DateTime createdAt;

            await using (var cmd = new SqlCommand(selectSql, conn))
            {
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username.Trim();
                await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (!await reader.ReadAsync(token).ConfigureAwait(false))
                    return null;

                userId = reader.GetInt32(0);
                dbUsername = reader.GetString(1);
                storedHash = reader.GetString(2);
                storedSalt = reader.GetString(3);
                fullName = reader.GetString(4);
                email = reader.IsDBNull(5) ? null : reader.GetString(5);
                birthday = reader.IsDBNull(6) ? null : reader.GetDateTime(6);
                coins = reader.GetInt32(7);
                createdAt = reader.GetDateTime(8);
                lastLogin = reader.IsDBNull(9) ? null : reader.GetDateTime(9);
            }

            if (!MatKhau.KiemTraMatKhau(password, storedHash, storedSalt))
                return null;

            DateTime now = DateTime.Now;
            const string updateSql = "UPDATE dbo.NguoiDung SET LanDangNhapCuoi = @Now WHERE UserId = @UserId;";
            await using (var upCmd = new SqlCommand(updateSql, conn))
            {
                upCmd.Parameters.Add("@Now", SqlDbType.DateTime2).Value = now;
                upCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                await upCmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            return new UserInfoDto
            {
                UserId = userId,
                Username = dbUsername,
                FullName = fullName,
                Email = email,
                Birthday = birthday,
                Coins = coins,
                NgayTao = createdAt,
                LanDangNhapCuoi = now
            };
        }

        public async Task<UserInfoDto?> LayThongTinAsync(string username, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string sql = @"
                SELECT UserId, Username, FullName, Email, Birthday, Coins, NgayTao, LanDangNhapCuoi
                FROM dbo.NguoiDung
                WHERE Username = @Username;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username.Trim();

            await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
                return null;

            return new UserInfoDto
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                FullName = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Birthday = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Coins = reader.GetInt32(5),
                NgayTao = reader.GetDateTime(6),
                LanDangNhapCuoi = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        public async Task<(bool Success, string Code, string Message)> DoiMatKhauAsync(
            string username,
            string email,
            string newPassword,
            CancellationToken token = default)
        {
            string cleanUser = username.Trim();
            string cleanEmail = email.Trim();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string verifySql = @"
                SELECT UserId
                FROM dbo.NguoiDung
                WHERE Username = @Username
                  AND Email IS NOT NULL
                  AND LOWER(LTRIM(RTRIM(Email))) = LOWER(@Email);";

            int userId;
            await using (var verifyCmd = new SqlCommand(verifySql, conn))
            {
                verifyCmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = cleanUser;
                verifyCmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = cleanEmail;

                object? result = await verifyCmd.ExecuteScalarAsync(token).ConfigureAwait(false);
                if (result == null || result == DBNull.Value)
                    return (false, ErrorCodes.AuthFailed, "Username or registered email does not match.");

                userId = Convert.ToInt32(result);
            }

            var (hash, salt) = MatKhau.BamMatKhau(newPassword);

            const string updateSql = @"
                UPDATE dbo.NguoiDung
                SET PasswordHash = @PasswordHash,
                    PasswordSalt = @PasswordSalt
                WHERE UserId = @UserId;";

            await using var updateCmd = new SqlCommand(updateSql, conn);
            updateCmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 128).Value = hash;
            updateCmd.Parameters.Add("@PasswordSalt", SqlDbType.NVarChar, 64).Value = salt;
            updateCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            int affected = await updateCmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            if (affected <= 0)
                return (false, ErrorCodes.ServerError, "Unable to update password.");

            return (true, ErrorCodes.Ok, "Password updated successfully.");
        }
    }
}