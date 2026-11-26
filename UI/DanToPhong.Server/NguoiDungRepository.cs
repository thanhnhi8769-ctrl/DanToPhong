using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        // Cache kho theo user. Mọi thay đổi xu/xe/map đều đi qua repository này nên xóa cache ở đó là đủ.
        // _invVer chống race: lần đọc bắt đầu TRƯỚC khi có ghi thì không được lưu kết quả cũ vào cache.
        private readonly ConcurrentDictionary<int, (InventoryDto Inv, long Ticks)> _invCache = new();
        private readonly ConcurrentDictionary<int, int> _invVer = new();
        private static readonly long InvTtlTicks = TimeSpan.FromSeconds(30).Ticks;

        private void XoaCacheKho(int userId)
        {
            _invVer.AddOrUpdate(userId, 1, (_, v) => v + 1);
            _invCache.TryRemove(userId, out _);
        }

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

        public const int StartingCoins = 100;   // xu của tài khoản mới, không phụ thuộc default trong DB

        public async Task<(bool Success, string Code, string Message, string? RacerId)> DangKyAsync(
            RequestPacket req, CancellationToken token = default)
        {
            string? racerId = string.IsNullOrWhiteSpace(req.RacerId) ? null : req.RacerId.Trim();
            string username = (req.Username ?? string.Empty).Trim();
            string password = req.Password ?? string.Empty;
            string fullName = (req.FullName ?? string.Empty).Trim();
            string? email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            DateTime? birthday = req.Birthday?.Date;
            string? gender = string.IsNullOrWhiteSpace(req.Gender) ? null : req.Gender.Trim();
            string? avatarPath = string.IsNullOrWhiteSpace(req.AvatarPath) ? null : req.AvatarPath.Trim();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string checkSql = "SELECT COUNT(1) FROM dbo.NguoiDung WHERE Username = @Username;";
            await using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username;
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(token).ConfigureAwait(false));
                if (count > 0)
                    return (false, ErrorCodes.UserExists, "Tên đăng nhập đã tồn tại.", null);
            }

            // Dùng ID client gợi ý nếu đúng 6 chữ số và chưa ai dùng, không thì cấp số mới
            racerId = await CapRacerIdAsync(conn, racerId, token).ConfigureAwait(false);

            var (hash, salt) = MatKhau.BamMatKhau(password);

            const string insertSql = @"
                INSERT INTO dbo.NguoiDung (RacerId, Username, PasswordHash, PasswordSalt, FullName, Email, Birthday, Gender, AvatarPath, Coins)
                VALUES (@RacerId, @Username, @PasswordHash, @PasswordSalt, @FullName, @Email, @Birthday, @Gender, @AvatarPath, @Coins);";

            try
            {
                await using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.Add("@RacerId", SqlDbType.NVarChar, 30).Value = racerId;
                insertCmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username;
                insertCmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 128).Value = hash;
                insertCmd.Parameters.Add("@PasswordSalt", SqlDbType.NVarChar, 64).Value = salt;
                insertCmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = fullName;
                insertCmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = (object?)email ?? DBNull.Value;
                insertCmd.Parameters.Add("@Birthday", SqlDbType.Date).Value = (object?)birthday ?? DBNull.Value;
                insertCmd.Parameters.Add("@Gender", SqlDbType.NVarChar, 20).Value = (object?)gender ?? DBNull.Value;
                insertCmd.Parameters.Add("@AvatarPath", SqlDbType.NVarChar, 260).Value = (object?)avatarPath ?? DBNull.Value;
                insertCmd.Parameters.Add("@Coins", SqlDbType.Int).Value = StartingCoins;

                await insertCmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                return (true, ErrorCodes.Ok, "Đăng ký tài khoản thành công.", racerId);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                if (ex.Message.Contains("RacerId", StringComparison.OrdinalIgnoreCase))
                    return (false, ErrorCodes.ServerError, "Không cấp được ID, vui lòng bấm Register lại.", null);

                return (false, ErrorCodes.UserExists, "Tên đăng nhập đã tồn tại.", null);
            }
        }

        public async Task<UserInfoDto?> DangNhapAsync(string username, string password, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string selectSql = @"
                SELECT UserId, RacerId, Username, PasswordHash, PasswordSalt, FullName, Email, Birthday, Gender, AvatarPath, Coins, NgayTao, LanDangNhapCuoi
                FROM dbo.NguoiDung
                WHERE Username = @Username;";

            int userId;
            string dbUsername, storedHash, storedSalt, fullName;
            string? racerId, email, gender, avatarPath;
            DateTime? birthday;
            int coins;
            DateTime createdAt;

            await using (var cmd = new SqlCommand(selectSql, conn))
            {
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username.Trim();
                await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (!await reader.ReadAsync(token).ConfigureAwait(false))
                    return null;

                userId = reader.GetInt32(0);
                racerId = reader.IsDBNull(1) ? null : reader.GetString(1);
                dbUsername = reader.GetString(2);
                storedHash = reader.GetString(3);
                storedSalt = reader.GetString(4);
                fullName = reader.GetString(5);
                email = reader.IsDBNull(6) ? null : reader.GetString(6);
                birthday = reader.IsDBNull(7) ? null : reader.GetDateTime(7);
                gender = reader.IsDBNull(8) ? null : reader.GetString(8);
                avatarPath = reader.IsDBNull(9) ? null : reader.GetString(9);
                coins = reader.GetInt32(10);
                createdAt = reader.GetDateTime(11);
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
                RacerId = racerId,
                Username = dbUsername,
                FullName = fullName,
                Email = email,
                Birthday = birthday,
                Gender = gender,
                AvatarPath = avatarPath,
                Coins = coins,
                NgayTao = createdAt,
                LanDangNhapCuoi = now
            };
        }

        public async Task<UserInfoDto?> LayThongTinTheoIdAsync(int userId, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string sql = @"
                SELECT UserId, RacerId, Username, FullName, Email, Birthday, Gender, AvatarPath, Coins, NgayTao, LanDangNhapCuoi
                FROM dbo.NguoiDung
                WHERE UserId = @UserId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
                return null;

            return MapUserInfo(reader);
        }

        public async Task<UserInfoDto?> LayThongTinAsync(string username, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string sql = @"
                SELECT UserId, RacerId, Username, FullName, Email, Birthday, Gender, AvatarPath, Coins, NgayTao, LanDangNhapCuoi
                FROM dbo.NguoiDung
                WHERE Username = @Username;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username.Trim();

            await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
                return null;

            return MapUserInfo(reader);
        }

        public async Task<bool> UsernameExistsAsync(string username, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.NguoiDung WHERE Username=@Username;", conn);
            cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 20).Value = username.Trim();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)) > 0;
        }

        private static UserInfoDto MapUserInfo(SqlDataReader reader)
        {
            return new UserInfoDto
            {
                UserId = reader.GetInt32(0),
                RacerId = reader.IsDBNull(1) ? null : reader.GetString(1),
                Username = reader.GetString(2),
                FullName = reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                Birthday = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Gender = reader.IsDBNull(6) ? null : reader.GetString(6),
                AvatarPath = reader.IsDBNull(7) ? null : reader.GetString(7),
                Coins = reader.GetInt32(8),
                NgayTao = reader.GetDateTime(9),
                LanDangNhapCuoi = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
            };
        }

        public async Task<(bool Success, string Code, string Message, UserInfoDto? User)> CapNhatAvatarAsync(
            int userId, string? avatarPath, CancellationToken token = default)
        {
            string? cleanAvatar = string.IsNullOrWhiteSpace(avatarPath) ? null : avatarPath.Trim();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            const string updateSql = @"
                UPDATE dbo.NguoiDung
                SET AvatarPath = @AvatarPath
                WHERE UserId = @UserId;";

            await using (var cmd = new SqlCommand(updateSql, conn))
            {
                cmd.Parameters.Add("@AvatarPath", SqlDbType.NVarChar, 260).Value = (object?)cleanAvatar ?? DBNull.Value;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                int affected = await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                if (affected <= 0)
                    return (false, ErrorCodes.AuthFailed, "Không tìm thấy tài khoản.", null);
            }

            var updated = await LayThongTinTheoIdAsync(userId, token).ConfigureAwait(false);
            return (true, ErrorCodes.Ok, "Đã cập nhật avatar.", updated);
        }

        public async Task<(bool Success, string Code, string Message, UserInfoDto? User)> CongTruXuAsync(
     int userId, int coinsDelta, CancellationToken token = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            // 1 lượt: cộng/trừ xu + số dòng bị ảnh hưởng + thông tin người chơi sau khi cập nhật
            await using var cmd = new SqlCommand(@"
        UPDATE dbo.NguoiDung
        SET Coins = Coins + @Delta
        WHERE UserId = @UserId
          AND (Coins + @Delta) >= 0;
        SELECT @@ROWCOUNT;
        SELECT UserId, RacerId, Username, FullName, Email, Birthday, Gender, AvatarPath, Coins, NgayTao, LanDangNhapCuoi
        FROM dbo.NguoiDung
        WHERE UserId = @UserId;", conn);
            cmd.Parameters.Add("@Delta", SqlDbType.Int).Value = coinsDelta;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            int affected = 0;
            UserInfoDto? user = null;

            await using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
            {
                if (await r.ReadAsync(token).ConfigureAwait(false))
                    affected = r.GetInt32(0);

                await r.NextResultAsync(token).ConfigureAwait(false);
                if (await r.ReadAsync(token).ConfigureAwait(false))
                    user = MapUserInfo(r);
            }

            if (affected > 0)
            {
                XoaCacheKho(userId);
                return (true, ErrorCodes.Ok, "Đã cập nhật số xu.", user);
            }

            if (user == null)
                return (false, ErrorCodes.AuthFailed, "Không tìm thấy tài khoản.", null);

            return (false, ErrorCodes.InsufficientCoins, "Số xu hiện tại không đủ.", user);
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

        // =====================================================================
        // KHO ĐỒ / MUA / TRANG BỊ / MAP
        // =====================================================================
        public async Task<InventoryDto> LayKhoAsync(int userId, CancellationToken t = default)
        {
            // Trúng cache: không chạm DB (client hỏi lại kho mỗi vài giây)
            if (_invCache.TryGetValue(userId, out var hit) && DateTime.UtcNow.Ticks - hit.Ticks < InvTtlTicks)
                return hit.Inv;

            int ver = _invVer.GetOrAdd(userId, 0);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            // 1 lần gọi: đảm bảo có xe mặc định + đọc xu/map + đọc đồ
            var inv = new InventoryDto();
            await using var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.VatPham WHERE UserId=@u AND Category='Scooter' AND ItemIndex=0)
                    INSERT dbo.VatPham(UserId,Category,ItemIndex,IsEquipped) VALUES(@u,'Scooter',0,1);
                SELECT Coins, SelectedMap FROM dbo.NguoiDung WHERE UserId=@u;
                SELECT Category, ItemIndex, IsEquipped FROM dbo.VatPham WHERE UserId=@u;", conn);
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;

            await using var r = await cmd.ExecuteReaderAsync(t).ConfigureAwait(false);
            if (await r.ReadAsync(t).ConfigureAwait(false))
            {
                inv.Coins = r.GetInt32(0);
                inv.SelectedMap = r.GetString(1);
            }
            await r.NextResultAsync(t).ConfigureAwait(false);
            while (await r.ReadAsync(t).ConfigureAwait(false))
            {
                inv.Items.Add(new ItemDto
                {
                    Category = r.GetString(0),
                    ItemIndex = r.GetInt32(1),
                    IsEquipped = r.GetBoolean(2)
                });
            }

            // Chỉ lưu cache nếu trong lúc đọc không có ai ghi
            if (_invVer.GetOrAdd(userId, 0) == ver)
                _invCache[userId] = (inv, DateTime.UtcNow.Ticks);

            return inv;
        }

        public async Task<(bool Ok, string Code)> MuaAsync(
            int userId, string cat, int idx, int price, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(@"
                DECLARE @coins INT = (SELECT Coins FROM dbo.NguoiDung WITH (UPDLOCK, ROWLOCK) WHERE UserId=@u);
                IF @coins IS NULL SELECT 3;
                ELSE IF EXISTS (SELECT 1 FROM dbo.VatPham WHERE UserId=@u AND Category=@c AND ItemIndex=@i) SELECT 1;
                ELSE IF @coins < @p SELECT 2;
                ELSE BEGIN
                    UPDATE dbo.NguoiDung SET Coins = Coins - @p WHERE UserId=@u;
                    INSERT dbo.VatPham(UserId,Category,ItemIndex,IsEquipped) VALUES(@u,@c,@i,0);
                    SELECT 0;
                END", conn, tx);
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@c", SqlDbType.NVarChar, 30).Value = cat;
            cmd.Parameters.Add("@i", SqlDbType.Int).Value = idx;
            cmd.Parameters.Add("@p", SqlDbType.Int).Value = price;

            int rs = Convert.ToInt32(await cmd.ExecuteScalarAsync(t).ConfigureAwait(false));
            await tx.CommitAsync(t).ConfigureAwait(false);

            if (rs == 0) XoaCacheKho(userId);

            return rs switch
            {
                0 => (true, ErrorCodes.Ok),
                2 => (false, ErrorCodes.InsufficientCoins),
                _ => (false, ErrorCodes.InvalidInput)
            };
        }

        public async Task<bool> TrangBiAsync(int userId, string cat, int idx, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM dbo.VatPham WHERE UserId=@u AND Category=@c AND ItemIndex=@i)
                BEGIN
                    UPDATE dbo.VatPham
                    SET IsEquipped = CASE WHEN ItemIndex=@i THEN 1 ELSE 0 END
                    WHERE UserId=@u AND Category=@c;
                    SELECT 1;
                END
                ELSE SELECT 0;", conn);
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@c", SqlDbType.NVarChar, 30).Value = cat;
            cmd.Parameters.Add("@i", SqlDbType.Int).Value = idx;

            bool ok = Convert.ToInt32(await cmd.ExecuteScalarAsync(t).ConfigureAwait(false)) == 1;
            if (ok) XoaCacheKho(userId);
            return ok;
        }

        public async Task DatMapAsync(int userId, string map, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(
                "UPDATE dbo.NguoiDung SET SelectedMap=@m WHERE UserId=@u;", conn);
            cmd.Parameters.Add("@m", SqlDbType.NVarChar, 50).Value = map;
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;
            await cmd.ExecuteNonQueryAsync(t).ConfigureAwait(false);

            XoaCacheKho(userId);
        }

        // =====================================================================
        // AVATAR LƯU TRONG DB (ảnh đã thu nhỏ, <= 40KB)
        // =====================================================================
        public async Task<bool> CapNhatAvatarDuLieuAsync(int userId, byte[] data, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(
                "UPDATE dbo.NguoiDung SET AvatarData=@d WHERE UserId=@u;", conn);
            cmd.Parameters.Add("@d", SqlDbType.VarBinary, -1).Value = data;
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;
            return await cmd.ExecuteNonQueryAsync(t).ConfigureAwait(false) > 0;
        }

        public async Task<byte[]?> LayAvatarDuLieuAsync(int userId, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(
                "SELECT AvatarData FROM dbo.NguoiDung WHERE UserId=@u;", conn);
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;

            object? o = await cmd.ExecuteScalarAsync(t).ConfigureAwait(false);
            return o == null || o == DBNull.Value ? null : (byte[])o;
        }

        // =====================================================================
        // ĐÁNH GIÁ + BẢNG XẾP HẠNG
        // =====================================================================
        public async Task<bool> GuiDanhGiaAsync(int userId, int stars, string? comment, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM dbo.DanhGia WHERE UserId=@u) SELECT 1;
                ELSE BEGIN
                    INSERT dbo.DanhGia(UserId,Stars,Comment) VALUES(@u,@s,@c);
                    SELECT 0;
                END", conn);
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@s", SqlDbType.TinyInt).Value = (byte)stars;
            cmd.Parameters.Add("@c", SqlDbType.NVarChar, 300).Value = (object?)comment ?? DBNull.Value;

            try { return Convert.ToInt32(await cmd.ExecuteScalarAsync(t).ConfigureAwait(false)) == 0; }
            catch (SqlException ex) when (ex.Number is 2627 or 2601) { return false; }
        }

        public async Task<ReviewSummaryDto> LayTomTatDanhGiaAsync(int userId, CancellationToken t = default)
        {
            var sum = new ReviewSummaryDto();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(@"
                SELECT Stars, COUNT(*) FROM dbo.DanhGia GROUP BY Stars;
                SELECT Stars FROM dbo.DanhGia WHERE UserId=@u;", conn);
            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;

            await using var r = await cmd.ExecuteReaderAsync(t).ConfigureAwait(false);
            while (await r.ReadAsync(t).ConfigureAwait(false))
            {
                int star = Convert.ToInt32(r.GetValue(0));
                if (star is >= 1 and <= 5) sum.Counts[star - 1] = r.GetInt32(1);
            }
            await r.NextResultAsync(t).ConfigureAwait(false);
            if (await r.ReadAsync(t).ConfigureAwait(false))
            {
                sum.HasReviewed = true;
                sum.MyStars = Convert.ToInt32(r.GetValue(0));
            }
            return sum;
        }

        public async Task<List<LeaderboardEntryDto>> LayBangXepHangAsync(int top, CancellationToken t = default)
        {
            var list = new List<LeaderboardEntryDto>();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(@"
                SELECT TOP (@n)
                       ROW_NUMBER() OVER (ORDER BY Coins DESC, UserId ASC) AS RankNo,
                       RacerId, Username, FullName, Coins
                FROM dbo.NguoiDung
                ORDER BY Coins DESC, UserId ASC;", conn);
            cmd.Parameters.Add("@n", SqlDbType.Int).Value = top;

            await using var r = await cmd.ExecuteReaderAsync(t).ConfigureAwait(false);
            while (await r.ReadAsync(t).ConfigureAwait(false))
            {
                list.Add(new LeaderboardEntryDto
                {
                    Rank = (int)r.GetInt64(0),
                    RacerId = r.IsDBNull(1) ? null : r.GetString(1),
                    Username = r.GetString(2),
                    FullName = r.GetString(3),
                    Coins = r.GetInt32(4)
                });
            }
            return list;
        }

        // =====================================================================
        // RACER ID DUY NHẤT + TÌM NGƯỜI CHƠI (mời bạn)
        // =====================================================================
        private static async Task<string> CapRacerIdAsync(SqlConnection conn, string? wanted, CancellationToken t)
        {
            async Task<bool> TonTai(string id)
            {
                await using var c = new SqlCommand("SELECT COUNT(1) FROM dbo.NguoiDung WHERE RacerId=@r;", conn);
                c.Parameters.Add("@r", SqlDbType.NVarChar, 30).Value = id;
                return Convert.ToInt32(await c.ExecuteScalarAsync(t).ConfigureAwait(false)) > 0;
            }

            if (wanted != null && wanted.Length == 6 && wanted.All(char.IsDigit) && !await TonTai(wanted).ConfigureAwait(false))
                return wanted;

            for (int i = 0; i < 50; i++)
            {
                string id = Random.Shared.Next(1, 1_000_000).ToString("D6");
                if (!await TonTai(id).ConfigureAwait(false)) return id;
            }
            return Guid.NewGuid().ToString("N")[..10];
        }

        public async Task<UserInfoDto?> TimNguoiChoiAsync(string key, CancellationToken t = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(t).ConfigureAwait(false);

            await using var cmd = new SqlCommand(@"
                SELECT TOP 1 UserId, RacerId, Username, FullName, Email, Birthday, Gender, AvatarPath, Coins, NgayTao, LanDangNhapCuoi
                FROM dbo.NguoiDung
                WHERE RacerId = @k OR Username = @k;", conn);
            cmd.Parameters.Add("@k", SqlDbType.NVarChar, 30).Value = key;

            await using var r = await cmd.ExecuteReaderAsync(t).ConfigureAwait(false);
            return await r.ReadAsync(t).ConfigureAwait(false) ? MapUserInfo(r) : null;
        }
    }
}
