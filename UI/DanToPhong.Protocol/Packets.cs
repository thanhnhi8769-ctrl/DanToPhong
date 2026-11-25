using System;
using System.Collections.Generic;

namespace DanToPhong.Protocol
{
    public sealed class RequestPacket
    {
        public string Command { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? RacerId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Gender { get; set; }
        public string? AvatarPath { get; set; }
        public int? CoinsDelta { get; set; }
        public string? Mode { get; set; }
        public string? RoomId { get; set; }

        // MỚI
        public string? Token { get; set; }
        public string? Category { get; set; }
        public int? ItemIndex { get; set; }
        public int? Price { get; set; }
        public string? MapName { get; set; }
        public string? AvatarBase64 { get; set; }
        public int? Stars { get; set; }
        public string? Comment { get; set; }
        public int? PlayerCount { get; set; }
        public int? MaxPlayers { get; set; }
        public bool? IsRacing { get; set; }
        public string? TargetId { get; set; }   // RacerId hoặc username người được mời
    }

    public sealed class ResponsePacket
    {
        public bool Success { get; set; }
        public string Code { get; set; } = ErrorCodes.Ok;
        public string Message { get; set; } = string.Empty;
        public string? Mode { get; set; }
        public string? RoomId { get; set; }
        public UserInfoDto? User { get; set; }

        // MỚI
        public string? Token { get; set; }
        public InventoryDto? Inventory { get; set; }
        public string? AvatarBase64 { get; set; }
        public ReviewSummaryDto? ReviewSummary { get; set; }
        public List<LeaderboardEntryDto>? Leaderboard { get; set; }
        public List<RoomDto>? Rooms { get; set; }
        public List<InviteDto>? Invites { get; set; }
    }

    public sealed class UserInfoDto
    {
        public int UserId { get; set; }
        public string? RacerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Gender { get; set; }
        public string? AvatarPath { get; set; }
        public int Coins { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
    }

    public sealed class ItemDto
    {
        public string Category { get; set; } = "";
        public int ItemIndex { get; set; }
        public bool IsEquipped { get; set; }
    }

    public sealed class InventoryDto
    {
        public int Coins { get; set; }
        public string SelectedMap { get; set; } = "game";
        public List<ItemDto> Items { get; set; } = new();
    }

    public sealed class ReviewSummaryDto
    {
        public int[] Counts { get; set; } = new int[5];   // số lượt 1..5 sao
        public bool HasReviewed { get; set; }
        public int MyStars { get; set; }
    }

    public sealed class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string? RacerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Coins { get; set; }
    }

    public sealed class RoomDto
    {
        public string Code { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsRacing { get; set; }
    }

    public sealed class InviteDto
    {
        public string RoomCode { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}