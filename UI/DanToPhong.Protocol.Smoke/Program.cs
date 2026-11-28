using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DanToPhong.Protocol;
using DanToPhong.Server;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

await RunAsync(args);

static async Task RunAsync(string[] args)
{
    bool sendSmtp4DevSample = args.Length == 1 && args[0] == "--smtp4dev-sample";
    bool sendQueuedOtpToSmtp4Dev = args.Length == 1 && args[0] == "--smtp4dev-otp-queue";
    if (args.Length > 0 && !sendSmtp4DevSample && !sendQueuedOtpToSmtp4Dev)
        throw new ArgumentException("Supported options are --smtp4dev-sample and --smtp4dev-otp-queue.");

    VerifyAccountInputValidation();
    VerifyMapAllowlist();
    VerifyCertificatePinValidation();
    VerifyClientEliminationDoesNotDeclareWinner();
    VerifyRoomCapacityAndReadiness();
    VerifyRoomReturnToLobbyAndLeaveEvents();
    await VerifyGameClientMailboxAsync();
    await VerifyEmailQueueAsync();

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
    Console.WriteLine("PASS: forged UserId/TargetId with ELIMINATE affects only the caller; opponent state and winner remain unchanged.");
    Console.WriteLine("PASS: rooms are capped at four players and the server blocks START until every player reports the lobby scene.");
    Console.WriteLine("PASS: room return preserves membership until everyone returns; reset, readiness, roster, and leave events are synchronized.");
    Console.WriteLine("PASS: per-client mailbox waits for important events and drops stale snapshots.");
    Console.WriteLine("PASS: shared account validation accepts the email limit and rejects overlong email/password values.");
    Console.WriteLine("PASS: SHA-256 certificate pinning accepts the exact self-signed certificate and rejects a wrong pin, hostname mismatch, and malformed pin.");
    Console.WriteLine("PASS: bounded OTP email worker retries SMTP 4xx/5xx responses.");

    if (sendSmtp4DevSample)
        await SendSmtp4DevSampleAsync();
    if (sendQueuedOtpToSmtp4Dev)
        await SendQueuedOtpToSmtp4DevAsync();
}

static void VerifyAccountInputValidation()
{
    string maxLengthEmail = new string('a', 88) + "@example.com";
    string overlongEmail = new string('a', 89) + "@example.com";
    string maxLengthPassword = new string('A', AccountInputValidation.MaxPasswordLength - 1) + "1";
    if (!AccountInputValidation.IsValidEmail(maxLengthEmail) ||
        AccountInputValidation.IsValidEmail(overlongEmail) ||
        AccountInputValidation.IsValidEmail("not-an-email") ||
        !AccountInputValidation.IsValidPassword("Abcdefg1") ||
        !AccountInputValidation.IsValidPassword(maxLengthPassword) ||
        AccountInputValidation.IsValidPassword(maxLengthPassword + "2"))
        throw new InvalidOperationException("Account input length or format validation is inconsistent.");
}

static void VerifyMapAllowlist()
{
    if (!MapNames.IsAllowed(MapNames.Game1) ||
        !MapNames.IsAllowed(MapNames.Game2) ||
        !MapNames.IsAllowed(MapNames.Game3) ||
        MapNames.IsAllowed("game") ||
        MapNames.IsAllowed("game 2") ||
        MapNames.IsAllowed("custom"))
        throw new InvalidOperationException("The server map allowlist does not match the three supported race scenes.");
}

static void VerifyCertificatePinValidation()
{
    using RSA key = RSA.Create(2048);
    var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
    subjectAlternativeNames.AddDnsName("localhost");
    subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
    request.CertificateExtensions.Add(subjectAlternativeNames.Build());

    DateTimeOffset now = DateTimeOffset.UtcNow;
    using X509Certificate2 certificate = request.CreateSelfSigned(now.AddMinutes(-1), now.AddDays(1));
    using var chain = new X509Chain();
    _ = chain.Build(certificate);
    using SHA256 sha256 = SHA256.Create();
    string pin = BitConverter.ToString(sha256.ComputeHash(certificate.GetRawCertData())).Replace("-", string.Empty);
    string formattedPin = string.Join(":", Enumerable.Range(0, pin.Length / 2).Select(index => pin.Substring(index * 2, 2)));

    if (!CertificatePinValidator.IsValidSha256Thumbprint(formattedPin) ||
        CertificatePinValidator.NormalizeSha256Thumbprint(formattedPin) != pin ||
        !CertificatePinValidator.Validate(certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors, pin) ||
        !CertificatePinValidator.Validate(certificate, chain, SslPolicyErrors.None, pin) ||
        CertificatePinValidator.Validate(certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors, new string('0', 64)) ||
        CertificatePinValidator.Validate(certificate, chain,
            SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch, pin) ||
        CertificatePinValidator.IsValidSha256Thumbprint("not-a-thumbprint"))
        throw new InvalidOperationException("Certificate pinning accepted an invalid pin, chain error, or hostname mismatch.");
}

static void VerifyClientEliminationDoesNotDeclareWinner()
{
    const int hostUserId = 900001;
    const int guestUserId = 900002;
    ResponsePacket created = TcpGameRoomRegistry.Create(hostUserId, "smoke-host",
        new RequestPacket { MapName = "game1", MaxPlayers = 2 });
    if (!created.Success || string.IsNullOrWhiteSpace(created.RoomId))
        throw new InvalidOperationException("The gameplay smoke room could not be created.");

    try
    {
        if (!TcpGameRoomRegistry.Join(guestUserId, "smoke-guest", created.RoomId).Success ||
            !TcpGameRoomRegistry.ReportScene(hostUserId, "LobbyScene").Success ||
            !TcpGameRoomRegistry.ReportScene(guestUserId, "LobbyScene").Success ||
            !TcpGameRoomRegistry.Start(hostUserId, new RequestPacket { MapName = "game1" }).Success)
            throw new InvalidOperationException("The gameplay smoke room could not be started.");

        ResponsePacket elimination = TcpGameRoomRegistry.SubmitEvent(hostUserId,
            new RequestPacket
            {
                UserId = guestUserId,
                TargetId = "smoke-guest",
                GameEventType = "ELIMINATE"
            });
        if (!elimination.Success || elimination.GameFinished || elimination.WinnerUserId.HasValue ||
            elimination.GamePlayers == null ||
            !elimination.GamePlayers.Any(player => player.UserId == hostUserId && player.IsEliminated) ||
            elimination.GamePlayers.Any(player =>
                player.UserId == guestUserId && (player.IsEliminated || player.IsFinished)))
            throw new InvalidOperationException("A client elimination request changed another player's state.");
    }
    finally
    {
        TcpGameRoomRegistry.Leave(hostUserId);
        TcpGameRoomRegistry.Leave(guestUserId);
    }
}

static void VerifyRoomCapacityAndReadiness()
{
    const int hostId = 901001;
    const int guestOneId = 901002;
    const int guestTwoId = 901003;
    const int guestThreeId = 901004;
    const int extraGuestId = 901005;
    ResponsePacket created = TcpGameRoomRegistry.Create(hostId, "capacity-host",
        new RequestPacket { MapName = "game1", MaxPlayers = 6 });
    if (!created.Success || created.MaxPlayers != 4 || created.CanStart)
        throw new InvalidOperationException("The server did not cap a room at four players.");

    try
    {
        string roomCode = created.RoomId!;
        if (!TcpGameRoomRegistry.ReportScene(hostId, "LobbyScene").Success ||
            TcpGameRoomRegistry.Start(hostId, new RequestPacket()).Success)
            throw new InvalidOperationException("The server allowed a race to start without enough ready players.");

        if (!TcpGameRoomRegistry.Join(guestOneId, "capacity-guest-1", roomCode).Success ||
            !TcpGameRoomRegistry.Join(guestTwoId, "capacity-guest-2", roomCode).Success ||
            !TcpGameRoomRegistry.Join(guestThreeId, "capacity-guest-3", roomCode).Success ||
            TcpGameRoomRegistry.Join(extraGuestId, "capacity-guest-4", roomCode).Success ||
            TcpGameRoomRegistry.CanInvite(hostId, extraGuestId, roomCode))
            throw new InvalidOperationException("The room accepted more than four players or invitations after reaching capacity.");

        foreach (int userId in new[] { hostId, guestOneId, guestTwoId, guestThreeId })
        {
            if (!TcpGameRoomRegistry.ReportScene(userId, "LobbyScene").Success)
                throw new InvalidOperationException("The server rejected a valid lobby scene report.");
        }

        ResponsePacket ready = TcpGameRoomRegistry.State(hostId);
        if (!ready.CanStart || ready.ReadyPlayerCount != 4 || ready.UnreadyPlayerCount != 0 ||
            !TcpGameRoomRegistry.Start(hostId, new RequestPacket { MapName = "game1" }).Success)
            throw new InvalidOperationException("All ready players were not allowed to start the race.");
    }
    finally
    {
        TcpGameRoomRegistry.Leave(hostId);
        TcpGameRoomRegistry.Leave(guestOneId);
        TcpGameRoomRegistry.Leave(guestTwoId);
        TcpGameRoomRegistry.Leave(guestThreeId);
        TcpGameRoomRegistry.Leave(extraGuestId);
    }
}

static void VerifyRoomReturnToLobbyAndLeaveEvents()
{
    if (TcpGameRoomRegistry.WinnerRewardCoins != 50)
        throw new InvalidOperationException("The multiplayer winner reward must be exactly 50 coins.");

    const int hostId = 902001;
    const int guestId = 902002;
    ResponsePacket created = TcpGameRoomRegistry.Create(hostId, "roundtrip-host",
        new RequestPacket { MapName = "game1", MaxPlayers = 2 });
    if (!created.Success || string.IsNullOrWhiteSpace(created.RoomId))
        throw new InvalidOperationException("The room round-trip smoke room could not be created.");

    try
    {
        string roomCode = created.RoomId!;
        if (!TcpGameRoomRegistry.Join(guestId, "roundtrip-guest", roomCode).Success ||
            !TcpGameRoomRegistry.ReportScene(hostId, "LobbyScene").Success ||
            !TcpGameRoomRegistry.ReportScene(guestId, "LobbyScene").Success ||
            !TcpGameRoomRegistry.Start(hostId, new RequestPacket { MapName = "game1" }).Success)
            throw new InvalidOperationException("Two ready players could not start a match.");

        if (!TcpGameRoomRegistry.ListRooms().Any(room =>
                room.Code == roomCode && room.CurrentPlayers == 2 && room.MaxPlayers == 2))
            throw new InvalidOperationException("The open-room list did not expose the active room roster.");

        if (!TcpGameRoomRegistry.ReturnToLobby(hostId).Success ||
            !TcpGameRoomRegistry.ReportScene(hostId, "LobbyScene").Success)
            throw new InvalidOperationException("The host could not return to the persistent room lobby.");

        ResponsePacket waiting = TcpGameRoomRegistry.State(hostId);
        if (!waiting.IsRacing || !waiting.IsReturnedToLobby ||
            waiting.GameEvent?.Type != "PLAYER_RETURNED_TO_LOBBY")
            throw new InvalidOperationException("The room did not retain the early-returning player or publish the lobby event.");

        if (!TcpGameRoomRegistry.ReturnToLobby(guestId).Success ||
            !TcpGameRoomRegistry.ReportScene(guestId, "LobbyScene").Success)
            throw new InvalidOperationException("The guest could not return to the persistent room lobby.");

        ResponsePacket readyAgain = TcpGameRoomRegistry.State(hostId);
        if (readyAgain.IsRacing || !readyAgain.CanStart || readyAgain.PlayerCount != 2 ||
            readyAgain.GameEvent?.Type != "ROOM_RESET")
            throw new InvalidOperationException("The room did not reset with both players ready for another match.");

        if (!TcpGameRoomRegistry.Leave(guestId).Success)
            throw new InvalidOperationException("The guest could not leave the room.");
        ResponsePacket afterLeave = TcpGameRoomRegistry.State(hostId);
        if (afterLeave.PlayerCount != 1 || afterLeave.GameEvent?.Type != "PLAYER_LEFT" ||
            afterLeave.GameEvent.Username != "roundtrip-guest")
            throw new InvalidOperationException("The room did not publish the guest-leave event.");
    }
    finally
    {
        TcpGameRoomRegistry.Leave(hostId);
        TcpGameRoomRegistry.Leave(guestId);
    }
}

static async Task VerifyGameClientMailboxAsync()
{
    using var mailbox = new GameClientMailbox(eventCapacity: 1);
    var firstEvent = new ResponsePacket { Command = Commands.GameEvent, ServerTick = 1 };
    var secondEvent = new ResponsePacket { Command = Commands.GameEvent, ServerTick = 2 };

    await mailbox.QueueImportantEventAsync(firstEvent, CancellationToken.None);
    ValueTask pendingEvent = mailbox.QueueImportantEventAsync(secondEvent, CancellationToken.None);
    if (pendingEvent.IsCompleted)
        throw new InvalidOperationException("A full important-event mailbox must wait for capacity.");
    if (!mailbox.TryReadImportantEvent(out ResponsePacket queuedEvent) || queuedEvent.ServerTick != 1)
        throw new InvalidOperationException("The first important event was not retained.");
    await pendingEvent;
    if (!mailbox.TryReadImportantEvent(out queuedEvent) || queuedEvent.ServerTick != 2)
        throw new InvalidOperationException("The waiting important event was not retained.");

    mailbox.TryQueueSnapshot(new ResponsePacket { Command = Commands.GameSnapshot, ServerTick = 10 });
    mailbox.TryQueueSnapshot(new ResponsePacket { Command = Commands.GameSnapshot, ServerTick = 11 });
    if (!mailbox.TryReadSnapshot(out ResponsePacket snapshot) || snapshot.ServerTick != 11)
        throw new InvalidOperationException("The snapshot mailbox did not discard the stale state.");
}

static async Task VerifyEmailQueueAsync()
{
    int attempts = 0;
    var queue = new EmailVerificationQueue(
        capacity: 1,
        sendCode: (_, _) =>
        {
            int currentAttempt = Interlocked.Increment(ref attempts);
            if (currentAttempt == 1)
                throw new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, (SmtpStatusCode)451, "temporary failure");
            if (currentAttempt == 2)
                throw new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, (SmtpStatusCode)550, "temporary failure");
            return Task.CompletedTask;
        },
        retryDelay: TimeSpan.Zero);

    if (!queue.TryReserve(out EmailVerificationQueue.Reservation? reservation) || reservation is null)
        throw new InvalidOperationException("The email queue did not reserve available capacity.");
    if (queue.TryReserve(out _))
        throw new InvalidOperationException("The bounded email queue accepted work beyond its capacity.");

    using (reservation)
        reservation.Enqueue("racer@example.invalid", "123456", passwordReset: false);

    queue.Complete();
    await queue.RunAsync(CancellationToken.None);
    if (attempts != 3)
        throw new InvalidOperationException($"Expected retries for SMTP 4xx/5xx responses; observed {attempts} send attempts.");
}

static async Task SendSmtp4DevSampleAsync()
{
    using var protocolTrace = new MemoryStream();
    using var logger = new ProtocolLogger(protocolTrace, leaveOpen: true)
    {
        RedactSecrets = true
    };
    using var client = new SmtpClient(logger)
    {
        Timeout = 10000
    };

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("DanToPhong SMTP sample", "test@dantophong.invalid"));
    message.To.Add(new MailboxAddress("SMTP Sample", "smtp-sample@example.invalid"));
    message.Subject = "SMTP sample: Vietnamese text and attachment";

    using var attachmentContent = new MemoryStream(Encoding.UTF8.GetBytes("Tệp đính kèm mẫu cho kiểm thử SMTP.\n"));
    var attachment = new MimePart("text", "plain")
    {
        Content = new MimeContent(attachmentContent),
        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
        ContentTransferEncoding = ContentEncoding.Base64,
        FileName = "tep-mau.txt"
    };
    message.Body = new Multipart("mixed")
    {
        new TextPart("plain") { Text = "Xin chào, đây là email mẫu kiểm thử SMTP của DanToPhong." },
        attachment
    };

    await client.ConnectAsync("127.0.0.1", 2525, SecureSocketOptions.None);
    await client.SendAsync(FormatOptions.Default, new SmtpSendRequest(message), CancellationToken.None);
    await client.DisconnectAsync(true);

    string trace = Encoding.UTF8.GetString(protocolTrace.ToArray());
    if (!logger.RedactSecrets ||
        !trace.Contains("Content-Disposition: attachment", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("The sample SMTP trace did not confirm redacted logging and attachment serialization.");

    Console.WriteLine("PASS: synthetic Vietnamese email with attachment was accepted by local smtp4dev.");
    Console.WriteLine("PASS: ProtocolLogger.RedactSecrets was enabled; trace stayed in memory and no SMTP credentials were used.");
}

static async Task SendQueuedOtpToSmtp4DevAsync()
{
    Environment.SetEnvironmentVariable("DANTOPHONG_SMTP_MODE", "smtp4dev");
    Environment.SetEnvironmentVariable("DANTOPHONG_SMTP_HOST", "127.0.0.1");
    Environment.SetEnvironmentVariable("DANTOPHONG_SMTP_PORT", "2525");

    bool delivered = false;
    var queue = new EmailVerificationQueue(
        capacity: 1,
        sendCode: async (job, token) =>
        {
            await EmailVerificationService.SendCodeAsync(
                job.Email, job.Code, job.PasswordReset, token);
            delivered = true;
        },
        retryDelay: TimeSpan.Zero);
    if (!queue.TryReserve(out EmailVerificationQueue.Reservation? reservation) ||
        reservation is null)
        throw new InvalidOperationException("The email queue did not reserve available capacity.");

    using (reservation)
        reservation.Enqueue(
            "queue-worker-test@example.invalid",
            RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString(CultureInfo.InvariantCulture),
            passwordReset: false);

    queue.Complete();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await queue.RunAsync(timeout.Token);
    if (!delivered)
        throw new InvalidOperationException("The queued OTP email was not accepted by smtp4dev.");

    Console.WriteLine("PASS: bounded OTP email worker delivered synthetic content to loopback smtp4dev.");
    Console.WriteLine("PASS: no SMTP credentials or external recipient were used.");
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
