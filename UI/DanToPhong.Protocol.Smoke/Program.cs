using System.Buffers.Binary;
using DanToPhong.Protocol;

await RunAsync();

static async Task RunAsync()
{
    using var frames = new MemoryStream();
    await MessageFramer.SendAsync(frames, new SmokePacket { Command = "FIRST" });
    await MessageFramer.SendAsync(frames, new SmokePacket { Command = "SECOND" });

    using (var fragmented = new OneByteReadStream(frames.ToArray()))
    {
        SmokePacket first = await MessageFramer.ReceiveAsync<SmokePacket>(fragmented)
            ?? throw new InvalidOperationException("The first coalesced frame was not decoded.");
        SmokePacket second = await MessageFramer.ReceiveAsync<SmokePacket>(fragmented)
            ?? throw new InvalidOperationException("The second coalesced frame was not decoded.");
        if (first.Command != "FIRST" || second.Command != "SECOND")
            throw new InvalidOperationException("Frame order or payload changed.");
        if (await MessageFramer.ReceiveAsync<SmokePacket>(fragmented) != null)
            throw new InvalidOperationException("EOF without a header must return null.");
    }

    byte[] oversizedHeader = new byte[4];
    BinaryPrimitives.WriteInt32BigEndian(oversizedHeader, 64 * 1024 + 1);
    await ExpectAsync<InvalidDataException>(
        () => MessageFramer.ReceiveAsync<SmokePacket>(new MemoryStream(oversizedHeader)),
        "oversized length prefix");

    byte[] emptyHeader = new byte[4];
    await ExpectAsync<InvalidDataException>(
        () => MessageFramer.ReceiveAsync<SmokePacket>(new MemoryStream(emptyHeader)),
        "zero length prefix");

    byte[] truncatedFrame = new byte[5];
    BinaryPrimitives.WriteInt32BigEndian(truncatedFrame.AsSpan(0, 4), 3);
    truncatedFrame[4] = (byte)'{';
    await ExpectAsync<EndOfStreamException>(
        () => MessageFramer.ReceiveAsync<SmokePacket>(new MemoryStream(truncatedFrame)),
        "truncated frame body");

    byte[] malformedJson = new byte[5];
    BinaryPrimitives.WriteInt32BigEndian(malformedJson.AsSpan(0, 4), 1);
    malformedJson[4] = (byte)'{';
    await ExpectAsync<System.Text.Json.JsonException>(
        () => MessageFramer.ReceiveAsync<SmokePacket>(new MemoryStream(malformedJson)),
        "malformed JSON body");

    await ExpectAsync<InvalidDataException>(
        () => MessageFramer.SendAsync(frames, new SmokePacket { Command = new string('x', 64 * 1024) }),
        "oversized outgoing payload");

    using var gameplayFrames = new MemoryStream();
    await MessageFramer.SendAsync(gameplayFrames, new ResponsePacket
    {
        Success = true,
        Command = Commands.GameEvent,
        GameEvent = new GameEventDto
        {
            EventId = 7,
            Type = "FINISH",
            UserId = 2,
            Username = "racer2",
            ServerTimestampMs = 123456
        }
    });
    await MessageFramer.SendAsync(gameplayFrames, new ResponsePacket
    {
        Success = true,
        Command = Commands.GameSnapshot,
        ServerTick = 42,
        ServerTimestampMs = 123457
    });
    gameplayFrames.Position = 0;
    ResponsePacket orderedEvent = await MessageFramer.ReceiveAsync<ResponsePacket>(gameplayFrames)
        ?? throw new InvalidOperationException("The ordered game event was not decoded.");
    ResponsePacket followingSnapshot = await MessageFramer.ReceiveAsync<ResponsePacket>(gameplayFrames)
        ?? throw new InvalidOperationException("The snapshot after the game event was not decoded.");
    GameEventDto? eventPayload = orderedEvent.GameEvent;
    if (orderedEvent.Command != Commands.GameEvent ||
        eventPayload == null ||
        eventPayload.EventId != 7 ||
        followingSnapshot.Command != Commands.GameSnapshot ||
        followingSnapshot.ServerTick != 42 ||
        eventPayload.ServerTimestampMs >= followingSnapshot.ServerTimestampMs)
        throw new InvalidOperationException("Game events and snapshots did not preserve their ordered wire format.");

    Console.WriteLine("PASS: fragmented reads and coalesced frames preserve order.");
    Console.WriteLine("PASS: clean EOF, invalid/oversized length, malformed JSON, truncated frame, and oversized output are handled.");
    Console.WriteLine("PASS: ordered game-event and timestamped snapshot frames preserve the TCP wire order.");
}

static async Task ExpectAsync<TException>(Func<Task> action, string testName)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}: {testName}.");
}

internal sealed class SmokePacket
{
    public string Command { get; set; } = string.Empty;
}

internal sealed class OneByteReadStream : Stream
{
    private readonly MemoryStream _inner;

    public OneByteReadStream(byte[] content) => _inner = new MemoryStream(content, writable: false);

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, Math.Min(count, 1));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _inner.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}
