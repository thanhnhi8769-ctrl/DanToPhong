using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DanToPhong.Server
{
    // Tự tạo database + bảng khi Server khởi động. Chạy lại nhiều lần vẫn an toàn (chỉ tạo cái còn thiếu).
    public static class DatabaseInitializer
    {
        public static async Task EnsureAsync(string connectionString, CancellationToken token = default)
        {
            var target = new SqlConnectionStringBuilder(connectionString);
            string dbName = target.InitialCatalog;
            if (string.IsNullOrWhiteSpace(dbName)) return;

            // 1) Tạo database (kết nối vào master)
            var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await using (var conn = new SqlConnection(master.ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);
                await using var cmd = new SqlCommand(
                    @"IF DB_ID(@db) IS NULL
                      BEGIN
                          DECLARE @sql NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@db) + N';';
                          EXEC(@sql);
                      END", conn);
                cmd.Parameters.AddWithValue("@db", dbName);
                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            // 2) Tạo bảng / cột / index còn thiếu
            await using var db = new SqlConnection(connectionString);
            await db.OpenAsync(token).ConfigureAwait(false);

            foreach (string sql in Statements)
            {
                try
                {
                    await using var cmd = new SqlCommand(sql, db);
                    await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }
                catch (SqlException ex)
                {
                    // Ví dụ: index unique bị từ chối vì dữ liệu cũ có ID trùng. Không chặn Server khởi động.
                    Console.WriteLine($"[DB] Setup warning: {ex.Message}");
                }
            }

            await using (var verify = new SqlCommand(@"
                IF OBJECT_ID('dbo.AuthTokens') IS NULL OR
                   OBJECT_ID('dbo.VerificationCodes') IS NULL OR
                      OBJECT_ID('dbo.RequestIdempotency') IS NULL OR
                      OBJECT_ID('dbo.LoginRateLimits') IS NULL OR
                      OBJECT_ID('dbo.GameMatchRewards') IS NULL
                BEGIN
                    THROW 51000, 'Required security-state tables are missing.', 1;
                END;", db))
            {
                await verify.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            Console.WriteLine("[DB] Database setup OK.");
        }

        private static readonly string[] Statements =
        {
            @"IF OBJECT_ID('dbo.NguoiDung') IS NULL
              CREATE TABLE dbo.NguoiDung (
                  UserId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NguoiDung PRIMARY KEY,
                  RacerId         NVARCHAR(30)   NULL,
                  Username        NVARCHAR(20)   NOT NULL CONSTRAINT UQ_NguoiDung_Username UNIQUE,
                  PasswordHash    NVARCHAR(128)  NOT NULL,
                  PasswordSalt    NVARCHAR(64)   NOT NULL,
                  FullName        NVARCHAR(100)  NOT NULL,
                  Email           NVARCHAR(100)  NULL,
                  Birthday        DATE           NULL,
                  Gender          NVARCHAR(20)   NULL,
                  AvatarPath      NVARCHAR(260)  NULL,
                  AvatarData      VARBINARY(MAX) NULL,
                  Coins           INT            NOT NULL CONSTRAINT DF_NguoiDung_Coins DEFAULT 100,
                  SelectedMap     NVARCHAR(50)   NOT NULL CONSTRAINT DF_NguoiDung_Map DEFAULT 'game',
                  NgayTao         DATETIME2      NOT NULL CONSTRAINT DF_NguoiDung_NgayTao DEFAULT SYSDATETIME(),
                  LanDangNhapCuoi DATETIME2      NULL
              );",

            @"IF OBJECT_ID('dbo.GameMatchRewards') IS NULL
              CREATE TABLE dbo.GameMatchRewards (
                  MatchId UNIQUEIDENTIFIER NOT NULL,
                  UserId INT NOT NULL,
                  CoinsAwarded INT NOT NULL,
                  AwardedAt DATETIME2 NOT NULL CONSTRAINT DF_GameMatchRewards_AwardedAt DEFAULT SYSUTCDATETIME(),
                  CONSTRAINT PK_GameMatchRewards PRIMARY KEY (MatchId, UserId),
                  CONSTRAINT FK_GameMatchRewards_NguoiDung FOREIGN KEY (UserId) REFERENCES dbo.NguoiDung(UserId)
              );",

            // Bảng đã có từ trước (script cũ của nhóm): bổ sung cột mới
            @"IF COL_LENGTH('dbo.NguoiDung', 'AvatarData') IS NULL
                  ALTER TABLE dbo.NguoiDung ADD AvatarData VARBINARY(MAX) NULL;",

            @"IF COL_LENGTH('dbo.NguoiDung', 'SelectedMap') IS NULL
                  ALTER TABLE dbo.NguoiDung ADD SelectedMap NVARCHAR(50) NOT NULL
                      CONSTRAINT DF_NguoiDung_Map DEFAULT 'game';",

            @"IF OBJECT_ID('dbo.VatPham') IS NULL
              CREATE TABLE dbo.VatPham (
                  UserId     INT          NOT NULL,
                  Category   NVARCHAR(30) NOT NULL,
                  ItemIndex  INT          NOT NULL,
                  IsEquipped BIT          NOT NULL CONSTRAINT DF_VatPham_Eq DEFAULT 0,
                  CONSTRAINT PK_VatPham PRIMARY KEY (UserId, Category, ItemIndex),
                  CONSTRAINT FK_VatPham_ND FOREIGN KEY (UserId) REFERENCES dbo.NguoiDung(UserId)
              );",

            @"IF OBJECT_ID('dbo.DanhGia') IS NULL
              CREATE TABLE dbo.DanhGia (
                  UserId    INT           NOT NULL PRIMARY KEY,
                  Stars     TINYINT       NOT NULL CHECK (Stars BETWEEN 1 AND 5),
                  Comment   NVARCHAR(300) NULL,
                  CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_DanhGia_Time DEFAULT SYSDATETIME(),
                  CONSTRAINT FK_DanhGia_ND FOREIGN KEY (UserId) REFERENCES dbo.NguoiDung(UserId)
              );",

            @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_NguoiDung_RacerId')
                  CREATE UNIQUE INDEX UX_NguoiDung_RacerId ON dbo.NguoiDung(RacerId) WHERE RacerId IS NOT NULL;",

            @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NguoiDung_Coins')
                  CREATE INDEX IX_NguoiDung_Coins ON dbo.NguoiDung(Coins DESC);",

            @"IF OBJECT_ID('dbo.AuthTokens') IS NULL
              CREATE TABLE dbo.AuthTokens (
                  TokenHash CHAR(64) NOT NULL CONSTRAINT PK_AuthTokens PRIMARY KEY,
                  UserId INT NOT NULL,
                  Username NVARCHAR(20) NOT NULL,
                  ExpiresAt DATETIME2 NOT NULL,
                  RevokedAt DATETIME2 NULL,
                  CONSTRAINT FK_AuthTokens_NguoiDung FOREIGN KEY (UserId) REFERENCES dbo.NguoiDung(UserId)
              );
              IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AuthTokens_UserId')
                  CREATE INDEX IX_AuthTokens_UserId ON dbo.AuthTokens(UserId, RevokedAt, ExpiresAt);",

            @"IF OBJECT_ID('dbo.VerificationCodes') IS NULL
              CREATE TABLE dbo.VerificationCodes (
                  KeyHash CHAR(64) NOT NULL CONSTRAINT PK_VerificationCodes PRIMARY KEY,
                  CodeHash BINARY(32) NULL,
                  CodeSalt BINARY(16) NULL,
                  ExpiresAt DATETIME2 NULL,
                  Attempts INT NOT NULL,
                  CooldownUntil DATETIME2 NOT NULL
              );
              IF COL_LENGTH('dbo.VerificationCodes', 'CodeSalt') IS NULL
                  ALTER TABLE dbo.VerificationCodes ADD CodeSalt BINARY(16) NULL;",

            @"IF OBJECT_ID('dbo.RequestIdempotency') IS NULL
              CREATE TABLE dbo.RequestIdempotency (
                  UserId INT NOT NULL,
                  Command VARCHAR(20) NOT NULL,
                  RequestId UNIQUEIDENTIFIER NOT NULL,
                  Fingerprint CHAR(64) NOT NULL,
                  ResponseJson NVARCHAR(MAX) NULL,
                  CreatedAt DATETIME2 NOT NULL,
                  CONSTRAINT PK_RequestIdempotency PRIMARY KEY(UserId, Command, RequestId),
                  CONSTRAINT FK_RequestIdempotency_NguoiDung FOREIGN KEY(UserId) REFERENCES dbo.NguoiDung(UserId)
              );
              IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RequestIdempotency_CreatedAt')
                  CREATE INDEX IX_RequestIdempotency_CreatedAt ON dbo.RequestIdempotency(CreatedAt);",
            @"IF OBJECT_ID('dbo.LoginRateLimits') IS NULL
              CREATE TABLE dbo.LoginRateLimits (
                  KeyHash CHAR(64) NOT NULL CONSTRAINT PK_LoginRateLimits PRIMARY KEY,
                  WindowStartedAt DATETIME2 NOT NULL,
                  Attempts INT NOT NULL,
                  LockedUntil DATETIME2 NULL,
                  UpdatedAt DATETIME2 NOT NULL
              );
              IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LoginRateLimits_UpdatedAt')
                  CREATE INDEX IX_LoginRateLimits_UpdatedAt ON dbo.LoginRateLimits(UpdatedAt);"
        };
    }
}
