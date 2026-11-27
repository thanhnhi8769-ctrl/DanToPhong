using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DanToPhong.Protocol;
using Microsoft.Data.SqlClient;

namespace DanToPhong.Server
{
    // Persistent security/session state. Raw login tokens and OTPs are never stored.
    internal sealed class ServerStateRepository
    {
        private readonly string _connectionString;

        internal ServerStateRepository(string connectionString) => _connectionString = connectionString;

        private static string HashKey(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        internal async Task PruneExpiredStateAsync(CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                DELETE dbo.AuthTokens
                WHERE ExpiresAt < DATEADD(day, -7, SYSUTCDATETIME())
                   OR RevokedAt < DATEADD(day, -7, SYSUTCDATETIME());
                DELETE dbo.VerificationCodes
                WHERE CooldownUntil < DATEADD(day, -1, SYSUTCDATETIME())
                  AND (ExpiresAt IS NULL OR ExpiresAt < SYSUTCDATETIME());
                DELETE dbo.RequestIdempotency
                WHERE ResponseJson IS NOT NULL AND CreatedAt < DATEADD(day, -30, SYSUTCDATETIME());
                DELETE TOP (500) dbo.LoginRateLimits
                WHERE UpdatedAt < DATEADD(day, -2, SYSUTCDATETIME());", conn);
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        internal async Task<bool> TryBeginLoginAttemptAsync(
            string key,
            int maxAttempts,
            TimeSpan window,
            TimeSpan lockDuration,
            CancellationToken token)
        {
            string keyHash = HashKey(key);
            DateTime now = DateTime.UtcNow;
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            DateTime? windowStartedAt = null;
            DateTime? lockedUntil = null;
            int attempts = 0;
            bool rowExists = false;
            await using (var read = new SqlCommand(@"
                SELECT WindowStartedAt, Attempts, LockedUntil
                FROM dbo.LoginRateLimits WITH (UPDLOCK, HOLDLOCK)
                WHERE KeyHash=@keyHash;", conn, tx))
            {
                read.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
                await using var reader = await read.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    rowExists = true;
                    windowStartedAt = reader.GetDateTime(0);
                    attempts = reader.GetInt32(1);
                    lockedUntil = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                }
            }

            if (lockedUntil > now)
            {
                await tx.CommitAsync(token).ConfigureAwait(false);
                return false;
            }

            bool reset = !windowStartedAt.HasValue ||
                         lockedUntil.HasValue ||
                         windowStartedAt.Value <= now - window;
            if (reset)
            {
                windowStartedAt = now;
                attempts = 0;
                lockedUntil = null;
            }

            if (attempts >= maxAttempts)
            {
                DateTime lockUntil = now + lockDuration;
                await using var lockCmd = new SqlCommand(@"
                    UPDATE dbo.LoginRateLimits SET LockedUntil=@lockedUntil, UpdatedAt=@now
                    WHERE KeyHash=@keyHash;", conn, tx);
                lockCmd.Parameters.Add("@lockedUntil", SqlDbType.DateTime2).Value = lockUntil;
                lockCmd.Parameters.Add("@now", SqlDbType.DateTime2).Value = now;
                lockCmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
                await lockCmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                await tx.CommitAsync(token).ConfigureAwait(false);
                return false;
            }

            attempts++;
            if (!rowExists)
            {
                await using var insert = new SqlCommand(@"
                    INSERT dbo.LoginRateLimits(KeyHash, WindowStartedAt, Attempts, LockedUntil, UpdatedAt)
                    VALUES(@keyHash, @windowStartedAt, @attempts, NULL, @now);", conn, tx);
                insert.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
                insert.Parameters.Add("@windowStartedAt", SqlDbType.DateTime2).Value = now;
                insert.Parameters.Add("@attempts", SqlDbType.Int).Value = attempts;
                insert.Parameters.Add("@now", SqlDbType.DateTime2).Value = now;
                await insert.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            else
            {
                DateTime startTime = windowStartedAt ?? now;
                await using var update = new SqlCommand(@"
                    UPDATE dbo.LoginRateLimits SET WindowStartedAt=@windowStartedAt, Attempts=@attempts,
                        LockedUntil=NULL, UpdatedAt=@now
                    WHERE KeyHash=@keyHash;", conn, tx);
                update.Parameters.Add("@windowStartedAt", SqlDbType.DateTime2).Value = startTime;
                update.Parameters.Add("@attempts", SqlDbType.Int).Value = attempts;
                update.Parameters.Add("@now", SqlDbType.DateTime2).Value = now;
                update.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
                await update.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            await tx.CommitAsync(token).ConfigureAwait(false);
            return true;
        }

        internal async Task RecordFailedLoginAsync(
            string key,
            int maxAttempts,
            TimeSpan lockDuration,
            CancellationToken token)
        {
            string keyHash = HashKey(key);
            DateTime now = DateTime.UtcNow;
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                UPDATE dbo.LoginRateLimits
                SET LockedUntil=CASE WHEN Attempts >= @maxAttempts THEN DATEADD(second, @lockSeconds, @now) ELSE LockedUntil END,
                    UpdatedAt=@now
                WHERE KeyHash=@keyHash;", conn);
            cmd.Parameters.Add("@maxAttempts", SqlDbType.Int).Value = maxAttempts;
            cmd.Parameters.Add("@lockSeconds", SqlDbType.Int).Value = checked((int)lockDuration.TotalSeconds);
            cmd.Parameters.Add("@now", SqlDbType.DateTime2).Value = now;
            cmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
            if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                throw new InvalidOperationException("Login limiter state disappeared during authentication.");
        }

        internal async Task ReleaseLoginAttemptAsync(string key, CancellationToken token)
        {
            string keyHash = HashKey(key);
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                UPDATE dbo.LoginRateLimits SET Attempts=CASE WHEN Attempts > 0 THEN Attempts - 1 ELSE 0 END,
                    UpdatedAt=SYSUTCDATETIME()
                WHERE KeyHash=@keyHash;", conn);
            cmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        internal async Task ClearLoginAttemptsAsync(string key, CancellationToken token)
        {
            string keyHash = HashKey(key);
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand("DELETE dbo.LoginRateLimits WHERE KeyHash=@keyHash;", conn);
            cmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        internal async Task SaveTokenAsync(string tokenValue, int userId, string username, DateTime expiresAt, CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                INSERT dbo.AuthTokens(TokenHash, UserId, Username, ExpiresAt, RevokedAt)
                VALUES(@hash, @userId, @username, @expiresAt, NULL);", conn);
            cmd.Parameters.Add("@hash", SqlDbType.Char, 64).Value = HashKey(tokenValue);
            cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@username", SqlDbType.NVarChar, 20).Value = username;
            cmd.Parameters.Add("@expiresAt", SqlDbType.DateTime2).Value = expiresAt;
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        internal async Task<(int UserId, string Username)?> ValidateTokenAsync(string tokenValue, CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                SELECT UserId, Username FROM dbo.AuthTokens
                WHERE TokenHash=@hash AND RevokedAt IS NULL AND ExpiresAt > SYSUTCDATETIME();", conn);
            cmd.Parameters.Add("@hash", SqlDbType.Char, 64).Value = HashKey(tokenValue);
            await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
            return await reader.ReadAsync(token).ConfigureAwait(false)
                ? (reader.GetInt32(0), reader.GetString(1))
                : null;
        }

        internal async Task RevokeUserTokensAsync(int userId, CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                UPDATE dbo.AuthTokens SET RevokedAt=SYSUTCDATETIME()
                WHERE UserId=@userId AND RevokedAt IS NULL;", conn);
            cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        internal async Task<bool> TryReserveVerificationAsync(string key, DateTime cooldownUntil, CancellationToken token)
        {
            string keyHash = HashKey(key);
            DateTime now = DateTime.UtcNow;
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            await using (var prune = new SqlCommand(@"
                DELETE TOP (500) dbo.VerificationCodes
                WHERE CooldownUntil < DATEADD(hour, -1, SYSUTCDATETIME())
                  AND (ExpiresAt IS NULL OR ExpiresAt < SYSUTCDATETIME());", conn, tx))
            {
                await prune.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            await using var cmd = new SqlCommand(@"
                SELECT CooldownUntil FROM dbo.VerificationCodes WITH (UPDLOCK, HOLDLOCK)
                WHERE KeyHash=@keyHash;", conn, tx);
            cmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
            object? value = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
            if (value is DateTime activeUntil && activeUntil > now)
            {
                await tx.CommitAsync(token).ConfigureAwait(false);
                return false;
            }

            if (value == null || value == DBNull.Value)
            {
                await using var insert = new SqlCommand(@"
                    INSERT dbo.VerificationCodes(KeyHash, CodeHash, CodeSalt, ExpiresAt, Attempts, CooldownUntil)
                    VALUES(@keyHash, NULL, NULL, NULL, 0, @cooldownUntil);", conn, tx);
                insert.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
                insert.Parameters.Add("@cooldownUntil", SqlDbType.DateTime2).Value = cooldownUntil;
                await insert.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            else
            {
                await using var update = new SqlCommand(@"
                    UPDATE dbo.VerificationCodes SET CodeHash=NULL, CodeSalt=NULL, ExpiresAt=NULL, Attempts=0, CooldownUntil=@cooldownUntil
                    WHERE KeyHash=@keyHash;", conn, tx);
                update.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = keyHash;
                update.Parameters.Add("@cooldownUntil", SqlDbType.DateTime2).Value = cooldownUntil;
                await update.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            await tx.CommitAsync(token).ConfigureAwait(false);
            return true;
        }

        internal async Task SaveVerificationCodeAsync(string key, string code, DateTime expiresAt, CancellationToken token)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] codeHash = Rfc2898DeriveBytes.Pbkdf2(code, salt, 120_000, HashAlgorithmName.SHA256, 32);
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                UPDATE dbo.VerificationCodes SET CodeHash=@codeHash, CodeSalt=@salt, ExpiresAt=@expiresAt, Attempts=0
                WHERE KeyHash=@keyHash AND CooldownUntil > SYSUTCDATETIME();", conn);
            cmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = HashKey(key);
            cmd.Parameters.Add("@codeHash", SqlDbType.Binary, 32).Value = codeHash;
            cmd.Parameters.Add("@salt", SqlDbType.Binary, 16).Value = salt;
            cmd.Parameters.Add("@expiresAt", SqlDbType.DateTime2).Value = expiresAt;
            if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                throw new InvalidOperationException("Verification reservation expired before the code was stored.");
        }

        internal async Task CancelVerificationAsync(string key, CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new SqlCommand(@"
                UPDATE dbo.VerificationCodes
                SET CodeHash=NULL, CodeSalt=NULL, ExpiresAt=NULL, Attempts=0,
                    CooldownUntil=SYSUTCDATETIME()
                WHERE KeyHash=@keyHash;", conn);
            cmd.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = HashKey(key);
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        // Validate and consume under a SQL row lock so an OTP is single-use across restarts/instances.
        internal async Task<bool> VerifyAndConsumeCodeAsync(string key, string? code, CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            byte[]? expected = null;
            byte[]? salt = null;
            DateTime expiresAt = DateTime.MinValue;
            int attempts = 5;
            await using (var read = new SqlCommand(@"
                SELECT CodeHash, CodeSalt, ExpiresAt, Attempts FROM dbo.VerificationCodes WITH (UPDLOCK, HOLDLOCK)
                WHERE KeyHash=@keyHash;", conn, tx))
            {
                read.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = HashKey(key);
                await using var reader = await read.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    expected = reader.IsDBNull(0) ? null : (byte[])reader[0];
                    salt = reader.IsDBNull(1) ? null : (byte[])reader[1];
                    expiresAt = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);
                    attempts = reader.GetInt32(3);
                }
            }

            bool canCheck = expected != null && salt != null && expiresAt > DateTime.UtcNow && attempts < 5;
            bool hasValidFormat = code is { Length: 6 } && code.All(static c => c is >= '0' and <= '9');
            string candidate = hasValidFormat ? code! : "invalid-otp";
            byte[] supplied = canCheck
                ? Rfc2898DeriveBytes.Pbkdf2(candidate, salt!, 120_000, HashAlgorithmName.SHA256, 32)
                : Array.Empty<byte>();
            bool valid = canCheck && CryptographicOperations.FixedTimeEquals(expected!, supplied);
            if (canCheck)
            {
                if (valid)
                {
                    await using var consume = new SqlCommand(@"
                        UPDATE dbo.VerificationCodes SET CodeHash=NULL, CodeSalt=NULL, ExpiresAt=NULL, Attempts=0
                        WHERE KeyHash=@keyHash;", conn, tx);
                    consume.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = HashKey(key);
                    await consume.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }
                else
                {
                    await using var increment = new SqlCommand(@"
                        UPDATE dbo.VerificationCodes SET Attempts=Attempts+1 WHERE KeyHash=@keyHash;", conn, tx);
                    increment.Parameters.Add("@keyHash", SqlDbType.Char, 64).Value = HashKey(key);
                    await increment.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }
            }
            await tx.CommitAsync(token).ConfigureAwait(false);
            return valid;
        }

        internal async Task<ResponsePacket> ExecuteIdempotentAsync(
            int userId,
            string command,
            Guid requestId,
            string fingerprint,
            Func<SqlConnection, SqlTransaction, Task<ResponsePacket>> execute,
            CancellationToken token)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            string? storedFingerprint = null;
            string? responseJson = null;
            await using (var read = new SqlCommand(@"
                SELECT Fingerprint, ResponseJson FROM dbo.RequestIdempotency WITH (UPDLOCK, HOLDLOCK)
                WHERE UserId=@userId AND Command=@command AND RequestId=@requestId;", conn, tx))
            {
                read.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                read.Parameters.Add("@command", SqlDbType.VarChar, 20).Value = command;
                read.Parameters.Add("@requestId", SqlDbType.UniqueIdentifier).Value = requestId;
                await using var reader = await read.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    storedFingerprint = reader.GetString(0);
                    responseJson = reader.IsDBNull(1) ? null : reader.GetString(1);
                }
            }

            if (storedFingerprint != null)
            {
                await tx.CommitAsync(token).ConfigureAwait(false);
                if (!string.Equals(storedFingerprint, fingerprint, StringComparison.Ordinal))
                    return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Request id was already used for a different transaction." };
                if (responseJson == null)
                    return new ResponsePacket { Success = false, Code = ErrorCodes.ServerError, Message = "Transaction record is incomplete; refresh the inventory before retrying." };
                return JsonSerializer.Deserialize<ResponsePacket>(responseJson)
                    ?? new ResponsePacket { Success = false, Code = ErrorCodes.ServerError, Message = "Stored transaction response is invalid." };
            }

            // The ledger row, currency/item mutation, and response commit together.
            // A crash rolls all three back, so the same RequestId can safely be retried.
            ResponsePacket response = await execute(conn, tx).ConfigureAwait(false);
            await using (var insert = new SqlCommand(@"
                INSERT dbo.RequestIdempotency(UserId, Command, RequestId, Fingerprint, ResponseJson, CreatedAt)
                VALUES(@userId, @command, @requestId, @fingerprint, @response, SYSUTCDATETIME());", conn, tx))
            {
                insert.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                insert.Parameters.Add("@command", SqlDbType.VarChar, 20).Value = command;
                insert.Parameters.Add("@requestId", SqlDbType.UniqueIdentifier).Value = requestId;
                insert.Parameters.Add("@fingerprint", SqlDbType.Char, 64).Value = fingerprint;
                insert.Parameters.Add("@response", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(response);
                await insert.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            await tx.CommitAsync(token).ConfigureAwait(false);
            return response;
        }
    }
}
