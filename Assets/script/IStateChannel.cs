using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public interface IStateChannel
{
    Task StartReceivingAsync();
    Task<Resp> SendStateAsync(Req input);
    Task<Resp> SendEventAsync(Req gameEvent);
    bool TryDequeueSnapshot(out Resp snapshot);
    bool TryDequeueEvent(out Resp gameEvent);
    void Close();
}

public sealed class TcpStateChannel : IStateChannel
{
    private const int MaxPacketBytes = 64 * 1024;
    private const int MaxQueuedSnapshots = 8;
    private const int TimeoutMilliseconds = 8000;
    private readonly object queueLock = new object();
    private readonly Queue<Resp> snapshots = new Queue<Resp>(MaxQueuedSnapshots);
    private readonly Queue<Resp> events = new Queue<Resp>();
    private readonly CancellationTokenSource stop = new CancellationTokenSource();
    private long lastEventId;
    private TcpClient client;
    private Stream stream;
    private Task receiveTask;
    private bool started;
    private bool closed;

    public async Task StartReceivingAsync()
    {
        if (closed)
            throw new ObjectDisposedException(nameof(TcpStateChannel));
        if (started)
            return;

        started = true;
        try
        {
            await ConnectAndSubscribeAsync(stop.Token);
            receiveTask = ReceiveLoopAsync(stop.Token);
        }
        catch
        {
            started = false;
            CloseConnection();
            throw;
        }
    }

    public Task<Resp> SendStateAsync(Req input)
    {
        return ServerClient.CallAsync(input);
    }

    public Task<Resp> SendEventAsync(Req gameEvent)
    {
        return ServerClient.CallAsync(gameEvent);
    }

    public bool TryDequeueSnapshot(out Resp snapshot)
    {
        lock (queueLock)
        {
            if (snapshots.Count == 0)
            {
                snapshot = null;
                return false;
            }
            snapshot = snapshots.Dequeue();
            return true;
        }
    }

    public bool TryDequeueEvent(out Resp gameEvent)
    {
        lock (queueLock)
        {
            if (events.Count == 0)
            {
                gameEvent = null;
                return false;
            }
            gameEvent = events.Dequeue();
            return true;
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        if (string.IsNullOrWhiteSpace(LauncherArgs.Host) ||
            LauncherArgs.Port < 1 || LauncherArgs.Port > 65535 ||
            string.IsNullOrWhiteSpace(LauncherArgs.Token))
            throw new InvalidOperationException("Launcher connection arguments are missing or invalid.");

        var newClient = new TcpClient();
        client = newClient;
        Stream newStream = null;
        try
        {
            Task connect = newClient.ConnectAsync(LauncherArgs.Host, LauncherArgs.Port);
            await AwaitWithTimeoutAsync(connect, token, "Gameplay stream connection timeout.");
            token.ThrowIfCancellationRequested();
            newStream = newClient.GetStream();

            if (LauncherArgs.UseTls)
            {
                var ssl = new SslStream(newStream, leaveInnerStreamOpen: false);
                Task authenticate = ssl.AuthenticateAsClientAsync(LauncherArgs.Host);
                try
                {
                    await AwaitWithTimeoutAsync(authenticate, token, "Gameplay stream TLS handshake timeout.");
                    token.ThrowIfCancellationRequested();
                    newStream = ssl;
                }
                catch
                {
                    ssl.Dispose();
                    throw;
                }
            }

            stream = newStream;
            await SendAsync(new Req { command = "XACTHUCTOKEN", token = LauncherArgs.Token });
            Resp authenticated = await ReceiveAsync();
            if (authenticated == null || !authenticated.success)
                throw new InvalidOperationException("Gameplay stream token validation failed.");

            await SendAsync(new Req { command = "GAME_STREAM", roomId = LauncherArgs.Room });
            Resp subscribed = await ReceiveAsync();
            if (subscribed == null || !subscribed.success || subscribed.command != "GAME_STREAM")
                throw new InvalidOperationException("Server rejected the gameplay snapshot stream.");
        }
        catch
        {
            try { newStream?.Dispose(); } catch { }
            try { newClient.Close(); } catch { }
            try { newClient.Dispose(); } catch { }
            if (ReferenceEquals(client, newClient)) client = null;
            if (ReferenceEquals(stream, newStream)) stream = null;
            throw;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (stream == null)
            {
                try
                {
                    await ConnectAndSubscribeAsync(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    CloseConnection();
                    Debug.LogWarning("[TCP gameplay] Snapshot stream reconnect failed: " + ex.GetType().Name);
                    try { await Task.Delay(2000, token); }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                    continue;
                }
            }

            try
            {
                Resp response = await ReceiveAsync();
                if (response == null)
                    throw new EndOfStreamException("Gameplay snapshot stream closed by the server.");

                lock (queueLock)
                {
                    if (response.command == "GAME_SNAPSHOT")
                    {
                        while (snapshots.Count >= MaxQueuedSnapshots)
                            snapshots.Dequeue();
                        snapshots.Enqueue(response);
                    }
                    else if (response.command == "GAME_EVENT")
                    {
                        if (response.gameEvent == null)
                            throw new InvalidDataException("Gameplay event payload is missing.");
                        if (response.gameEvent.eventId > lastEventId)
                        {
                            lastEventId = response.gameEvent.eventId;
                            events.Enqueue(response);
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("Unexpected gameplay stream message.");
                    }
                }
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                CloseConnection();
                Debug.LogWarning("[TCP gameplay] Snapshot stream interrupted: " + ex.GetType().Name);
                try { await Task.Delay(1000, token); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            }
        }
    }

    private static async Task AwaitWithTimeoutAsync(Task task, CancellationToken token, string message)
    {
        token.ThrowIfCancellationRequested();
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            Task delay = Task.Delay(TimeoutMilliseconds, timeout.Token);
            if (await Task.WhenAny(task, delay) != task)
            {
                token.ThrowIfCancellationRequested();
                throw new TimeoutException(message);
            }
            timeout.Cancel();
            await task;
        }
    }

    private async Task SendAsync(Req request)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        if (body.Length <= 0 || body.Length > MaxPacketBytes)
            throw new InvalidDataException("Gameplay request exceeds the 64 KiB protocol limit.");

        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, body.Length);
        await stream.WriteAsync(header, 0, header.Length);
        await stream.WriteAsync(body, 0, body.Length);
        await stream.FlushAsync();
    }

    private async Task<Resp> ReceiveAsync()
    {
        byte[] header = await ReadExactlyAsync(4, allowCleanEof: true);
        if (header == null)
            return null;

        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > MaxPacketBytes)
            throw new InvalidDataException("Gameplay response length is outside the 64 KiB protocol limit.");

        byte[] body = await ReadExactlyAsync(length, allowCleanEof: false);
        Resp response = JsonUtility.FromJson<Resp>(Encoding.UTF8.GetString(body));
        if (response == null)
            throw new InvalidDataException("Gameplay response is not valid JSON.");
        return response;
    }

    private async Task<byte[]> ReadExactlyAsync(int length, bool allowCleanEof)
    {
        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = await stream.ReadAsync(buffer, offset, length - offset);
            if (read == 0)
            {
                if (allowCleanEof && offset == 0)
                    return null;
                throw new EndOfStreamException("Gameplay stream ended during a frame.");
            }
            offset += read;
        }
        return buffer;
    }

    private void CloseConnection()
    {
        Stream oldStream = stream;
        TcpClient oldClient = client;
        stream = null;
        client = null;
        try { oldStream?.Dispose(); } catch { }
        try { oldClient?.Close(); } catch { }
        try { oldClient?.Dispose(); } catch { }
    }

    public void Close()
    {
        if (closed)
            return;
        closed = true;
        stop.Cancel();
        CloseConnection();
        stop.Dispose();
    }
}
