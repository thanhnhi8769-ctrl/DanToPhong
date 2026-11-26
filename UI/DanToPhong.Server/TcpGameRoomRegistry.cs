using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DanToPhong.Protocol;

namespace DanToPhong.Server
{
    internal static class TcpGameRoomRegistry
    {
        private const int DefaultMaxPlayers = 6;
        private const double TickSeconds = 0.05;
        private const double InputTimeoutSeconds = 0.5;
        private const float MaxWorldCoordinate = 100000f;
        private static readonly object Gate = new();
        private static readonly Dictionary<string, GameRoom> Rooms = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, string> Membership = new();
        private static readonly char[] RoomAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        public static ResponsePacket Create(int userId, string username, RequestPacket request)
        {
            lock (Gate)
            {
                RemoveUserInternal(userId);
                string code;
                do
                {
                    byte[] bytes = RandomNumberGenerator.GetBytes(5);
                    code = new string(bytes.Select(value => RoomAlphabet[value % RoomAlphabet.Length]).ToArray());
                } while (Rooms.ContainsKey(code));

                int maxPlayers = Math.Clamp(request.MaxPlayers ?? DefaultMaxPlayers, 2, 8);
                var room = new GameRoom(code, userId, username, maxPlayers, SanitizeMap(request.MapName));
                Rooms.Add(code, room);
                Membership[userId] = code;
                return ToResponse(room, userId, "Room created.");
            }
        }

        public static ResponsePacket Join(int userId, string username, string roomCode)
        {
            lock (Gate)
            {
                if (!Rooms.TryGetValue(NormalizeCode(roomCode), out GameRoom? room))
                    return Failure("Room not found.");
                if (room.IsRacing)
                    return Failure("The game has already started.");
                if (room.Players.ContainsKey(userId))
                    return ToResponse(room, userId, "Already in room.");
                if (room.Players.Count >= room.MaxPlayers)
                    return Failure("Room is full.");

                RemoveUserInternal(userId);
                room.Players.Add(userId, new GamePlayer(userId, username, room.NextSpawnIndex++));
                Membership[userId] = room.Code;
                return ToResponse(room, userId, "Joined room.");
            }
        }

        public static ResponsePacket Leave(int userId)
        {
            lock (Gate)
            {
                RemoveUserInternal(userId);
                return new ResponsePacket { Success = true, Code = ErrorCodes.Ok, Message = "Left room." };
            }
        }

        public static ResponsePacket Start(int userId, RequestPacket request)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room))
                    return Failure("Join a room first.");
                if (room.HostUserId != userId)
                    return Failure("Only the room host can start the game.");
                if (room.Players.Count < 2)
                    return Failure("At least two players are required.");
                if (room.IsRacing)
                    return Failure("The game has already started.");

                if (!string.IsNullOrWhiteSpace(request.MapName))
                    room.MapName = request.MapName.Trim();
                room.IsRacing = true;
                room.LastSimulationAt = DateTime.UtcNow;
                return ToResponse(room, userId, "Game started.");
            }
        }

        public static ResponsePacket State(int userId)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room))
                    return Failure("Join a room first.");
                Simulate(room, DateTime.UtcNow);
                return ToResponse(room, userId, "Room state.");
            }
        }

        public static ResponsePacket SubmitInput(int userId, RequestPacket request)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room) || !room.IsRacing)
                    return Failure("You are not in a running game.");
                if (!room.Players.TryGetValue(userId, out GamePlayer? player))
                    return Failure("Player is not in this room.");
                if (!IsFinite(request.PositionX) || !IsFinite(request.PositionY) ||
                    !IsFinite(request.PositionZ) || !IsFinite(request.RotationY) ||
                    !IsFinite(request.Throttle) || !IsFinite(request.Steer) ||
                    !request.ClientTick.HasValue || !request.ClientTimestampMs.HasValue)
                    return Failure("Invalid game input.");
                if (request.ClientTick.Value <= player.LastClientTick)
                    return Failure("Stale game input.");

                if (!player.Initialized)
                {
                    float x = request.PositionX!.Value;
                    float y = request.PositionY!.Value;
                    float z = request.PositionZ!.Value;
                    if (Math.Abs(x) > MaxWorldCoordinate || Math.Abs(y) > MaxWorldCoordinate ||
                        Math.Abs(z) > MaxWorldCoordinate)
                        return Failure("Initial position is out of bounds.");

                    player.X = x;
                    player.Y = y;
                    player.Z = z;
                    player.Yaw = NormalizeAngle(request.RotationY!.Value);
                    player.Initialized = true;
                }

                player.Throttle = Math.Clamp(request.Throttle!.Value, -1f, 1f);
                player.Steer = Math.Clamp(request.Steer!.Value, -1f, 1f);
                player.Brake = request.Brake == true;
                player.LastInputAt = DateTime.UtcNow;
                player.LastClientTick = request.ClientTick.Value;
                if (request.VehicleIndex.HasValue)
                    player.VehicleIndex = Math.Clamp(request.VehicleIndex.Value, 0, 100);

                Simulate(room, player.LastInputAt);
                return ToResponse(room, userId, "Snapshot.");
            }
        }

        public static ResponsePacket SubmitEvent(int userId, RequestPacket request)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room) || !room.IsRacing)
                    return Failure("You are not in a running game.");
                if (!room.Players.TryGetValue(userId, out GamePlayer? player))
                    return Failure("Player is not in this room.");
                if (room.WinnerUserId.HasValue)
                    return ToResponse(room, userId, "A winner has already been recorded.");

                string eventType = (request.GameEventType ?? string.Empty).Trim().ToUpperInvariant();
                if (eventType == "CHECKPOINT")
                {
                    int total = request.TotalCheckpoints ?? -1;
                    int checkpoint = request.CheckpointIndex ?? 0;
                    if (total is < 0 or > 100 || checkpoint is < 1 or > 100 ||
                        (player.TotalCheckpoints != 0 && player.TotalCheckpoints != total) ||
                        checkpoint != player.NextCheckpoint + 1 || checkpoint > total)
                        return Failure("Checkpoint sequence is invalid.");

                    player.TotalCheckpoints = total;
                    player.NextCheckpoint = checkpoint;
                    return ToResponse(room, userId, "Checkpoint accepted.");
                }

                if (eventType == "FINISH")
                {
                    if (player.NextCheckpoint < player.TotalCheckpoints)
                        return Failure("Required checkpoints have not been completed.");
                    room.WinnerUserId = userId;
                    room.WinnerUsername = player.Username;
                    return ToResponse(room, userId, "Finish recorded.");
                }

                return Failure("Unsupported game event.");
            }
        }

        public static ResponsePacket SendChat(int userId, string? message)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room))
                    return Failure("Join a room first.");
                string text = (message ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
                if (text.Length is < 1 or > 120 || text.Any(char.IsControl))
                    return Failure("Chat message must contain 1 to 120 printable characters.");

                room.ChatMessages.Add(new RoomChatMessageDto
                {
                    MessageId = ++room.NextMessageId,
                    Username = room.Players.TryGetValue(userId, out GamePlayer? player) ? player.Username : "Player",
                    Message = text,
                    ServerTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                if (room.ChatMessages.Count > 100)
                    room.ChatMessages.RemoveRange(0, room.ChatMessages.Count - 100);
                return ChatResponse(room, 0);
            }
        }

        public static ResponsePacket PollChat(int userId, long afterMessageId)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room))
                    return Failure("Join a room first.");
                return ChatResponse(room, Math.Max(0, afterMessageId));
            }
        }

        public static void RemoveUser(int userId)
        {
            lock (Gate)
                RemoveUserInternal(userId);
        }

        private static ResponsePacket ChatResponse(GameRoom room, long afterMessageId)
        {
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Command = Commands.ChatPoll,
                ChatMessages = room.ChatMessages
                    .Where(message => message.MessageId > afterMessageId)
                    .Take(50)
                    .ToList()
            };
        }

        private static void Simulate(GameRoom room, DateTime now)
        {
            if (!room.IsRacing)
                return;

            double elapsed = (now - room.LastSimulationAt).TotalSeconds;
            if (elapsed < TickSeconds)
                return;

            int steps = Math.Clamp((int)(elapsed / TickSeconds), 1, 4);
            room.LastSimulationAt = now;
            float dt = (float)TickSeconds;

            foreach (GamePlayer player in room.Players.Values)
            {
                if (!player.Initialized)
                    continue;

                if ((now - player.LastInputAt).TotalSeconds > InputTimeoutSeconds)
                {
                    player.Throttle = 0f;
                    player.Steer = 0f;
                    player.Brake = true;
                }

                for (int i = 0; i < steps; i++)
                {
                    float acceleration = player.Brake ? 18f : player.Throttle >= 0f ? 12f : 8f;
                    player.Speed += player.Throttle * acceleration * dt;
                    if (player.Brake || Math.Abs(player.Throttle) < 0.01f)
                        player.Speed = MoveTowards(player.Speed, 0f, (player.Brake ? 18f : 2f) * dt);
                    player.Speed = Math.Clamp(player.Speed, -5f, 25f);

                    float turnRate = 65f * Math.Min(Math.Abs(player.Speed) / 8f, 1f);
                    player.Yaw = NormalizeAngle(player.Yaw + player.Steer * turnRate * dt);
                    double radians = player.Yaw * Math.PI / 180d;
                    player.X += (float)Math.Sin(radians) * player.Speed * dt;
                    player.Z += (float)Math.Cos(radians) * player.Speed * dt;
                }
            }
        }

        private static ResponsePacket ToResponse(GameRoom room, int userId, string message)
        {
            long serverTick = ++room.ServerTick;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Command = Commands.GameSnapshot,
                Message = message,
                RoomId = room.Code,
                PlayerCount = room.Players.Count,
                MaxPlayers = room.MaxPlayers,
                SpawnIndex = room.Players.TryGetValue(userId, out GamePlayer? currentPlayer) ? currentPlayer.SpawnIndex : 0,
                IsHost = room.HostUserId == userId,
                IsRacing = room.IsRacing,
                GameFinished = room.WinnerUserId.HasValue,
                WinnerUserId = room.WinnerUserId,
                WinnerUsername = room.WinnerUsername,
                MapName = room.MapName,
                ServerTick = serverTick,
                ServerTimestampMs = timestamp,
                GamePlayers = room.Players.Values
                    .Where(player => player.Initialized)
                    .Select(player => new GamePlayerSnapshotDto
                    {
                        UserId = player.UserId,
                        Username = player.Username,
                        SpawnIndex = player.SpawnIndex,
                        VehicleIndex = player.VehicleIndex,
                        Sequence = serverTick,
                        PositionX = player.X,
                        PositionY = player.Y,
                        PositionZ = player.Z,
                        RotationX = 0f,
                        RotationY = player.Yaw,
                        RotationZ = 0f,
                        RotationW = 1f,
                        VelocityX = (float)Math.Sin(player.Yaw * Math.PI / 180d) * player.Speed,
                        VelocityY = 0f,
                        VelocityZ = (float)Math.Cos(player.Yaw * Math.PI / 180d) * player.Speed,
                        ServerTimestampMs = timestamp
                    })
                    .ToList()
            };
        }

        private static void RemoveUserInternal(int userId)
        {
            if (!Membership.TryGetValue(userId, out string? roomCode))
                return;

            Membership.Remove(userId);
            if (!Rooms.TryGetValue(roomCode, out GameRoom? room))
                return;

            room.Players.Remove(userId);
            if (room.Players.Count == 0)
            {
                Rooms.Remove(roomCode);
                return;
            }
            if (room.HostUserId == userId)
                room.HostUserId = room.Players.Keys.First();
        }

        private static bool TryGetMemberRoom(int userId, out GameRoom room)
        {
            if (Membership.TryGetValue(userId, out string? code) && Rooms.TryGetValue(code, out GameRoom? found))
            {
                room = found;
                return true;
            }
            room = null!;
            return false;
        }

        private static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

        private static string SanitizeMap(string? mapName)
        {
            string value = (mapName ?? "game").Trim();
            return value.Length is > 0 and <= 50 ? value : "game";
        }

        private static bool IsFinite(float? value) => value.HasValue && !float.IsNaN(value.Value) && !float.IsInfinity(value.Value);

        private static float NormalizeAngle(float value)
        {
            value %= 360f;
            return value < 0f ? value + 360f : value;
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
                return target;
            return current + Math.Sign(target - current) * maxDelta;
        }

        private static ResponsePacket Failure(string message) => new()
        {
            Success = false,
            Code = ErrorCodes.InvalidInput,
            Message = message
        };

        private sealed class GameRoom
        {
            public GameRoom(string code, int hostUserId, string hostUsername, int maxPlayers, string mapName)
            {
                Code = code;
                HostUserId = hostUserId;
                MaxPlayers = maxPlayers;
                MapName = mapName;
                Players.Add(hostUserId, new GamePlayer(hostUserId, hostUsername, 0));
                NextSpawnIndex = 1;
            }

            public string Code { get; }
            public int HostUserId { get; set; }
            public int MaxPlayers { get; }
            public string MapName { get; set; }
            public Dictionary<int, GamePlayer> Players { get; } = new();
            public List<RoomChatMessageDto> ChatMessages { get; } = new();
            public int NextSpawnIndex { get; set; }
            public long NextMessageId { get; set; }
            public int? WinnerUserId { get; set; }
            public string? WinnerUsername { get; set; }
            public bool IsRacing { get; set; }
            public long ServerTick { get; set; }
            public DateTime LastSimulationAt { get; set; } = DateTime.UtcNow;
        }

        private sealed class GamePlayer
        {
            public GamePlayer(int userId, string username, int spawnIndex)
            {
                UserId = userId;
                Username = username;
                SpawnIndex = spawnIndex;
                LastInputAt = DateTime.UtcNow;
            }

            public int UserId { get; }
            public string Username { get; }
            public int SpawnIndex { get; }
            public int VehicleIndex { get; set; }
            public bool Initialized { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Yaw { get; set; }
            public float Speed { get; set; }
            public float Throttle { get; set; }
            public float Steer { get; set; }
            public bool Brake { get; set; }
            public DateTime LastInputAt { get; set; }
            public int LastClientTick { get; set; } = -1;
            public int NextCheckpoint { get; set; }
            public int TotalCheckpoints { get; set; }
        }
    }
}
