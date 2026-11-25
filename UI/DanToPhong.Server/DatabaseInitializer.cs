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
                  CREATE INDEX IX_NguoiDung_Coins ON dbo.NguoiDung(Coins DESC);"
        };
    }
}