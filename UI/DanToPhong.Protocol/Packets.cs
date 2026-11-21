using System;

namespace DanToPhong.Protocol
{
    public sealed class RequestPacket
    {
        public string Command { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
    }

    public sealed class ResponsePacket
    {
        public bool Success { get; set; }
        public string Code { get; set; } = ErrorCodes.Ok;
        public string Message { get; set; } = string.Empty;
        public UserInfoDto? User { get; set; }
    }

    public sealed class UserInfoDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public int Coins { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
    }
}