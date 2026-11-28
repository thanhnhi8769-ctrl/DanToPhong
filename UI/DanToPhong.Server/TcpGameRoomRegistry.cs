using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DanToPhong.Protocol;

namespace DanToPhong.Server
{
    internal static class TcpGameRoomRegistry
    {
        private const int DefaultMaxPlayers = 4;
        private const double TickSeconds = 0.05;
        private const double InputTimeoutSeconds = 0.5;
        private const double DisconnectGraceSeconds = 120;
        private const float VehicleCollisionRadius = 0.65f;
        internal const int WinnerRewardCoins = 50;
        private static readonly object Gate = new();
        private static readonly Dictionary<string, GameRoom> Rooms = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, string> Membership = new();
        private static readonly char[] RoomAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        public static ResponsePacket Create(int userId, string username, RequestPacket request)
        {
            lock (Gate)
            {
                string mapName = SanitizeMap(request.MapName);
                if (!RaceTrackCatalog.TryGet(mapName, out RaceTrackCatalog.RaceTrack track))
                    return Failure("The selected race track is unavailable.");

                RemoveUserInternal(userId);
                string code;
                do
                {
                    byte[] bytes = RandomNumberGenerator.GetBytes(5);
                    code = new string(bytes.Select(value => RoomAlphabet[value % RoomAlphabet.Length]).ToArray());
                } while (Rooms.ContainsKey(code));

                int maxPlayers = Math.Clamp(request.MaxPlayers ?? DefaultMaxPlayers, 2, 4);
                var room = new GameRoom(code, userId, username, maxPlayers, track);
                Rooms.Add(code, room);
                Membership[userId] = code;
                RecordEvent(room, "ROOM_CREATED", userId, username, 0);
                return ToResponse(room, userId, "Room created.");
            }
        }

            public static ResponsePacket ReportScene(int userId, string? sceneName, bool isReady)
            {
                string scene = (sceneName ?? string.Empty).Trim();
                if (scene.Length is 0 or > 64 || scene.Any(ch => !char.IsLetterOrDigit(ch) && ch != ' ' && ch != '_' && ch != '-'))
                    return Failure("Invalid scene name.");

                lock (Gate)
                {
                    if (!TryGetMemberRoom(userId, out GameRoom? room) ||
                        !room.Players.TryGetValue(userId, out GamePlayer? player))
                        return Failure("Join a room before reporting scene state.");

                    player.SceneName = scene;
                    player.SelectionConfirmed = isReady &&
                        string.Equals(scene, "SelectScene", StringComparison.OrdinalIgnoreCase);
                    player.LastSceneReportAt = DateTime.UtcNow;
                    TryResetRoomForLobby(room);
                    return ToResponse(room, userId, "Scene state updated.");
                }
            }

            public static bool CanInvite(int inviterUserId, int targetUserId, string roomCode)
            {
                lock (Gate)
                {
                    return TryGetMemberRoom(inviterUserId, out GameRoom? room) &&
                        string.Equals(room.Code, NormalizeCode(roomCode), StringComparison.OrdinalIgnoreCase) &&
                        !room.IsRacing &&
                        room.Players.Count < room.MaxPlayers &&
                        !room.Players.ContainsKey(targetUserId);
                }
            }

            public static bool AreRoommates(int firstUserId, int secondUserId)
            {
                lock (Gate)
                {
                    return TryGetMemberRoom(firstUserId, out GameRoom? firstRoom) &&
                        TryGetMemberRoom(secondUserId, out GameRoom? secondRoom) &&
                        ReferenceEquals(firstRoom, secondRoom);
                }
            }

            public static List<RoomDto> ListRooms()
            {
                lock (Gate)
                {
                    return Rooms.Values
                        .OrderBy(room => room.IsRacing)
                        .ThenBy(room => room.Code, StringComparer.OrdinalIgnoreCase)
                        .Select(room => new RoomDto
                        {
                            Code = room.Code,
                            HostName = room.Players.TryGetValue(room.HostUserId, out GamePlayer? host)
                                ? host.Username
                                : "Unknown",
                            CurrentPlayers = room.Players.Count,
                            MaxPlayers = room.MaxPlayers,
                            IsRacing = room.IsRacing
                        })
                        .ToList();
                }
            }

        public static ResponsePacket Join(int userId, string username, string roomCode)
        {
            lock (Gate)
            {
                if (!Rooms.TryGetValue(NormalizeCode(roomCode), out GameRoom? room))
                    return Failure("Room not found.");
                if (room.Players.ContainsKey(userId))
                    return ToResponse(room, userId, "Already in room.");
                if (room.IsRacing)
                    return Failure("The game has already started. Use spectate or reconnect.");
                if (room.Players.Count >= room.MaxPlayers)
                    return Failure("Room is full.");

                RemoveUserInternal(userId);
                room.Players.Add(userId, new GamePlayer(userId, username, room.NextSpawnIndex++));
                Membership[userId] = room.Code;
                RecordEvent(room, "PLAYER_JOINED", userId, username, 0);
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

        public static ResponsePacket ReturnToLobby(int userId)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room) ||
                    !room.Players.TryGetValue(userId, out GamePlayer? player))
                    return Failure("Join a room before returning to the lobby.");

                if (!room.IsRacing)
                    return ToResponse(room, userId, "Already in the room lobby.");

                if (player.ReturnedToLobby)
                    return ToResponse(room, userId, "Waiting for all room players to return to the lobby.");

                player.ReturnedToLobby = true;
                player.Throttle = 0f;
                player.Steer = 0f;
                player.Brake = true;
                RecordEvent(room, "PLAYER_RETURNED_TO_LOBBY", userId, player.Username, player.NextCheckpoint);
                TryResetRoomForLobby(room);
                return ToResponse(room, userId, room.IsRacing
                    ? "Waiting for all room players to return to the lobby."
                    : "Room is ready for another match.");
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
                if (room.Players.Values.Any(player => !IsReady(player)))
                    return Failure("All room players must enter Select Scene and confirm their selections before starting.");

                if (!string.IsNullOrWhiteSpace(request.MapName))
                {
                    if (!RaceTrackCatalog.TryGet(request.MapName.Trim(), out RaceTrackCatalog.RaceTrack selectedTrack))
                        return Failure("The selected race track is unavailable.");
                    room.Track = selectedTrack;
                }

                DateTime now = DateTime.UtcNow;
                foreach (GamePlayer player in room.Players.Values)
                {
                    RaceTrackCatalog.RaceSpawn spawn = room.Track.Spawns[player.SpawnIndex % room.Track.Spawns.Length];
                    player.X = spawn.X;
                    player.Y = spawn.Y;
                    player.Z = spawn.Z;
                    player.Yaw = NormalizeAngle(spawn.Yaw);
                    player.Speed = 0f;
                    player.Throttle = 0f;
                    player.Steer = 0f;
                    player.Brake = false;
                    player.Initialized = true;
                    player.LastInputAt = now;
                    player.LastClientTick = -1;
                    player.NextCheckpoint = 0;
                    player.IsEliminated = false;
                    player.IsFinished = false;
                    player.DisconnectedAt = null;
                }
                room.IsRacing = true;
                room.LastSimulationAt = now;
                RecordEvent(room, "RACE_STARTED", userId, room.Players[userId].Username, 0);
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
                if (room.WinnerUserId.HasValue || player.IsEliminated || player.DisconnectedAt.HasValue)
                    return Failure("This player cannot submit input in the current race state.");
                if (!IsFinite(request.Throttle) || !IsFinite(request.Steer) ||
                    !request.ClientTick.HasValue || !request.ClientTimestampMs.HasValue)
                    return Failure("Invalid game input.");
                if (request.ClientTick.Value <= player.LastClientTick)
                    return Failure("Stale game input.");
                if (Math.Abs(request.Throttle!.Value) > 1f || Math.Abs(request.Steer!.Value) > 1f)
                    return Failure("Game input is outside the permitted range.");

                player.Throttle = request.Throttle.Value;
                player.Steer = request.Steer.Value;
                player.Brake = request.Brake == true;
                player.LastInputAt = DateTime.UtcNow;
                player.LastClientTick = request.ClientTick.Value;
                if (request.VehicleIndex.HasValue)
                    player.VehicleIndex = Math.Clamp(request.VehicleIndex.Value, 0, 100);

                Simulate(room, player.LastInputAt);
                return ToResponse(room, userId, "Snapshot.");
            }
        }

        public static bool SubmitUdpInput(
            int userId,
            uint sequence,
            float throttle,
            float steer,
            bool brake,
            int vehicleIndex)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room) || !room.IsRacing ||
                    !room.Players.TryGetValue(userId, out GamePlayer? player) ||
                    room.WinnerUserId.HasValue || player.IsEliminated || player.DisconnectedAt.HasValue ||
                    sequence <= player.LastClientTick ||
                    !float.IsFinite(throttle) || !float.IsFinite(steer) ||
                    Math.Abs(throttle) > 1f || Math.Abs(steer) > 1f)
                    return false;

                DateTime now = DateTime.UtcNow;
                player.Throttle = throttle;
                player.Steer = steer;
                player.Brake = brake;
                player.LastInputAt = now;
                player.LastClientTick = sequence;
                player.VehicleIndex = Math.Clamp(vehicleIndex, 0, 100);
                Simulate(room, now);
                return true;
            }
        }

        public static bool TryGetNextUdpSequence(int userId, out uint nextSequence)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room) || !room.IsRacing ||
                    !room.Players.TryGetValue(userId, out GamePlayer? player) ||
                    player.DisconnectedAt.HasValue || player.IsEliminated || room.WinnerUserId.HasValue)
                {
                    nextSequence = 0;
                    return false;
                }

                long next = Math.Max(1L, player.LastClientTick + 1);
                if (next > uint.MaxValue)
                {
                    nextSequence = 0;
                    return false;
                }

                nextSequence = (uint)next;
                return true;
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
                if (eventType == "ELIMINATE")
                {
                    if (player.IsEliminated)
                        return ToResponse(room, userId, "Player was already eliminated.");
                    player.IsEliminated = true;
                    player.Throttle = 0f;
                    player.Steer = 0f;
                    player.Brake = true;
                    RecordEvent(room, "PLAYER_ELIMINATED", userId, player.Username, player.NextCheckpoint);
                    return ToResponse(room, userId, "Player eliminated.");
                }

                if (eventType is "CHECKPOINT" or "FINISH")
                    return Failure("Checkpoint and finish events are determined by the server.");
                return Failure("Unsupported game event.");
            }
        }

        public static ResponsePacket Spectate(int userId, string roomCode)
        {
            lock (Gate)
            {
                if (!Rooms.TryGetValue(NormalizeCode(roomCode), out GameRoom? room) || !room.IsRacing)
                    return Failure("A running game with that room code was not found.");
                if (room.Players.ContainsKey(userId))
                    return ToResponse(room, userId, "Player state restored.");

                RemoveUserInternal(userId);
                room.Spectators.Add(userId);
                Membership[userId] = room.Code;
                return ToResponse(room, userId, "Spectating game.");
            }
        }

        public static ResponsePacket Reconnect(int userId, string? roomCode)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room) ||
                    !string.IsNullOrWhiteSpace(roomCode) &&
                    !string.Equals(room.Code, NormalizeCode(roomCode), StringComparison.OrdinalIgnoreCase) ||
                    !room.IsRacing)
                    return Failure("No reconnectable race was found.");

                if (room.Spectators.Contains(userId))
                    return ToResponse(room, userId, "Spectating state restored.");
                if (!room.Players.TryGetValue(userId, out GamePlayer? player))
                    return Failure("No reconnectable race was found.");

                player.DisconnectedAt = null;
                player.Throttle = 0f;
                player.Steer = 0f;
                player.Brake = true;
                player.LastInputAt = DateTime.UtcNow;
                RecordEvent(room, "PLAYER_RECONNECTED", userId, player.Username, player.NextCheckpoint);
                return ToResponse(room, userId, "Race state restored.");
            }
        }

        public static bool TryBeginWinnerReward(int userId, out string roomCode, out Guid matchId, out int winnerUserId)
        {
            lock (Gate)
            {
                if (TryGetMemberRoom(userId, out GameRoom? room) &&
                    room.WinnerUserId.HasValue && !room.WinnerRewardInProgress && !room.WinnerRewarded)
                {
                    room.WinnerRewardInProgress = true;
                    roomCode = room.Code;
                    matchId = room.MatchId;
                    winnerUserId = room.WinnerUserId.Value;
                    return true;
                }
                roomCode = string.Empty;
                matchId = Guid.Empty;
                winnerUserId = 0;
                return false;
            }
        }

        public static void CompleteWinnerReward(string roomCode, bool succeeded)
        {
            lock (Gate)
            {
                if (Rooms.TryGetValue(roomCode, out GameRoom? room))
                {
                    room.WinnerRewardInProgress = false;
                    room.WinnerRewarded = succeeded;
                }
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

        public static void DisconnectUser(int userId)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room))
                    return;
                if (room.IsRacing && room.Players.TryGetValue(userId, out GamePlayer? player))
                {
                    player.DisconnectedAt = DateTime.UtcNow;
                    player.Throttle = 0f;
                    player.Steer = 0f;
                    player.Brake = true;
                    RecordEvent(room, "PLAYER_DISCONNECTED", userId, player.Username, player.NextCheckpoint);
                    return;
                }
                RemoveUserInternal(userId);
            }
        }

        public static List<GameEventDto> ReadEventsAfter(int userId, ref long lastEventId)
        {
            lock (Gate)
            {
                if (!TryGetMemberRoom(userId, out GameRoom? room))
                    return new List<GameEventDto>();

                long eventCursor = lastEventId;
                List<GameEventDto> events = room.Events
                    .Where(gameEvent => gameEvent.EventId > eventCursor)
                    .ToList();
                if (events.Count > 0)
                    lastEventId = events[events.Count - 1].EventId;
                else if (room.Events.Count > 0 && lastEventId < room.Events[0].EventId - 1)
                    lastEventId = room.Events[0].EventId - 1;
                return events;
            }
        }

        private static void RecordEvent(GameRoom room, string type, int userId, string username, int checkpointIndex)
        {
            room.Events.Add(new GameEventDto
            {
                EventId = ++room.NextEventId,
                Type = type,
                UserId = userId,
                Username = username,
                CheckpointIndex = checkpointIndex,
                ServerTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            if (room.Events.Count > 128)
                room.Events.RemoveRange(0, room.Events.Count - 128);
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

        private static bool IsReady(GamePlayer player) =>
            string.Equals(player.SceneName, "SelectScene", StringComparison.OrdinalIgnoreCase) &&
            player.SelectionConfirmed &&
            DateTime.UtcNow - player.LastSceneReportAt <= TimeSpan.FromSeconds(8);

        private static void TryResetRoomForLobby(GameRoom room)
        {
            DateTime now = DateTime.UtcNow;
            if (!room.IsRacing || room.Players.Count == 0 ||
                room.Players.Values.Any(player =>
                    !player.ReturnedToLobby ||
                    !string.Equals(player.SceneName, "LobbyScene", StringComparison.OrdinalIgnoreCase) ||
                    now - player.LastSceneReportAt > TimeSpan.FromSeconds(8)))
                return;

            room.IsRacing = false;
            room.MatchId = Guid.NewGuid();
            room.WinnerUserId = null;
            room.WinnerUsername = null;
            room.WinnerRewardInProgress = false;
            room.WinnerRewarded = false;
            room.LastSimulationAt = now;
            foreach (GamePlayer player in room.Players.Values)
            {
                player.ReturnedToLobby = false;
                player.SelectionConfirmed = false;
                player.Initialized = false;
                player.Speed = 0f;
                player.Throttle = 0f;
                player.Steer = 0f;
                player.Brake = false;
                player.LastClientTick = -1;
                player.NextCheckpoint = 0;
                player.IsEliminated = false;
                player.IsFinished = false;
                player.DisconnectedAt = null;
            }
            RecordEvent(room, "ROOM_RESET", 0, "SYSTEM", 0);
        }

        private static void Simulate(GameRoom room, DateTime now)
        {
            if (!room.IsRacing || room.WinnerUserId.HasValue)
                return;

            int[] expiredPlayers = room.Players.Values
                .Where(player => player.DisconnectedAt.HasValue &&
                    (now - player.DisconnectedAt.Value).TotalSeconds > DisconnectGraceSeconds)
                .Select(player => player.UserId)
                .ToArray();
            foreach (int expiredUserId in expiredPlayers)
                RemoveUserInternal(expiredUserId);
            if (!Rooms.ContainsKey(room.Code))
                return;

            double elapsed = (now - room.LastSimulationAt).TotalSeconds;
            if (elapsed < TickSeconds)
                return;

            int steps = Math.Clamp((int)(elapsed / TickSeconds), 1, 4);
            room.LastSimulationAt = now;
            float dt = (float)TickSeconds;

            foreach (GamePlayer player in room.Players.Values)
            {
                if (!player.Initialized || player.IsEliminated || player.DisconnectedAt.HasValue)
                    continue;

                if ((now - player.LastInputAt).TotalSeconds > InputTimeoutSeconds)
                {
                    player.Throttle = 0f;
                    player.Steer = 0f;
                    player.Brake = true;
                }

                for (int i = 0; i < steps; i++)
                {
                    float previousX = player.X;
                    float previousY = player.Y;
                    float previousZ = player.Z;
                    float acceleration = player.Brake ? 18f : player.Throttle >= 0f ? 12f : 8f;
                    player.Speed += player.Throttle * acceleration * dt;
                    if (player.Brake || Math.Abs(player.Throttle) < 0.01f)
                        player.Speed = MoveTowards(player.Speed, 0f, (player.Brake ? 18f : 2f) * dt);
                    player.Speed = Math.Clamp(player.Speed, -5f, 25f);

                    float turnRate = 65f * Math.Min(Math.Abs(player.Speed) / 8f, 1f);
                    player.Yaw = NormalizeAngle(player.Yaw + player.Steer * turnRate * dt);
                    double radians = player.Yaw * Math.PI / 180d;
                    player.X += (float)Math.Cos(radians) * player.Speed * dt;
                    player.Z -= (float)Math.Sin(radians) * player.Speed * dt;

                    if (player.NextCheckpoint < room.Track.Checkpoints.Length &&
                        CrossedGate(previousX, previousY, previousZ, player.X, player.Y, player.Z,
                            room.Track.Checkpoints[player.NextCheckpoint]))
                    {
                        player.NextCheckpoint++;
                        RecordEvent(room, "CHECKPOINT", player.UserId, player.Username, player.NextCheckpoint);
                    }

                    if (player.NextCheckpoint == room.Track.Checkpoints.Length &&
                        CrossedGate(previousX, previousY, previousZ, player.X, player.Y, player.Z, room.Track.Finish))
                    {
                        player.IsFinished = true;
                        room.WinnerUserId = player.UserId;
                        room.WinnerUsername = player.Username;
                        RecordEvent(room, "FINISH", player.UserId, player.Username, player.NextCheckpoint);
                        return;
                    }
                }
            }
        }

        private static bool CrossedGate(
            float startX, float startY, float startZ,
            float endX, float endY, float endZ,
            RaceTrackCatalog.RaceGate gate)
        {
            float radius = VehicleCollisionRadius;
            float minX = gate.X - gate.ExtentX - radius;
            float maxX = gate.X + gate.ExtentX + radius;
            float minY = gate.Y - gate.ExtentY - 1f;
            float maxY = gate.Y + gate.ExtentY + 1f;
            float minZ = gate.Z - gate.ExtentZ - radius;
            float maxZ = gate.Z + gate.ExtentZ + radius;
            float near = 0f;
            float far = 1f;

            return ClipAxis(startX, endX - startX, minX, maxX, ref near, ref far) &&
                   ClipAxis(startY, endY - startY, minY, maxY, ref near, ref far) &&
                   ClipAxis(startZ, endZ - startZ, minZ, maxZ, ref near, ref far);
        }

        private static bool ClipAxis(float start, float delta, float min, float max, ref float near, ref float far)
        {
            if (Math.Abs(delta) < 0.0001f)
                return start >= min && start <= max;

            float first = (min - start) / delta;
            float second = (max - start) / delta;
            if (first > second)
                (first, second) = (second, first);
            near = Math.Max(near, first);
            far = Math.Min(far, second);
            return near <= far;
        }

        private static ResponsePacket ToResponse(GameRoom room, int userId, string message)
        {
            long serverTick = ++room.ServerTick;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            List<RoomPlayerDto> roomPlayers = room.Players.Values
                .OrderBy(player => player.SpawnIndex)
                .Select(player => new RoomPlayerDto
                {
                    UserId = player.UserId,
                    Username = player.Username,
                    SceneName = player.SceneName ?? string.Empty,
                    IsHost = room.HostUserId == player.UserId,
                    IsReady = IsReady(player),
                    IsReturnedToLobby = player.ReturnedToLobby
                })
                .ToList();
            int readyPlayerCount = roomPlayers.Count(player => player.IsReady);
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
                IsSpectator = room.Spectators.Contains(userId),
                IsReturnedToLobby = room.Players.TryGetValue(userId, out GamePlayer? roomPlayer) &&
                    roomPlayer.ReturnedToLobby,
                IsRacing = room.IsRacing,
                CanStart = room.HostUserId == userId &&
                    room.Players.Count >= 2 &&
                    readyPlayerCount == room.Players.Count &&
                    !room.IsRacing,
                ReadyPlayerCount = readyPlayerCount,
                UnreadyPlayerCount = room.Players.Count - readyPlayerCount,
                RoomPlayers = roomPlayers,
                GameFinished = room.WinnerUserId.HasValue,
                WinnerUserId = room.WinnerUserId,
                WinnerUsername = room.WinnerUsername,
                MapName = room.Track.Name,
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
                        VelocityX = (float)Math.Cos(player.Yaw * Math.PI / 180d) * player.Speed,
                        VelocityY = 0f,
                        VelocityZ = -(float)Math.Sin(player.Yaw * Math.PI / 180d) * player.Speed,
                        CheckpointsCompleted = player.NextCheckpoint,
                        IsEliminated = player.IsEliminated,
                        IsFinished = player.IsFinished,
                        IsDisconnected = player.DisconnectedAt.HasValue,
                        ServerTimestampMs = timestamp
                    })
                    .ToList(),
                GameEvent = room.Events.Count > 0 ? room.Events[room.Events.Count - 1] : null
            };
        }

        private static void RemoveUserInternal(int userId)
        {
            if (!Membership.TryGetValue(userId, out string? roomCode))
                return;

            Membership.Remove(userId);
            if (!Rooms.TryGetValue(roomCode, out GameRoom? room))
                return;

            if (room.Players.TryGetValue(userId, out GamePlayer? leavingPlayer))
                RecordEvent(room, "PLAYER_LEFT", userId, leavingPlayer.Username, leavingPlayer.NextCheckpoint);
            room.Players.Remove(userId);
            room.Spectators.Remove(userId);
            if (room.Players.Count == 0)
            {
                Rooms.Remove(roomCode);
                return;
            }
            if (room.HostUserId == userId && room.Players.Count > 0)
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
            return MapNames.Normalize(mapName?.Trim());
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
            public GameRoom(string code, int hostUserId, string hostUsername, int maxPlayers, RaceTrackCatalog.RaceTrack track)
            {
                Code = code;
                HostUserId = hostUserId;
                MaxPlayers = maxPlayers;
                Track = track;
                Players.Add(hostUserId, new GamePlayer(hostUserId, hostUsername, 0));
                NextSpawnIndex = 1;
            }

            public string Code { get; }
            public int HostUserId { get; set; }
            public int MaxPlayers { get; }
            public RaceTrackCatalog.RaceTrack Track { get; set; }
            public Guid MatchId { get; set; } = Guid.NewGuid();
            public Dictionary<int, GamePlayer> Players { get; } = new();
            public HashSet<int> Spectators { get; } = new();
            public List<RoomChatMessageDto> ChatMessages { get; } = new();
            public int NextSpawnIndex { get; set; }
            public long NextMessageId { get; set; }
            public int? WinnerUserId { get; set; }
            public string? WinnerUsername { get; set; }
            public bool WinnerRewardInProgress { get; set; }
            public bool WinnerRewarded { get; set; }
            public bool IsRacing { get; set; }
            public long ServerTick { get; set; }
            public DateTime LastSimulationAt { get; set; } = DateTime.UtcNow;
            public long NextEventId { get; set; }
            public List<GameEventDto> Events { get; } = new();
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
            public string? SceneName { get; set; }
            public DateTime LastSceneReportAt { get; set; } = DateTime.MinValue;
            public bool SelectionConfirmed { get; set; }
            public long LastClientTick { get; set; } = -1;
            public int NextCheckpoint { get; set; }
            public bool IsEliminated { get; set; }
            public bool IsFinished { get; set; }
            public DateTime? DisconnectedAt { get; set; }
            public bool ReturnedToLobby { get; set; }
        }
    }
}
