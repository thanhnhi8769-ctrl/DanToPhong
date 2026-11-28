using System;
using System.Collections.Generic;
using System.Linq;
using DanToPhong.Protocol;

namespace DanToPhong.Server
{
    // Danh sách phòng + lời mời, giữ trong RAM. Game (chủ phòng) báo lên; phòng không cập nhật 30s sẽ tự biến mất.
    public static class RoomRegistry
    {
        private sealed class Room
        {
            public string Code = "";
            public int OwnerUserId;
            public string HostName = "";
            public int Current, Max;
            public bool IsRacing;
            public DateTime Updated;
        }

        private sealed class Invite
        {
            public int ToUserId;
            public string RoomCode = "";
            public string FromName = "";
            public DateTime Created;
            public bool Delivered;
        }

        private static readonly object Gate = new();
        private static readonly Dictionary<string, Room> Rooms = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Invite> Invites = new();
        private static readonly TimeSpan RoomTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan InviteTtl = TimeSpan.FromMinutes(2);

        private static void Purge()
        {
            DateTime now = DateTime.UtcNow;
            foreach (string k in Rooms.Where(kv => now - kv.Value.Updated > RoomTtl).Select(kv => kv.Key).ToList())
                Rooms.Remove(k);
            Invites.RemoveAll(i => now - i.Created > InviteTtl);
        }

        public static string? HostNameIfOwned(string code, int owner)
        {
            lock (Gate)
                return Rooms.TryGetValue(code, out var r) && r.OwnerUserId == owner ? r.HostName : null;
        }

        public static bool Upsert(string code, int owner, string hostName, int current, int max, bool racing)
        {
            lock (Gate)
            {
                Purge();
                if (Rooms.TryGetValue(code, out Room? existing) && existing.OwnerUserId != owner)
                    return false;
                Rooms[code] = new Room
                {
                    Code = code.ToUpperInvariant(),
                    OwnerUserId = owner,
                    HostName = hostName,
                    Current = current,
                    Max = max,
                    IsRacing = racing,
                    Updated = DateTime.UtcNow
                };
                return true;
            }
        }

        public static void Remove(string code, int owner)
        {
            lock (Gate)
                if (Rooms.TryGetValue(code, out var r) && r.OwnerUserId == owner) Rooms.Remove(code);
        }

        public static void RemoveByOwner(int owner)
        {
            lock (Gate)
                foreach (string k in Rooms.Where(kv => kv.Value.OwnerUserId == owner).Select(kv => kv.Key).ToList())
                    Rooms.Remove(k);
        }

        public static List<RoomDto> List()
        {
            lock (Gate)
            {
                Purge();
                return Rooms.Values
                    .OrderBy(r => r.IsRacing)                 // phòng đang chờ lên trước
                    .ThenByDescending(r => r.Updated)
                    .Select(r => new RoomDto
                    {
                        Code = r.Code,
                        HostName = r.HostName,
                        CurrentPlayers = r.Current,
                        MaxPlayers = r.Max,
                        IsRacing = r.IsRacing
                    }).ToList();
            }
        }

        public static bool Exists(string code)
        {
            lock (Gate) { Purge(); return Rooms.ContainsKey(code); }
        }

        public static bool AddInvite(int toUserId, string code, string fromName)
        {
            lock (Gate)
            {
                Purge();
                if (Invites.Any(i => i.ToUserId == toUserId && i.RoomCode.Equals(code, StringComparison.OrdinalIgnoreCase)
                                     && i.FromName == fromName))
                    return true; // đã mời rồi, coi như thành công
                if (Invites.Count(i => i.ToUserId == toUserId) >= 5) return false;

                Invites.Add(new Invite
                {
                    ToUserId = toUserId,
                    RoomCode = code.ToUpperInvariant(),
                    FromName = fromName,
                    Created = DateTime.UtcNow
                });
                return true;
            }
        }

        public static List<InviteDto> TakeNewInvites(int userId)
        {
            lock (Gate)
            {
                Purge();
                var list = new List<InviteDto>();
                foreach (Invite i in Invites.Where(x => x.ToUserId == userId && !x.Delivered))
                {
                    i.Delivered = true;
                    list.Add(new InviteDto { RoomCode = i.RoomCode, FromName = i.FromName });
                }
                return list;
            }
        }
    }
}
