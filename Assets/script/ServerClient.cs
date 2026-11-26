using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Tên field viết thường chữ đầu để JsonUtility khớp JSON camelCase của server.
[Serializable]
public class Req
{
    public string command, token, category, mapName, targetId, roomId, mode, requestId;
    public int itemIndex, coinsDelta, price, playerCount, maxPlayers;
    public bool isRacing;
}
[Serializable] public class ItemDto { public string category; public int itemIndex; public bool isEquipped; }
[Serializable] public class InvDto { public int coins; public string selectedMap; public List<ItemDto> items = new List<ItemDto>(); }
[Serializable] public class UserDto { public string username, fullName, racerId, email, avatarPath; public int coins; }
[Serializable] public class InviteDto { public string roomCode, fromName; }
[Serializable] public class LbEntry { public int rank, coins; public string racerId, username, fullName; }
[Serializable] public class Resp { public bool success; public string code, message; public InvDto inventory; public UserDto user; public string avatarBase64; public List<InviteDto> invites; public List<LbEntry> leaderboard; }

public static class ServerClient
{
    private static TcpClient tcp;
    private static Stream ns;
    private static readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

    private static bool closed;      // Close() đã được gọi: không tự nối lại nữa
    private static bool resyncing;
    private static int connId;       // tăng mỗi lần mở kết nối mới
    private static int lifecycleVersion;
    private static readonly SemaphoreSlim connectGate = new SemaphoreSlim(1, 1);

    // Các lệnh trong danh sách có thể retry sau reconnect. CAPNHATXU/MUAHANG
    // chỉ an toàn vì giữ nguyên requestId và server khử trùng lặp theo user + command + id.
    // LAYTRANGTHAI không retry vì việc đọc có thể đồng thời lấy lời mời khỏi hàng đợi.
    private static readonly HashSet<string> Idempotent = new HashSet<string>
    {
        "XACTHUCTOKEN", "LAYKHO", "LAYTHONGTIN", "LAYAVATAR", 
        "CAPNHATPHONG", "XOAPHONG", "CHONMAP", "TRANGBI", "CAPNHATXU", "MUAHANG"
    };

    public static InvDto Cache;
    public static bool Ready => Cache != null && ns != null;

    // Bắn khi kho (xu / xe / map) khác với Cache hiện tại (kể cả khi Cache vừa bị game sửa lạc quan rồi server từ chối)
    public static event Action DaCapNhat;

    // JsonUtility KHÔNG bao giờ trả null cho field class lồng nhau: response không có kho vẫn cho ra
    // một InvDto rỗng (coins = 0). Server luôn gửi selectedMap khi có kho, nên dùng nó làm dấu hiệu.
    private static bool CoKho(Resp r)
    {
        return r != null && r.inventory != null && !string.IsNullOrWhiteSpace(r.inventory.selectedMap);
    }

    private static int Sig(InvDto inv)
    {
        unchecked
        {
            int h = inv.coins;
            h = h * 31 + (inv.selectedMap?.GetHashCode() ?? 0);
            if (inv.items != null)
                foreach (ItemDto it in inv.items)
                {
                    if (it == null) continue;
                    h = h * 31 + ((it.category?.GetHashCode() ?? 0) ^ (it.itemIndex * 397) ^ (it.isEquipped ? 1 : 0));
                }
            return h;
        }
    }

    // ---------------------------------------------------------------------
    // Kết nối
    // ---------------------------------------------------------------------
    public static async Task<bool> ConnectAsync()
    {
        await connectGate.WaitAsync();
        try
        {
            if (!closed && ns != null && tcp != null && tcp.Connected && Cache != null) return true;
            if (string.IsNullOrWhiteSpace(LauncherArgs.Host) || LauncherArgs.Port < 1 || LauncherArgs.Port > 65535 || string.IsNullOrWhiteSpace(LauncherArgs.Token))
                throw new InvalidOperationException("Launcher connection arguments are missing or invalid.");
            closed = false;
            int version = Interlocked.Increment(ref lifecycleVersion);
            await OpenAsync(version);
            if (closed || version != Volatile.Read(ref lifecycleVersion)) return false;

            Resp r = await SendOnce(new Req { command = "XACTHUCTOKEN", token = LauncherArgs.Token });
            if (closed || version != Volatile.Read(ref lifecycleVersion)) return false;
            if (r == null || !r.success) { Close(); return false; }

            r = await SendOnce(new Req { command = "LAYKHO" });
            if (closed || version != Volatile.Read(ref lifecycleVersion)) return false;
            if (r != null && r.success && Cache != null) return true;
            Close();
            return false;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ServerClient] " + e.Message);
            Close();
            return false;
        }
        finally { connectGate.Release(); }
    }

    private static async Task OpenAsync(int expectedVersion)
    {
        if (closed || expectedVersion != Volatile.Read(ref lifecycleVersion))
            throw new OperationCanceledException("Connection attempt was canceled.");

        NetworkStream oldStream = ns;
        TcpClient oldClient = tcp;
        ns = null;
        tcp = null;
        try { oldStream?.Dispose(); } catch { }
        try { oldClient?.Close(); } catch { }

        TcpClient client = new TcpClient();
        tcp = client;
        Stream stream = null;
        try
        {
            Task connect = client.ConnectAsync(LauncherArgs.Host, LauncherArgs.Port);
            using (var timeout = new CancellationTokenSource())
            {
                Task delay = Task.Delay(TIMEOUT_MS, timeout.Token);
                if (await Task.WhenAny(connect, delay) != connect)
                {
                    _ = connect.ContinueWith(x => { var ignored = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException("Connection timeout");
                }
                timeout.Cancel();
            }
            await connect;
            if (closed || expectedVersion != Volatile.Read(ref lifecycleVersion))
                throw new OperationCanceledException("Connection attempt was canceled.");

            stream = client.GetStream();
            if (LauncherArgs.UseTls)
            {
                var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
                try
                {
                    Task authenticate = ssl.AuthenticateAsClientAsync(LauncherArgs.Host);
                    using (var timeout = new CancellationTokenSource())
                    {
                        Task delay = Task.Delay(TIMEOUT_MS, timeout.Token);
                        if (await Task.WhenAny(authenticate, delay) != authenticate)
                        {
                            _ = authenticate.ContinueWith(x => { var ignored = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                            throw new TimeoutException("TLS handshake timeout");
                        }
                        timeout.Cancel();
                    }
                    await authenticate;
                    stream = ssl;
                }
                catch
                {
                    try { ssl.Dispose(); } catch { }
                    throw;
                }
            }

            if (closed || expectedVersion != Volatile.Read(ref lifecycleVersion))
                throw new OperationCanceledException("Connection attempt was canceled.");

            ns = stream;
            connId++;
        }
        catch
        {
            try { stream?.Dispose(); } catch { }
            try { client.Close(); } catch { }
            try { client.Dispose(); } catch { }
            if (ReferenceEquals(ns, stream)) ns = null;
            if (ReferenceEquals(tcp, client)) tcp = null;
            throw;
        }
    }

    // Nối lại + xác thực lại bằng token cũ (server giữ token 6 giờ, dùng lại được).
    private static async Task<bool> ReconnectAsync()
    {
        if (closed) return false;
        int version = Volatile.Read(ref lifecycleVersion);

        await gate.WaitAsync();
        try
        {
            if (closed || version != Volatile.Read(ref lifecycleVersion)) return false;
            await OpenAsync(version);
            var (a, _) = await ExchangeVoiTimeout(new Req { command = "XACTHUCTOKEN", token = LauncherArgs.Token });
            return !closed && version == Volatile.Read(ref lifecycleVersion) && a != null && a.success;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ServerClient] Reconnect failed: " + e.Message);
            return false;
        }
        finally { gate.Release(); }
    }

    // ---------------------------------------------------------------------
    // Gọi lệnh
    // ---------------------------------------------------------------------
    public static async Task<Resp> CallAsync(Req req)
    {
        if (closed || ns == null) return null;
        if (req.command == "CAPNHATXU" || req.command == "MUAHANG")
            req.requestId = string.IsNullOrWhiteSpace(req.requestId) ? Guid.NewGuid().ToString("N") : req.requestId;

        int id = connId;
        Resp r = await SendOnce(req);
        if (r != null) return r;

        // Lệnh không gửi lại được: chỉ nối lại rồi lấy số xu/kho thật từ server
        if (!Idempotent.Contains(req.command))
        {
            Resync();
            return null;
        }

        for (int i = 1; i <= 2; i++)
        {
            await Task.Delay(400 * i);
            if (closed) return null;

            // Nếu chỗ khác đã nối lại rồi thì chỉ cần gửi lại
            if (connId == id && !await ReconnectAsync()) continue;

            r = await SendOnce(req);
            if (r != null) return r;
            id = connId;
        }
        if (req.command == "CAPNHATXU" || req.command == "MUAHANG") Resync();
        return null;
    }

    private static async Task<Resp> SendOnce(Req req)
    {
        Resp resp;
        bool khoDoi;
        await gate.WaitAsync();
        try
        {
            if (ns == null) return null;
            (resp, khoDoi) = await ExchangeVoiTimeout(req);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ServerClient] " + e.Message);
            return null;
        }
        finally { gate.Release(); }

        // Báo UI sau khi nhả gate để UI gọi lại ServerClient không bị kẹt
        if (khoDoi) Notify();
        return resp;
    }

    // Gửi 1 gói + đọc 1 gói. Gọi khi đang giữ gate. KhoDoi = kho mới khác Cache hiện tại.
    private static async Task<(Resp Resp, bool KhoDoi)> Exchange(Req req)
    {

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(req));
        byte[] head = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(head, body.Length);
        await ns.WriteAsync(head, 0, 4);
        await ns.WriteAsync(body, 0, body.Length);

        byte[] lb = await ReadN(4);
        int len = BinaryPrimitives.ReadInt32BigEndian(lb);
        if (len <= 0 || len > 256 * 1024) throw new Exception("Bad packet length " + len);
        byte[] rb = await ReadN(len);
        Resp resp = JsonUtility.FromJson<Resp>(Encoding.UTF8.GetString(rb));

        bool doi = false;
        if (!closed && CoKho(resp))
        {
            doi = Cache == null || Sig(Cache) != Sig(resp.inventory);
            Cache = resp.inventory;
        }
        return (resp, doi);
    }

    private static async Task<byte[]> ReadN(int n)
    {
        byte[] b = new byte[n];
        int o = 0;
        while (o < n)
        {
            int r = await ns.ReadAsync(b, o, n - o);
            if (r == 0) throw new Exception("Server closed connection");
            o += r;
        }
        return b;
    }

    private static void Notify()
    {
        try { DaCapNhat?.Invoke(); }
        catch (Exception e) { Debug.LogWarning("[ServerClient] " + e.Message); }
    }

    // Lệnh không gửi lại được bị lỗi giữa chừng: không biết server đã nhận chưa -> hỏi lại server cho chắc
    private static async void Resync()
    {
        if (resyncing || closed) return;
        resyncing = true;
        try
        {
            await Task.Delay(300);
            if (await ReconnectAsync())
                await SendOnce(new Req { command = "LAYKHO" });
        }
        finally { resyncing = false; }
    }
    private const int TIMEOUT_MS = 8000;

    private static async Task<(Resp Resp, bool KhoDoi)> ExchangeVoiTimeout(Req req)
    {
        Task<(Resp Resp, bool KhoDoi)> t = Exchange(req);
        using (var timeout = new CancellationTokenSource())
        {
            Task delay = Task.Delay(TIMEOUT_MS, timeout.Token);
            Task xong = await Task.WhenAny(t, delay);
            if (xong == t)
            {
                timeout.Cancel();
                return await t;
            }
        }

        // Quá hạn: đóng socket để Exchange kết thúc bằng lỗi và NHẢ gate (nếu không mọi lệnh sau sẽ treo mãi)
        try { ns?.Dispose(); } catch { }
        _ = t.ContinueWith(x => { var e = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        throw new TimeoutException("Server timeout");
    }
    // Hỏi lại server để cache khớp launcher
    public static async Task LamMoi()
    {
        await CallAsync(new Req { command = "LAYKHO" });
    }

    // Kho + lời mời trong 1 lần gọi. inRoom = true thì server giữ lại lời mời (không lấy đi).
    public static Task<Resp> LayTrangThai(bool inRoom)
    {
        return CallAsync(new Req { command = "LAYTRANGTHAI", mode = inRoom ? "room" : "" });
    }

    // Gửi "bắn rồi quên": thứ tự được giữ nhờ gate, Cache tự sync lại từ response.
    public static async void Fire(Req req) { await CallAsync(req); }

    public static void Close()
    {
        closed = true;
        Interlocked.Increment(ref lifecycleVersion);
        try { ns?.Dispose(); } catch { }
        try { tcp?.Close(); } catch { }
        ns = null; tcp = null; Cache = null;
    }
}
