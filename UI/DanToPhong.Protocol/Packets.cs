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
        public string? VerificationCode { get; set; }
        public string? RequestId { get; set; }
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
        public string? ChatMessage { get; set; }
        public long? AfterMessageId { get; set; }
        public float? PositionX { get; set; }
        public float? PositionY { get; set; }
        public float? PositionZ { get; set; }
        public float? RotationX { get; set; }
        public float? RotationY { get; set; }
        public float? RotationZ { get; set; }
        public float? RotationW { get; set; }
        public float? VelocityX { get; set; }
        public float? VelocityY { get; set; }
        public float? VelocityZ { get; set; }
        public float? Throttle { get; set; }
        public float? Steer { get; set; }
        public bool? Brake { get; set; }
        public int? VehicleIndex { get; set; }
        public int? ClientTick { get; set; }
        public long? ClientTimestampMs { get; set; }
        public string? GameEventType { get; set; }
        public int? CheckpointIndex { get; set; }
        public int? TotalCheckpoints { get; set; }
    }

    public sealed class ResponsePacket
    {
        public bool Success { get; set; }
        public string Code { get; set; } = ErrorCodes.Ok;
        public string Command { get; set; } = string.Empty;
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
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public int SpawnIndex { get; set; }
        public bool IsHost { get; set; }
        public bool IsSpectator { get; set; }
        public bool IsRacing { get; set; }
        public bool GameFinished { get; set; }
        public int? WinnerUserId { get; set; }
        public string? WinnerUsername { get; set; }
        public string? MapName { get; set; }
        public long ServerTick { get; set; }
        public long ServerTimestampMs { get; set; }
        public List<GamePlayerSnapshotDto>? GamePlayers { get; set; }
        public List<RoomChatMessageDto>? ChatMessages { get; set; }
        public GameEventDto? GameEvent { get; set; }
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

    public sealed class GamePlayerSnapshotDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int SpawnIndex { get; set; }
        public int VehicleIndex { get; set; }
        public long Sequence { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float VelocityZ { get; set; }
        public int CheckpointsCompleted { get; set; }
        public bool IsEliminated { get; set; }
        public bool IsFinished { get; set; }
        public bool IsDisconnected { get; set; }
        public long ServerTimestampMs { get; set; }
    }

    public sealed class GameEventDto
    {
        public long EventId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int CheckpointIndex { get; set; }
        public long ServerTimestampMs { get; set; }
    }

    public sealed class RoomChatMessageDto
    {
        public long MessageId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public long ServerTimestampMs { get; set; }
    }
}
