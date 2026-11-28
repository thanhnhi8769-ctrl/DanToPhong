using System.Collections.Concurrent;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using DanToPhong.Protocol;
using DanToPhong.Server;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if (args.Length == 1 && args[0] == "--tc05-pinned")
            return await RunTc05PinnedAsync();
        if (args.Length != 0)
            throw new ArgumentException("Supported option: --tc05-pinned.");
        string root = FindRepositoryRoot();
        string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string evidencePath = Path.Combine(root, "docs", "evidence", $"local-protocol-integration-{stamp}.log");
        using var log = new EvidenceLog(evidencePath);
        log.Write("INFO", "Bounded DB-backed TCP integration run; protocol evidence only, not GUI or cross-network evidence.");

        TestAccounts? accounts = null;
        Exception? failure = null;
        bool blocked = false;
        string? roomIdLostOnRestart = null;

        try
        {
            string connectionString = ResolveTestConnectionString();
            string serverAssembly = ResolveServerAssembly(root);
            await RunSecurityStateChecksAsync(connectionString, log);
            accounts = new TestAccounts();

            await using (TestServer server = await TestServer.StartAsync(serverAssembly, connectionString))
            {
                log.Write("PASS", $"T1 test server was observed listening on dedicated loopback port {server.Port}.");
                await accounts.SeedAsync(connectionString);
                _ = await RunDisconnectPhaseAsync(server, accounts, connectionString, log);
                await server.StopAbruptlyAsync();
            }

            await SetSelectedMapAsync(connectionString, accounts.A, "game 1");
            await SetSelectedMapAsync(connectionString, accounts.B, "game 2");
            await SetSelectedMapAsync(connectionString, accounts.C, "game 3");
            await SetSelectedMapAsync(connectionString, accounts.D, "game");
            await SetSelectedMapAsync(connectionString, accounts.E, "unsupported-map");
            await using (TestServer server = await TestServer.StartAsync(serverAssembly, connectionString))
            {
                await VerifySelectedMapMigrationAsync(connectionString, accounts, log);
                roomIdLostOnRestart = await RunShutdownPhaseAsync(server, accounts, log);
                await server.StopAbruptlyAsync();
            }

            await using (TestServer server = await TestServer.StartAsync(serverAssembly, connectionString))
            {
                string roomId = roomIdLostOnRestart
                    ?? throw new TestFailureException("The pre-restart test room was not recorded.");
                await VerifyRestartDropsRoomStateAsync(server, accounts.A, roomId, log);
                await RunLoadAndFramingPhaseAsync(server, log);
                await server.StopAbruptlyAsync();
            }
        }
        catch (TestBlockedException ex)
        {
            blocked = true;
            log.Write("BLOCKED", ex.Message);
        }
        catch (Exception ex)
        {
            failure = ex;
            string detail = ex is TestFailureException
                ? ex.Message
                : $"Unexpected {ex.GetType().Name}; diagnostic details withheld.";
            log.Write("FAIL", detail);
        }
        finally
        {
            if (accounts?.HasCreatedAccounts == true)
            {
                try
                {
                    await accounts.CleanupAsync(
                        Environment.GetEnvironmentVariable("DANTOPHONG_DB_CONNECTION")
                        ?? throw new InvalidOperationException("The test DB connection environment variable is unavailable."));
                    log.Write("PASS", "Temporary protocol-test accounts were removed from DanToPhongDB_Test.");
                }
                catch (Exception ex)
                {
                    failure ??= ex;
                    log.Write("FAIL", $"Temporary account cleanup failed ({ex.GetType().Name}).");
                }
            }
        }

        if (failure != null)
        {
            log.Write("RESULT", "FAIL");
            return 1;
        }
        if (blocked)
        {
            log.Write("RESULT", "BLOCKED");
            return 2;
        }

        log.Write("RESULT", "PASS — OTP/login-state, room/snapshot, reconnect, shutdown, and bounded load checks passed; GUI/cross-network checks remain manual.");
        return 0;
    }

    private static async Task<int> RunTc05PinnedAsync()
    {
        string root = FindRepositoryRoot();
        string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string evidencePath = Path.Combine(root, "docs", "evidence", $"TC05-pinned-local-{stamp}.log");
        using var log = new EvidenceLog(evidencePath);
        log.Write("INFO", "TC05 transport checks on dedicated loopback ports; no GUI or remote-network claim.");
        string? certificatePath = null;
        try
        {
            string connectionString = ResolveTestConnectionString();
            string serverAssembly = ResolveServerAssembly(root);
            using RSA key = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            DateTimeOffset now = DateTimeOffset.UtcNow;
            using X509Certificate2 certificate = request.CreateSelfSigned(now.AddMinutes(-1), now.AddDays(1));
            certificatePath = Path.Combine(Path.GetTempPath(), $"DanToPhong-tc05-{Guid.NewGuid():N}.pfx");
            string certificatePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            byte[] pfx = certificate.Export(X509ContentType.Pkcs12, certificatePassword);
            await using (var certificateFile = new FileStream(certificatePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await certificateFile.WriteAsync(pfx);
            using SHA256 sha256 = SHA256.Create();
            string pin = BitConverter.ToString(sha256.ComputeHash(certificate.GetRawCertData())).Replace("-", string.Empty);
            log.Write("INFO", "Ephemeral localhost SAN certificate generated outside the repository; thumbprint omitted.");

            await using (TestServer server = await TestServer.StartAsync(
                serverAssembly, connectionString, requestedPort: 19000, useTls: true,
                certificatePath: certificatePath, certificatePassword: certificatePassword, secondaryPort: 19001))
            {
                await server.WaitForSecondaryListenerAsync(19001, TimeSpan.FromSeconds(5));
                ResponsePacket primary = await SendTlsPingAsync(19000, pin);
                VerifyUnauthorizedPing(primary);
                log.Write("PASS", "TLS client with exact SHA-256 pin completed TLS 1.2/1.3 and received the expected unauthenticated PING response on port 19000.");

                ResponsePacket secondary = await SendTlsPingAsync(19001, pin);
                VerifyUnauthorizedPing(secondary);
                log.Write("PASS", "The optional secondary TLS listener accepted the same pinned certificate and framed protocol on port 19001.");

                await VerifyMismatchedPinRejectedAsync(19000, pin);
                log.Write("PASS", "A TLS client with a mismatched certificate pin was rejected during handshake.");

                await VerifyPlaintextRejectedAsync(19000);
                log.Write("PASS", "Plaintext framed PING received no application response from the TLS listener.");
                await server.StopAbruptlyAsync();
            }

            await using (TestServer server = await TestServer.StartAsync(
                serverAssembly, connectionString, requestedPort: 19000, useTls: false))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, 19000, timeout.Token);
                await MessageFramer.SendAsync(client.GetStream(), new RequestPacket { Command = Commands.Ping }, timeout.Token);
                ResponsePacket response = await MessageFramer.ReceiveAsync<ResponsePacket>(client.GetStream(), timeout.Token)
                    ?? throw new TestFailureException("The explicitly plaintext test server closed before responding.");
                VerifyUnauthorizedPing(response);
                log.Write("PASS", "Explicitly enabled plaintext mode on loopback port 19000 returned the expected framed PING response.");
                await server.StopAbruptlyAsync();
            }

            log.Write("RESULT", "PASS — all four TC05 transport cases observed; TLS pinning is required in Launcher and Unity clients.");
            return 0;
        }
        catch (TestBlockedException ex)
        {
            log.Write("BLOCKED", ex.Message);
            log.Write("RESULT", "BLOCKED");
            return 2;
        }
        catch (Exception ex)
        {
            string detail = ex is TestFailureException ? ex.Message : $"Unexpected {ex.GetType().Name}; diagnostic details withheld.";
            log.Write("FAIL", detail);
            log.Write("RESULT", "FAIL");
            return 1;
        }
        finally
        {
            if (certificatePath != null && File.Exists(certificatePath))
            {
                try
                {
                    File.Delete(certificatePath);
                }
                catch (Exception ex)
                {
                    log.Write("WARN", $"Temporary TLS certificate cleanup failed ({ex.GetType().Name}); remove the exact test artifact manually.");
                }
            }
        }
    }

    private static void VerifyUnauthorizedPing(ResponsePacket response)
    {
        if (response.Success || response.Code != ErrorCodes.Unauthorized)
            throw new TestFailureException("A pinned or plaintext PING did not reach the expected unauthenticated application response.");
    }

    private static async Task<ResponsePacket> SendTlsPingAsync(int port, string pin)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false,
            (_, certificate, chain, errors) => CertificatePinValidator.Validate(certificate, chain, errors, pin));
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }, timeout.Token);
        await MessageFramer.SendAsync(ssl, new RequestPacket { Command = Commands.Ping }, timeout.Token);
        return await MessageFramer.ReceiveAsync<ResponsePacket>(ssl, timeout.Token)
            ?? throw new TestFailureException("The pinned TLS connection closed before its application response.");
    }

    private static async Task VerifyMismatchedPinRejectedAsync(int port, string pin)
    {
        string wrongPin = new string(pin[0] == '0' ? '1' : '0', 64);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false,
            (_, certificate, chain, errors) => CertificatePinValidator.Validate(certificate, chain, errors, wrongPin));
        try
        {
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeout.Token);
        }
        catch (AuthenticationException)
        {
            return;
        }
        throw new TestFailureException("The TLS handshake succeeded with a mismatched certificate pin.");
    }

    private static async Task VerifyPlaintextRejectedAsync(int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        NetworkStream stream = client.GetStream();
        try
        {
            await MessageFramer.SendAsync(stream, new RequestPacket { Command = Commands.Ping }, timeout.Token);
            byte[] response = new byte[1];
            int received = await stream.ReadAsync(response, timeout.Token);
            if (received != 0)
                throw new TestFailureException("A TLS listener returned application bytes to a plaintext client.");
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
        }
    }

    private static async Task RunSecurityStateChecksAsync(string connectionString, EvidenceLog log)
    {
        var state = new ServerStateRepository(connectionString);
        string wrongCodeLockKey = $"harness|otp-lock|{Guid.NewGuid():N}";
        string expiredCodeKey = $"harness|otp-expired|{Guid.NewGuid():N}";
        string consumedCodeKey = $"harness|otp-consume|{Guid.NewGuid():N}";
        string loginLimiterKey = $"harness|login|{Guid.NewGuid():N}";
        string[] verificationKeys = { wrongCodeLockKey, expiredCodeKey, consumedCodeKey };

        try
        {
            string validCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            string wrongCode = validCode == "999999" ? "100000" : (int.Parse(validCode, CultureInfo.InvariantCulture) + 1).ToString("D6", CultureInfo.InvariantCulture);
            if (!await state.TryReserveVerificationAsync(wrongCodeLockKey, DateTime.UtcNow.AddMinutes(1), CancellationToken.None))
                throw new TestFailureException("OTP test reservation unexpectedly hit a cooldown.");
            await state.SaveVerificationCodeAsync(wrongCodeLockKey, validCode, DateTime.UtcNow.AddMinutes(10), CancellationToken.None);
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (await state.VerifyAndConsumeCodeAsync(wrongCodeLockKey, wrongCode, CancellationToken.None))
                    throw new TestFailureException("OTP accepted a wrong verification code.");
            }
            if (await state.VerifyAndConsumeCodeAsync(wrongCodeLockKey, validCode, CancellationToken.None))
                throw new TestFailureException("OTP accepted a correct code after five failed attempts.");
            log.Write("PASS", "TC04 OTP repository rejected wrong codes and locked verification after five attempts; code values were not logged.");

            if (!await state.TryReserveVerificationAsync(expiredCodeKey, DateTime.UtcNow.AddMinutes(1), CancellationToken.None))
                throw new TestFailureException("Expired OTP test reservation unexpectedly hit a cooldown.");
            string expiredCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            await state.SaveVerificationCodeAsync(expiredCodeKey, expiredCode, DateTime.UtcNow.AddSeconds(-1), CancellationToken.None);
            if (await state.VerifyAndConsumeCodeAsync(expiredCodeKey, expiredCode, CancellationToken.None))
                throw new TestFailureException("OTP accepted an expired verification code.");

            if (!await state.TryReserveVerificationAsync(consumedCodeKey, DateTime.UtcNow.AddMinutes(1), CancellationToken.None))
                throw new TestFailureException("Valid OTP test reservation unexpectedly hit a cooldown.");
            string singleUseCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            await state.SaveVerificationCodeAsync(consumedCodeKey, singleUseCode, DateTime.UtcNow.AddMinutes(10), CancellationToken.None);
            if (!await state.VerifyAndConsumeCodeAsync(consumedCodeKey, singleUseCode, CancellationToken.None) ||
                await state.VerifyAndConsumeCodeAsync(consumedCodeKey, singleUseCode, CancellationToken.None))
                throw new TestFailureException("OTP did not accept a valid code exactly once.");
            log.Write("PASS", "TC04 OTP repository rejected an expired code and consumed a valid code exactly once.");

            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (!await state.TryBeginLoginAttemptAsync(
                        loginLimiterKey, 5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), CancellationToken.None))
                    throw new TestFailureException("Login limiter locked before the configured attempt limit.");
                await state.RecordFailedLoginAsync(loginLimiterKey, 5, TimeSpan.FromMinutes(15), CancellationToken.None);
            }
            if (await state.TryBeginLoginAttemptAsync(
                    loginLimiterKey, 5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), CancellationToken.None))
                throw new TestFailureException("Login limiter allowed an attempt after reaching its configured limit.");
            log.Write("PASS", "Login limiter allowed five attempts then rejected the next attempt.");
        }
        finally
        {
            foreach (string key in verificationKeys)
                await DeleteVerificationRowAsync(connectionString, key);
            await state.ClearLoginAttemptsAsync(loginLimiterKey, CancellationToken.None);
        }
        log.Write("PASS", "Temporary OTP and login-limiter database rows were removed.");
    }

    private static async Task DeleteVerificationRowAsync(string connectionString, string key)
    {
        string hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE dbo.VerificationCodes WHERE KeyHash=@hash;";
        command.Parameters.Add("@hash", System.Data.SqlDbType.Char, 64).Value = hash;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> RunDisconnectPhaseAsync(
        TestServer server,
        TestAccounts accounts,
        string connectionString,
        EvidenceLog log)
    {
        await using var clientA = new TestTcpClient("A");
        await using var clientB = new TestTcpClient("B");
        await using var clientC = new TestTcpClient("C");
        await using var clientD = new TestTcpClient("D");
        await clientA.ConnectAsync(server.Port);
        await clientB.ConnectAsync(server.Port);
        await clientC.ConnectAsync(server.Port);
        await clientD.ConnectAsync(server.Port);
        await clientA.LoginAsync(accounts.A);
        await clientB.LoginAsync(accounts.B);
        await clientC.LoginAsync(accounts.C);
        await clientD.LoginAsync(accounts.D);
        await VerifyAccountSettingsAsync(clientA, accounts.A, server.Port, connectionString, log);
        await VerifySoloWinRewardAsync(clientA, log);
        await VerifyMultiplayerRewardPersistenceAsync(clientA, accounts.A.Username, connectionString, log);
        log.Write("PASS", "T2 authenticated login responses returned each account email persisted in SQL.");
        await server.WaitForAcceptedClientsAsync(4, TimeSpan.FromSeconds(5));
        log.Write("PASS", "T2 four TCP clients connected, authenticated, and appeared in server session logs.");

        TimeSpan latencyA = await MeasurePingAsync(clientA);
        TimeSpan latencyB = await MeasurePingAsync(clientB);
        log.Write("PASS", $"T3 both authenticated clients received PONG in under 1 second (A {latencyA.TotalMilliseconds:F1} ms; B {latencyB.TotalMilliseconds:F1} ms).");

        string roomId = await CreateAndStartRoomAsync(clientA, clientB, clientC, clientD);
        await using var gameStreamB = await GameStreamClient.ConnectAsync(server.Port, clientB.Token);
        ResponsePacket initialSnapshot = await gameStreamB.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == roomId && snapshot.GamePlayers?.Count == 4,
            TimeSpan.FromSeconds(5));
        if (!initialSnapshot.IsRacing ||
            initialSnapshot.ServerTick <= 0 ||
            initialSnapshot.ServerTimestampMs <= 0 ||
            initialSnapshot.GamePlayers!.Any(player =>
                player.Sequence != initialSnapshot.ServerTick || player.ServerTimestampMs <= 0))
            throw new TestFailureException("TC03 initial room snapshot omitted race state, sequence, or timestamp.");
        log.Write("PASS", "TC03 four authenticated clients created/joined a room and received sequenced, timestamped server snapshots.");

        await VerifyAuthoritativeInputAsync(clientA, gameStreamB, clientA.UserId, initialSnapshot, log);
        await VerifyFourPlayerUdpMovementAsync(
            server,
            new[] { clientA, clientB, clientC, clientD },
            gameStreamB,
            roomId,
            log);

        await using var clientE = new TestTcpClient("E");
        await clientE.ConnectAsync(server.Port);
        await clientE.LoginAsync(accounts.E);
        ResponsePacket spectate = await clientE.SendAsync(new RequestPacket
        {
            Command = Commands.GameSpectate,
            RoomId = roomId
        });
        if (!spectate.Success || !spectate.IsSpectator || spectate.PlayerCount != 4)
            throw new TestFailureException("TC03 an additional authenticated client could not spectate without becoming a racer.");
        await using var gameStreamE = await GameStreamClient.ConnectAsync(server.Port, clientE.Token);
        ResponsePacket spectatorSnapshot = await gameStreamE.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == roomId && snapshot.IsSpectator && snapshot.GamePlayers?.Count == 4,
            TimeSpan.FromSeconds(5));
        if (!spectatorSnapshot.IsRacing)
            throw new TestFailureException("TC03 spectator stream did not receive the live authoritative snapshot.");
        log.Write("PASS", "TC03 an additional authenticated client spectated the four-racer room and received live snapshots.");

        TimeSpan messageLatency = await SendAndObserveChatAsync(clientA, clientB);
        log.Write("PASS", $"T4 client B received client A's chat message in under 1 second ({messageLatency.TotalMilliseconds:F1} ms).");

        string[] sentMessages = await SendTwentyMessagesAsync(clientA);
        ResponsePacket received = await clientB.SendAsync(
            new RequestPacket { Command = Commands.ChatPoll, AfterMessageId = 0 });
        VerifyTwentyMessages(sentMessages, received);
        log.Write("PASS", "T5 all 20 consecutive messages arrived with consecutive IDs and unchanged content.");

        const string vietnamese = "Xin chào, tiếng Việt có đủ dấu: ă â đ ê ô ơ ư.";
        if (!received.ChatMessages!.Any(message => message.Message == vietnamese))
            throw new TestFailureException("T6 the Vietnamese chat message did not round-trip exactly.");
        log.Write("PASS", "T6 the Vietnamese chat message round-tripped exactly through server storage and TCP framing.");

        clientA.AbortAbruptly();
        bool disconnectedEvent = await gameStreamB.WaitForDisconnectEventAndSnapshotAsync(
            clientA.UserId,
            TimeSpan.FromSeconds(8));
        if (!disconnectedEvent)
            throw new TestFailureException("T7 client B did not receive both the peer-disconnected event and snapshot.");
        await server.WaitForDisconnectedUserAsync(accounts.A.Username, TimeSpan.FromSeconds(5));
        log.Write("PASS", "T7 an abrupt client-A socket reset removed its session; client B received PLAYER_DISCONNECTED and IsDisconnected=true.");

        await using var reconnectedA = new TestTcpClient("A-reconnected");
        await reconnectedA.ConnectAsync(server.Port);
        await reconnectedA.LoginAsync(accounts.A);
        ResponsePacket reconnect = await reconnectedA.SendAsync(new RequestPacket
        {
            Command = Commands.GameReconnect,
            RoomId = roomId
        });
        if (!reconnect.Success || reconnect.RoomId != roomId || !reconnect.IsRacing ||
            reconnect.GamePlayers?.Any(player => player.UserId == clientA.UserId && player.IsDisconnected) == true)
            throw new TestFailureException("T7 reconnect inside the grace period did not restore the player to the running room.");
        await using var reconnectedStream = await GameStreamClient.ConnectAsync(server.Port, reconnectedA.Token);
        ResponsePacket restoredSnapshot = await reconnectedStream.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == roomId &&
                        snapshot.GamePlayers?.Any(player =>
                            player.UserId == clientA.UserId && !player.IsDisconnected) == true,
            TimeSpan.FromSeconds(5));
        if (!restoredSnapshot.IsRacing)
            throw new TestFailureException("T7 the reconnected client did not receive the restored live snapshot.");
        log.Write("PASS", "T7 the same account reconnected within the grace period and restored its racer state and snapshot stream.");
        return roomId;
    }

    private static async Task VerifyAccountSettingsAsync(
        TestTcpClient client,
        TestAccount account,
        int port,
        string connectionString,
        EvidenceLog log)
    {
        string previousPassword = account.Password;
        string previousToken = client.Token;
        string replacementPassword = $"Q8{Convert.ToHexString(RandomNumberGenerator.GetBytes(18))}x";
        string replacementEmail = $"{account.Username}+updated@example.invalid";

        ResponsePacket resetMismatch = await client.SendAsync(new RequestPacket
        {
            Command = Commands.RequestResetCode,
            Username = account.Username,
            Email = replacementEmail
        });
        if (resetMismatch.Success || !resetMismatch.Message.Contains("does not match", StringComparison.OrdinalIgnoreCase))
            throw new TestFailureException("Password recovery did not clearly reject a mismatched registered email.");

        string unknownUsername = $"ghost_{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}";
        ResponsePacket resetUnknownUser = await client.SendAsync(new RequestPacket
        {
            Command = Commands.RequestResetCode,
            Username = unknownUsername,
            Email = $"{unknownUsername}@example.invalid"
        });
        if (resetUnknownUser.Success ||
            !resetUnknownUser.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            throw new TestFailureException("Password recovery did not clearly reject an unknown username.");

        var repository = new NguoiDungRepository(connectionString);
        var resetPasswordResult =
            await repository.DoiMatKhauAsync(account.Username, account.Email, previousPassword);
        if (resetPasswordResult.Success ||
            !resetPasswordResult.Message.Contains("different", StringComparison.OrdinalIgnoreCase))
            throw new TestFailureException("Password recovery accepted a new password identical to the current password.");

        ResponsePacket samePassword = await client.SendAsync(new RequestPacket
        {
            Command = Commands.UpdateAccountSettings,
            CurrentPassword = previousPassword,
            NewPassword = previousPassword
        });
        if (samePassword.Success || !samePassword.Message.Contains("different", StringComparison.OrdinalIgnoreCase))
            throw new TestFailureException("Account settings accepted a new password identical to the current password.");

        string avatarBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/C2kAAAAASUVORK5CYII=";
        ResponsePacket avatarWithoutPassword = await client.SendAsync(new RequestPacket
        {
            Command = Commands.UploadAvatar,
            AvatarBase64 = avatarBase64
        });
        if (avatarWithoutPassword.Success || avatarWithoutPassword.Code != ErrorCodes.InvalidInput)
            throw new TestFailureException("Avatar upload did not require a current password.");

        ResponsePacket avatarWrongPassword = await client.SendAsync(new RequestPacket
        {
            Command = Commands.UploadAvatar,
            CurrentPassword = "incorrect-current-password",
            AvatarBase64 = avatarBase64
        });
        if (avatarWrongPassword.Success || avatarWrongPassword.Code != ErrorCodes.AuthFailed)
            throw new TestFailureException("Avatar upload accepted an incorrect current password.");

        ResponsePacket avatarSaved = await client.SendAsync(new RequestPacket
        {
            Command = Commands.UploadAvatar,
            CurrentPassword = previousPassword,
            AvatarBase64 = avatarBase64
        });
        ResponsePacket avatarFetched = await client.SendAsync(new RequestPacket { Command = Commands.GetAvatar });
        if (!avatarSaved.Success || !avatarFetched.Success || avatarFetched.AvatarBase64 != avatarBase64)
            throw new TestFailureException("Avatar upload did not persist data for the authenticated account.");

        ResponsePacket rejected = await client.SendAsync(new RequestPacket
        {
            Command = Commands.UpdateAccountSettings,
            CurrentPassword = "incorrect-current-password",
            NewPassword = replacementPassword,
            Email = replacementEmail
        });
        if (rejected.Success || rejected.Code != ErrorCodes.AuthFailed)
            throw new TestFailureException("Account settings accepted an incorrect current password.");

        ResponsePacket updated = await client.SendAsync(new RequestPacket
        {
            Command = Commands.UpdateAccountSettings,
            CurrentPassword = previousPassword,
            NewPassword = replacementPassword,
            Email = replacementEmail
        });
        if (!updated.Success ||
            updated.User == null ||
            updated.User.Email != replacementEmail ||
            string.IsNullOrWhiteSpace(updated.Token) ||
            updated.Token == previousToken)
            throw new TestFailureException("Account settings did not persist email and rotate the authenticated token.");

        client.SetToken(updated.Token);
        account.UpdateCredentials(replacementPassword, replacementEmail);

        await using var oldTokenClient = new TestTcpClient("stale-token");
        await oldTokenClient.ConnectAsync(port);
        ResponsePacket staleToken = await oldTokenClient.SendAsync(new RequestPacket
        {
            Command = Commands.AuthToken,
            Token = previousToken
        });
        if (staleToken.Success || staleToken.Code != ErrorCodes.Unauthorized)
            throw new TestFailureException("Password change did not revoke the previous auth token.");

        await using var verifier = new TestTcpClient("updated-credentials");
        await verifier.ConnectAsync(port);
        await verifier.LoginAsync(account);
        log.Write("PASS", "Recovery clearly rejected unknown users and email mismatch and rejected password reuse; settings rejected a wrong current password and password reuse; avatar changes required a correct current password and persisted; account changes rotated the token and revoked the old token.");
    }

    private static async Task VerifySoloWinRewardAsync(TestTcpClient client, EvidenceLog log)
    {
        ResponsePacket before = await client.SendAsync(new RequestPacket { Command = Commands.GetInventory });
        if (!before.Success || before.Inventory == null)
            throw new TestFailureException("The solo reward test could not read the initial account coins.");

        ResponsePacket started = await client.SendAsync(new RequestPacket
        {
            Command = Commands.StartSoloMatch,
            MapName = "game1"
        });
        if (!started.Success || string.IsNullOrWhiteSpace(started.MatchId))
            throw new TestFailureException("The server did not issue a solo match ticket.");

        await Task.Delay(TimeSpan.FromSeconds(10.2));
        var finishRequest = new RequestPacket
        {
            Command = Commands.FinishSoloMatch,
            MatchId = started.MatchId,
            MapName = "game1",
            GameEventType = "WIN"
        };
        ResponsePacket finished = await client.SendAsync(finishRequest);
        if (!finished.Success || finished.Inventory == null ||
            finished.Inventory.Coins != before.Inventory.Coins + 10)
            throw new TestFailureException("A verified solo win did not add exactly 10 coins.");

        ResponsePacket duplicate = await client.SendAsync(finishRequest);
        if (!duplicate.Success || duplicate.Inventory == null ||
            duplicate.Inventory.Coins != finished.Inventory.Coins)
            throw new TestFailureException("Retrying a solo win applied the reward more than once.");

        log.Write("PASS", "Server-issued solo match ticket awarded exactly 10 coins after the minimum duration; duplicate finish did not award again.");
    }

    private static async Task VerifyMultiplayerRewardPersistenceAsync(
        TestTcpClient client,
        string username,
        string connectionString,
        EvidenceLog log)
    {
        ResponsePacket before = await client.SendAsync(new RequestPacket { Command = Commands.GetInventory });
        if (!before.Success || before.Inventory == null)
            throw new TestFailureException("The multiplayer reward test could not read the initial account coins.");

        int userId;
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT UserId FROM dbo.NguoiDung WHERE Username=@username;";
            command.Parameters.Add("@username", System.Data.SqlDbType.NVarChar, 20).Value = username;
            object? result = await command.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new TestFailureException("The multiplayer reward test account was not present in the test database.");
            userId = Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }

        Guid matchId = Guid.NewGuid();
        var repository = new NguoiDungRepository(connectionString);
        await repository.AwardRaceWinnerAsync(
            matchId, userId, TcpGameRoomRegistry.WinnerRewardCoins, CancellationToken.None);
        int coinsAfterAward = await ReadAccountCoinsAsync(connectionString, username);
        if (coinsAfterAward != before.Inventory.Coins + 50)
            throw new TestFailureException("The multiplayer winner reward did not persist exactly 50 coins.");

        await repository.AwardRaceWinnerAsync(
            matchId, userId, TcpGameRoomRegistry.WinnerRewardCoins, CancellationToken.None);
        int coinsAfterDuplicate = await ReadAccountCoinsAsync(connectionString, username);
        if (coinsAfterDuplicate != coinsAfterAward)
            throw new TestFailureException("A repeated multiplayer winner reward was applied more than once.");

        log.Write("PASS", "Server multiplayer reward persistence added exactly 50 coins; repeated database award with the same match ID was deduplicated.");
    }

    private static async Task<int> ReadAccountCoinsAsync(string connectionString, string username)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Coins FROM dbo.NguoiDung WHERE Username=@username;";
        command.Parameters.Add("@username", System.Data.SqlDbType.NVarChar, 20).Value = username;
        object? result = await command.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            throw new TestFailureException("The reward test account was not present in the test database.");
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task SetSelectedMapAsync(
        string connectionString,
        TestAccount account,
        string map)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.NguoiDung SET SelectedMap=@map WHERE Username=@username;";
        command.Parameters.Add("@map", System.Data.SqlDbType.NVarChar, 50).Value = map;
        command.Parameters.Add("@username", System.Data.SqlDbType.NVarChar, 20).Value = account.Username;
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new TestFailureException("The selected-map migration fixture could not be updated.");
    }

    private static async Task VerifySelectedMapMigrationAsync(
        string connectionString,
        TestAccounts accounts,
        EvidenceLog log)
    {
        string[] usernames = new[] { accounts.A, accounts.B, accounts.C, accounts.D, accounts.E }
            .Select(account => account.Username)
            .ToArray();
        string[] expectedMaps = { MapNames.Game1, MapNames.Game2, MapNames.Game3, MapNames.Default, MapNames.Default };
        for (int i = 0; i < usernames.Length; i++)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT SelectedMap FROM dbo.NguoiDung WHERE Username=@username;";
            command.Parameters.Add("@username", System.Data.SqlDbType.NVarChar, 20).Value = usernames[i];
            string? actual = Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
            if (!string.Equals(actual, expectedMaps[i], StringComparison.Ordinal))
                throw new TestFailureException("A legacy selected-map value was not normalized to the expected catalog value.");
        }

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM dbo.NguoiDung
WHERE SelectedMap NOT IN (@game1, @game2, @game3);";
            command.Parameters.Add("@game1", System.Data.SqlDbType.NVarChar, 50).Value = MapNames.Game1;
            command.Parameters.Add("@game2", System.Data.SqlDbType.NVarChar, 50).Value = MapNames.Game2;
            command.Parameters.Add("@game3", System.Data.SqlDbType.NVarChar, 50).Value = MapNames.Game3;
            if (Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) != 0)
                throw new TestFailureException("The selected-map migration left unsupported values in the test database.");
        }

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT dc.definition
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name='dbo' AND t.name='NguoiDung' AND c.name='SelectedMap';";
            string definition = Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) ?? string.Empty;
            string normalized = definition.Replace("(", string.Empty, StringComparison.Ordinal)
                .Replace(")", string.Empty, StringComparison.Ordinal)
                .Replace("'", string.Empty, StringComparison.Ordinal)
                .Trim();
            if (!string.Equals(normalized, MapNames.Default, StringComparison.Ordinal))
                throw new TestFailureException("The SelectedMap database default does not match the canonical default map.");
        }

        log.Write("PASS", "Server startup normalized all legacy/unsupported SelectedMap values and set the database default to game1.");
    }

    private static async Task<string> RunShutdownPhaseAsync(
        TestServer server,
        TestAccounts accounts,
        EvidenceLog log)
    {
        await using var clientA = new TestTcpClient("A");
        await using var clientB = new TestTcpClient("B");
        await clientA.ConnectAsync(server.Port);
        await clientB.ConnectAsync(server.Port);
        await clientA.LoginAsync(accounts.A);
        await clientB.LoginAsync(accounts.B);
        string roomId = await CreateAndStartRoomAsync(clientA, clientB);
        await using var gameStreamA = await GameStreamClient.ConnectAsync(server.Port, clientA.Token);
        await using var gameStreamB = await GameStreamClient.ConnectAsync(server.Port, clientB.Token);
        await gameStreamA.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == roomId && snapshot.GamePlayers?.Count == 2,
            TimeSpan.FromSeconds(5));
        await gameStreamB.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == roomId && snapshot.GamePlayers?.Count == 2,
            TimeSpan.FromSeconds(5));

        ResponsePacket pingA = await clientA.SendAsync(new RequestPacket { Command = Commands.Ping }, TimeSpan.FromSeconds(1));
        ResponsePacket pingB = await clientB.SendAsync(new RequestPacket { Command = Commands.Ping }, TimeSpan.FromSeconds(1));
        if (!IsPong(pingA) || !IsPong(pingB))
            throw new TestFailureException("T8 both clients were not responsive immediately before server shutdown.");

        await server.StopAbruptlyAsync();
        bool clientALost = await clientA.DetectConnectionLossAsync(TimeSpan.FromSeconds(3));
        bool clientBLost = await clientB.DetectConnectionLossAsync(TimeSpan.FromSeconds(3));
        bool streamALost = await gameStreamA.WaitForCloseAsync(TimeSpan.FromSeconds(3));
        bool streamBLost = await gameStreamB.WaitForCloseAsync(TimeSpan.FromSeconds(3));
        if (!clientALost || !clientBLost || !streamALost || !streamBLost)
            throw new TestFailureException("T8 both clients did not report loss on their command and game-stream connections.");
        log.Write("PASS", "T8 terminating the test server made both clients detect command- and game-stream connection loss without a client crash.");
        return roomId;
    }

    private static async Task VerifyAuthoritativeInputAsync(
        TestTcpClient racer,
        GameStreamClient observerStream,
        int userId,
        ResponsePacket initialSnapshot,
        EvidenceLog log)
    {
        ResponsePacket outOfRange = await racer.SendAsync(new RequestPacket
        {
            Command = Commands.GameInput,
            Throttle = 2f,
            Steer = 0f,
            ClientTick = 1,
            ClientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        if (outOfRange.Success)
            throw new TestFailureException("Server accepted a throttle value outside [-1, 1].");

        ResponsePacket missingTimestamp = await racer.SendAsync(new RequestPacket
        {
            Command = Commands.GameInput,
            Throttle = 1f,
            Steer = 0f,
            ClientTick = 1
        });
        if (missingTimestamp.Success)
            throw new TestFailureException("Server accepted game input without a client timestamp.");

        ResponsePacket latest = await racer.SendAsync(new RequestPacket
        {
            Command = Commands.GameInput,
            Throttle = 0f,
            Steer = 0f,
            ClientTick = 1,
            ClientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        if (!latest.Success)
            throw new TestFailureException("Server rejected valid initial game input.");

        int tick = 1;
        GamePlayerSnapshotDto start = FindPlayer(initialSnapshot, userId);
        for (int step = 0; step < 8; step++)
        {
            await Task.Delay(60);
            latest = await racer.SendAsync(new RequestPacket
            {
                Command = Commands.GameInput,
                Throttle = 1f,
                Steer = 0f,
                ClientTick = ++tick,
                ClientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                PositionX = start.PositionX + 100_000f,
                PositionY = start.PositionY - 100_000f,
                PositionZ = start.PositionZ + 100_000f
            });
            if (!latest.Success)
                throw new TestFailureException("Server rejected valid bounded game input.");
        }

        ResponsePacket stale = await racer.SendAsync(new RequestPacket
        {
            Command = Commands.GameInput,
            Throttle = 0f,
            Steer = 0f,
            ClientTick = tick,
            ClientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        if (stale.Success)
            throw new TestFailureException("Server accepted a duplicate client input tick.");

        GamePlayerSnapshotDto end = FindPlayer(latest, userId);
        double displacement = Math.Sqrt(
            Math.Pow(end.PositionX - start.PositionX, 2) +
            Math.Pow(end.PositionY - start.PositionY, 2) +
            Math.Pow(end.PositionZ - start.PositionZ, 2));
        if (displacement < 0.1 || displacement > 100 ||
            latest.ServerTick <= initialSnapshot.ServerTick ||
            latest.ServerTimestampMs <= initialSnapshot.ServerTimestampMs)
            throw new TestFailureException("Server-authoritative input did not produce a bounded, sequenced movement snapshot.");

        ResponsePacket peerSnapshot = await observerStream.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == initialSnapshot.RoomId &&
                        snapshot.GamePlayers?.Any(player =>
                            player.UserId == userId &&
                            Math.Sqrt(Math.Pow(player.PositionX - start.PositionX, 2) +
                                      Math.Pow(player.PositionY - start.PositionY, 2) +
                                      Math.Pow(player.PositionZ - start.PositionZ, 2)) > 0.1) == true,
            TimeSpan.FromSeconds(5));
        if (FindPlayer(peerSnapshot, userId).IsDisconnected)
            throw new TestFailureException("Peer snapshot marked the active racer disconnected.");
        log.Write("PASS", $"TC03 server rejected invalid/stale input, ignored forged client coordinates, and broadcast bounded movement to the peer ({displacement:F2} m observed).");
    }

    private static GamePlayerSnapshotDto FindPlayer(ResponsePacket response, int userId) =>
        response.GamePlayers?.FirstOrDefault(player => player.UserId == userId)
        ?? throw new TestFailureException("Authoritative snapshot did not include the expected racer.");

    private static async Task VerifyFourPlayerUdpMovementAsync(
        TestServer server,
        TestTcpClient[] racers,
        GameStreamClient observer,
        string roomId,
        EvidenceLog log)
    {
        ResponsePacket before = await racers[0].SendAsync(new RequestPacket { Command = Commands.GameRoomState });
        if (!before.Success || before.GamePlayers?.Count != 4)
            throw new TestFailureException("The UDP test did not start with four server-authoritative racers.");

        UdpInputProbe[] probes = await Task.WhenAll(
            racers.Select(racer => racer.BindUdpAsync(server.UdpPort)));
        try
        {
            for (int frame = 0; frame < 8; frame++)
            {
                await Task.WhenAll(probes.Select((probe, index) =>
                    probe.SendNextAsync(1f, (index - 1.5f) * 0.05f)));
                await Task.Delay(50);
            }

            ResponsePacket moved = await observer.WaitForSnapshotAsync(snapshot =>
            {
                if (snapshot.RoomId != roomId || snapshot.GamePlayers?.Count != 4)
                    return false;
                return racers.All(racer =>
                {
                    GamePlayerSnapshotDto start = FindPlayer(before, racer.UserId);
                    GamePlayerSnapshotDto current = FindPlayer(snapshot, racer.UserId);
                    double distance = Math.Sqrt(
                        Math.Pow(current.PositionX - start.PositionX, 2) +
                        Math.Pow(current.PositionY - start.PositionY, 2) +
                        Math.Pow(current.PositionZ - start.PositionZ, 2));
                    return distance > 0.05;
                });
            }, TimeSpan.FromSeconds(5));

            if (moved.GamePlayers!.Any(player => player.Sequence != moved.ServerTick))
                throw new TestFailureException("TCP snapshots did not carry the matching server tick after UDP movement.");
            log.Write("PASS", "Four authenticated racers sent HMAC-protected, sequenced UDP movement; all four authoritative positions arrived in ordered TCP snapshots.");
        }
        finally
        {
            foreach (UdpInputProbe probe in probes)
                probe.Dispose();
        }
    }

    private static async Task VerifyRestartDropsRoomStateAsync(
        TestServer server,
        TestAccount account,
        string previousRoomId,
        EvidenceLog log)
    {
        await using var client = new TestTcpClient("post-restart");
        await client.ConnectAsync(server.Port);
        await client.LoginAsync(account);
        ResponsePacket reconnect = await client.SendAsync(new RequestPacket
        {
            Command = Commands.GameReconnect,
            RoomId = previousRoomId
        });
        if (reconnect.Success)
            throw new TestFailureException("Server restored an in-memory room after a process restart.");
        log.Write("PASS", "TC07 process restart cleared the documented in-memory room state; reconnect was rejected instead of returning a false success.");
    }

    private static async Task RunLoadAndFramingPhaseAsync(TestServer server, EvidenceLog log)
    {
        const int clientCount = 50;
        using var slowSender = new TcpClient(AddressFamily.InterNetwork);
        using var slowConnectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await slowSender.ConnectAsync(IPAddress.Loopback, server.Port, slowConnectTimeout.Token);
        await slowSender.GetStream().WriteAsync(new byte[] { 0, 0 }, slowConnectTimeout.Token);

        var allResponded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int responded = 0;
        async Task<double> ProbeAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var client = new TcpClient(AddressFamily.InterNetwork);
            Stopwatch stopwatch = Stopwatch.StartNew();
            await client.ConnectAsync(IPAddress.Loopback, server.Port, timeout.Token);
            NetworkStream stream = client.GetStream();
            await MessageFramer.SendAsync(stream, new RequestPacket { Command = Commands.Ping }, timeout.Token);
            ResponsePacket response = await MessageFramer.ReceiveAsync<ResponsePacket>(stream, timeout.Token)
                ?? throw new TestFailureException("An anonymous load client was disconnected before receiving a response.");
            stopwatch.Stop();
            if (response.Success || response.Code != ErrorCodes.Unauthorized)
                throw new TestFailureException("Anonymous load client received an unexpected PING response.");
            if (Interlocked.Increment(ref responded) == clientCount)
                allResponded.TrySetResult(true);
            await allResponded.Task.WaitAsync(timeout.Token);
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        double[] latencies = await Task.WhenAll(
                Enumerable.Range(0, clientCount).Select(_ => ProbeAsync()))
            .WaitAsync(TimeSpan.FromSeconds(15));
        double[] ordered = latencies.OrderBy(value => value).ToArray();
        double p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        if (latencies.Length != clientCount || p95 >= 3000 || ordered[^1] >= 5000)
            throw new TestFailureException("Fifty concurrent anonymous clients exceeded the bounded local response threshold.");
        log.Write("PASS", $"TC10 {clientCount} simultaneous anonymous clients got expected auth rejection; p95={p95:F1} ms, max={ordered[^1]:F1} ms while a slow sender held an incomplete header.");

        await VerifyMalformedFrameClosesAsync(server.Port, new byte[] { 0, 0, 0, 1, (byte)'{' }, "malformed JSON");
        log.Write("PASS", "TC10 malformed JSON frame was closed without an application response.");
        byte[] oversizedHeader = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(oversizedHeader, 64 * 1024 + 1);
        await VerifyMalformedFrameClosesAsync(server.Port, oversizedHeader, "oversized frame header");
        log.Write("PASS", "TC10 oversized frame header was closed without an application response.");
    }

    private static async Task VerifyMalformedFrameClosesAsync(int port, byte[] frame, string caseName)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient(AddressFamily.InterNetwork);
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        await client.GetStream().WriteAsync(frame, timeout.Token);
        byte[] response = new byte[1];
        try
        {
            int received = await client.GetStream().ReadAsync(response, timeout.Token);
            if (received != 0)
                throw new TestFailureException($"Server returned application data after {caseName}.");
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private static async Task<TimeSpan> MeasurePingAsync(TestTcpClient client)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ResponsePacket response = await client.SendAsync(
            new RequestPacket { Command = Commands.Ping },
            TimeSpan.FromSeconds(1));
        stopwatch.Stop();
        if (!IsPong(response) || stopwatch.Elapsed >= TimeSpan.FromSeconds(1))
            throw new TestFailureException($"T3 client {client.Name} did not receive PONG in under 1 second.");
        return stopwatch.Elapsed;
    }

    private static bool IsPong(ResponsePacket response) =>
        response.Success && string.Equals(response.Message, "PONG", StringComparison.Ordinal);

    private static async Task<string> CreateAndStartRoomAsync(
        TestTcpClient host,
        TestTcpClient guest,
        params TestTcpClient[] additionalRacers)
    {
        ResponsePacket created = await host.SendAsync(new RequestPacket
        {
            Command = Commands.CreateGameRoom,
            MapName = "game1",
            MaxPlayers = Math.Min(4, additionalRacers.Length + 2)
        });
        if (!created.Success || string.IsNullOrWhiteSpace(created.RoomId))
            throw new TestFailureException("The test host could not create a game room.");

        ResponsePacket joined = await guest.SendAsync(new RequestPacket
        {
            Command = Commands.JoinGameRoom,
            RoomId = created.RoomId
        });
        if (!joined.Success)
            throw new TestFailureException("The second test client could not join the game room.");

        foreach (TestTcpClient racer in additionalRacers)
        {
            ResponsePacket added = await racer.SendAsync(new RequestPacket
            {
                Command = Commands.JoinGameRoom,
                RoomId = created.RoomId
            });
            if (!added.Success)
                throw new TestFailureException("An additional test client could not join the four-player room.");
        }

        ResponsePacket blocked = await host.SendAsync(new RequestPacket
        {
            Command = Commands.StartGameRoom,
            MapName = "game1"
        });
        if (blocked.Success)
            throw new TestFailureException("The server allowed START before every client reported the lobby scene.");

        foreach (TestTcpClient racer in new[] { host, guest }.Concat(additionalRacers))
        {
            ResponsePacket reported = await racer.SendAsync(new RequestPacket
            {
                Command = Commands.ReportRoomScene,
                SceneName = "LobbyScene"
            });
            if (!reported.Success)
                throw new TestFailureException($"Test client {racer.Name} could not report lobby readiness.");
        }

        ResponsePacket started = await host.SendAsync(new RequestPacket
        {
            Command = Commands.StartGameRoom,
            MapName = "game1"
        });
        if (!started.Success || !started.IsRacing)
            throw new TestFailureException("The two-client game room could not be started.");
        return created.RoomId;
    }

    private static async Task<TimeSpan> SendAndObserveChatAsync(TestTcpClient sender, TestTcpClient receiver)
    {
        const string message = "T4 latency probe";
        Stopwatch stopwatch = Stopwatch.StartNew();
        ResponsePacket sent = await sender.SendAsync(new RequestPacket
        {
            Command = Commands.ChatSend,
            ChatMessage = message
        }, TimeSpan.FromSeconds(1));
        ResponsePacket received = await receiver.SendAsync(new RequestPacket
        {
            Command = Commands.ChatPoll,
            AfterMessageId = 0
        }, TimeSpan.FromSeconds(1));
        stopwatch.Stop();

        if (!sent.Success ||
            received.ChatMessages?.Any(item => item.Message == message) != true ||
            stopwatch.Elapsed >= TimeSpan.FromSeconds(1))
            throw new TestFailureException("T4 the peer did not receive the chat message in under 1 second.");
        return stopwatch.Elapsed;
    }

    private static async Task<string[]> SendTwentyMessagesAsync(TestTcpClient sender)
    {
        var sentMessages = new string[20];
        for (int index = 0; index < sentMessages.Length; index++)
        {
            sentMessages[index] = index == 7
                ? "Xin chào, tiếng Việt có đủ dấu: ă â đ ê ô ơ ư."
                : $"T5 sequence message {index + 1:D2}";
            ResponsePacket response = await sender.SendAsync(new RequestPacket
            {
                Command = Commands.ChatSend,
                ChatMessage = sentMessages[index]
            });

            int expectedCount = index + 2;
            if (!response.Success ||
                response.ChatMessages?.Count != expectedCount ||
                response.ChatMessages[^1].MessageId != expectedCount ||
                response.ChatMessages[^1].Message != sentMessages[index])
                throw new TestFailureException($"T5 message {index + 1} was not acknowledged in order.");
        }
        return sentMessages;
    }

    private static void VerifyTwentyMessages(string[] sentMessages, ResponsePacket received)
    {
        List<RoomChatMessageDto>? messages = received.ChatMessages;
        if (!received.Success || messages == null || messages.Count != sentMessages.Length + 1)
            throw new TestFailureException("T5 client B did not receive the expected message count.");

        for (int index = 0; index < messages.Count; index++)
        {
            if (messages[index].MessageId != index + 1)
                throw new TestFailureException("T5 the received chat message IDs were not consecutive.");
        }

        for (int index = 0; index < sentMessages.Length; index++)
        {
            if (!string.Equals(messages[index + 1].Message, sentMessages[index], StringComparison.Ordinal))
                throw new TestFailureException($"T5 message {index + 1} was lost, merged, or changed.");
        }
    }

    private static string ResolveTestConnectionString()
    {
        string? configured = Environment.GetEnvironmentVariable("DANTOPHONG_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
        {
            string? password =
                Environment.GetEnvironmentVariable("DANTOPHONG_TEST_SA_PASSWORD") ??
                Environment.GetEnvironmentVariable("DANTOPHONG_TEST_SA_PASSWORD", EnvironmentVariableTarget.User);
            if (string.IsNullOrWhiteSpace(password))
                throw new TestBlockedException("DANTOPHONG_TEST_SA_PASSWORD is unavailable; no database or server actions were run.");

            configured = new SqlConnectionStringBuilder
            {
                DataSource = "localhost,1433",
                InitialCatalog = "DanToPhongDB_Test",
                UserID = "sa",
                Password = password,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 10
            }.ConnectionString;
            Environment.SetEnvironmentVariable("DANTOPHONG_DB_CONNECTION", configured);
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(configured);
        }
        catch (ArgumentException)
        {
            throw new TestBlockedException("DANTOPHONG_DB_CONNECTION is invalid; expected the dedicated local test database.");
        }

        bool localTestTarget =
            string.Equals(builder.InitialCatalog, "DanToPhongDB_Test", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(builder.UserID, "sa", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(builder.DataSource, "localhost,1433", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(builder.DataSource, "tcp:localhost,1433", StringComparison.OrdinalIgnoreCase));
        if (!localTestTarget)
            throw new TestBlockedException("DANTOPHONG_DB_CONNECTION must target localhost:1433/DanToPhongDB_Test as sa.");

        return builder.ConnectionString;
    }

    private static string ResolveServerAssembly(string root)
    {
        string? configured = Environment.GetEnvironmentVariable("DANTOPHONG_TEST_SERVER_DLL");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return Path.GetFullPath(configured);

        string defaultPath = Path.Combine(root, "UI", "DanToPhong.Server", "bin", "Release", "net10.0", "DanToPhong.Server.dll");
        if (File.Exists(defaultPath))
            return defaultPath;

        throw new TestBlockedException("The server assembly was not found; run tools\\run-t2t8-protocol-test.ps1 to build the isolated test outputs.");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(Environment.CurrentDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                return current.FullName;
            current = current.Parent;
        }
        throw new TestBlockedException("Run the protocol test from inside the repository.");
    }
}

internal sealed class TestAccounts
{
    public TestAccount A { get; } = TestAccount.Create();
    public TestAccount B { get; } = TestAccount.Create();
    public TestAccount C { get; } = TestAccount.Create();
    public TestAccount D { get; } = TestAccount.Create();
    public TestAccount E { get; } = TestAccount.Create();
    public bool HasCreatedAccounts => A.Created || B.Created || C.Created || D.Created || E.Created;

    public async Task SeedAsync(string connectionString)
    {
        var repository = new NguoiDungRepository(connectionString);
        await CreateAsync(repository, A, "Protocol test A");
        await CreateAsync(repository, B, "Protocol test B");
        await CreateAsync(repository, C, "Protocol test C");
        await CreateAsync(repository, D, "Protocol test D");
        await CreateAsync(repository, E, "Protocol test E");
    }

    public async Task CleanupAsync(string connectionString)
    {
        string[] usernames = new[] { A, B, C, D, E }.Where(account => account.Created).Select(account => account.Username).ToArray();
        if (usernames.Length == 0)
            return;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
DECLARE @TestUserIds TABLE (UserId INT PRIMARY KEY);
INSERT INTO @TestUserIds(UserId)
SELECT UserId FROM dbo.NguoiDung WHERE Username IN (@usernameA, @usernameB, @usernameC, @usernameD, @usernameE);
DELETE r FROM dbo.GameMatchRewards AS r JOIN @TestUserIds AS u ON u.UserId = r.UserId;
DELETE r FROM dbo.RequestIdempotency AS r JOIN @TestUserIds AS u ON u.UserId = r.UserId;
DELETE t FROM dbo.AuthTokens AS t JOIN @TestUserIds AS u ON u.UserId = t.UserId;
DELETE d FROM dbo.DanhGia AS d JOIN @TestUserIds AS u ON u.UserId = d.UserId;
DELETE v FROM dbo.VatPham AS v JOIN @TestUserIds AS u ON u.UserId = v.UserId;
DELETE FROM dbo.NguoiDung WHERE UserId IN (SELECT UserId FROM @TestUserIds);";
        command.Parameters.Add("@usernameA", System.Data.SqlDbType.NVarChar, 20).Value =
            usernames.Contains(A.Username, StringComparer.Ordinal) ? A.Username : "__unused_a__";
        command.Parameters.Add("@usernameB", System.Data.SqlDbType.NVarChar, 20).Value =
            usernames.Contains(B.Username, StringComparer.Ordinal) ? B.Username : "__unused_b__";
        command.Parameters.Add("@usernameC", System.Data.SqlDbType.NVarChar, 20).Value =
            usernames.Contains(C.Username, StringComparer.Ordinal) ? C.Username : "__unused_c__";
        command.Parameters.Add("@usernameD", System.Data.SqlDbType.NVarChar, 20).Value =
            usernames.Contains(D.Username, StringComparer.Ordinal) ? D.Username : "__unused_d__";
        command.Parameters.Add("@usernameE", System.Data.SqlDbType.NVarChar, 20).Value =
            usernames.Contains(E.Username, StringComparer.Ordinal) ? E.Username : "__unused_e__";
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        A.Created = false;
        B.Created = false;
        C.Created = false;
        D.Created = false;
        E.Created = false;
    }

    private static async Task CreateAsync(NguoiDungRepository repository, TestAccount account, string fullName)
    {
        (bool success, string code, string _, string? _) = await repository.DangKyAsync(new RequestPacket
        {
            Username = account.Username,
            Password = account.Password,
            FullName = fullName,
            Email = account.Email
        });
        if (!success)
            throw new TestFailureException($"Could not create an isolated account fixture ({code}).");
        account.Created = true;
    }
}

internal sealed class TestAccount
{
    public string Username { get; }
    public string Password { get; private set; }
    public string Email { get; private set; }
    public bool Created { get; set; }

    private TestAccount(string username, string password)
    {
        Username = username;
        Password = password;
        Email = $"{username}@example.invalid";
    }

    public void UpdateCredentials(string password, string email)
    {
        Password = password;
        Email = email;
    }

    public static TestAccount Create()
    {
        string suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
        string password = $"Q7{Convert.ToHexString(RandomNumberGenerator.GetBytes(18))}z";
        return new TestAccount($"tc{suffix}", password);
    }
}

internal sealed class TestTcpClient : IAsyncDisposable
{
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(10);
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string Name { get; }
    public string Token { get; private set; } = string.Empty;
    public int UserId { get; private set; }

    public TestTcpClient(string name) => Name = name;

    public async Task ConnectAsync(int port)
    {
        _client = new TcpClient(AddressFamily.InterNetwork);
        await _client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromSeconds(5));
        _stream = _client.GetStream();
    }

    public async Task LoginAsync(TestAccount account)
    {
        ResponsePacket response = await SendAsync(new RequestPacket
        {
            Command = Commands.Login,
            Username = account.Username,
            Password = account.Password
        }, TimeSpan.FromSeconds(20));
        if (!response.Success || string.IsNullOrWhiteSpace(response.Token) || response.User == null)
            throw new TestFailureException($"Client {Name} did not authenticate over TCP.");
        if (!string.Equals(response.User.Email, account.Email, StringComparison.Ordinal))
            throw new TestFailureException($"Client {Name} did not receive the email persisted for its account.");

        Token = response.Token;
        UserId = response.User.UserId;
    }

    public async Task<UdpInputProbe> BindUdpAsync(int expectedPort)
    {
        ResponsePacket response = await SendAsync(new RequestPacket { Command = Commands.GameUdpBind });
        if (!response.Success || response.UdpPort != expectedPort ||
            string.IsNullOrWhiteSpace(response.UdpSessionId) ||
            string.IsNullOrWhiteSpace(response.UdpKey) ||
            response.UdpNextSequence == 0)
            throw new TestFailureException($"Client {Name} did not receive a valid authenticated UDP binding.");

        return new UdpInputProbe(
            IPAddress.Loopback,
            response.UdpPort,
            Convert.FromHexString(response.UdpSessionId),
            Convert.FromBase64String(response.UdpKey),
            response.UdpNextSequence);
    }

    public void SetToken(string token) => Token = token;

    public async Task<ResponsePacket> SendAsync(RequestPacket request, TimeSpan? timeout = null)
    {
        NetworkStream stream = _stream ?? throw new InvalidOperationException("TCP client is not connected.");
        using var deadline = new CancellationTokenSource(timeout ?? DefaultRequestTimeout);
        await MessageFramer.SendAsync(stream, request, deadline.Token);
        ResponsePacket? response = await MessageFramer.ReceiveAsync<ResponsePacket>(stream, deadline.Token);
        return response ?? throw new IOException("Server closed the request-response connection.");
    }

    public async Task<bool> DetectConnectionLossAsync(TimeSpan timeout)
    {
        try
        {
            _ = await SendAsync(new RequestPacket { Command = Commands.Ping }, timeout);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (SocketException)
        {
            return true;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void AbortAbruptly()
    {
        if (_client?.Client is Socket socket)
            socket.LingerState = new LingerOption(true, 0);
        _client?.Dispose();
        _client = null;
        _stream = null;
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        _stream = null;
        return ValueTask.CompletedTask;
    }
}

internal sealed class GameStreamClient : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<ResponsePacket> _messages = Channel.CreateUnbounded<ResponsePacket>();
    private readonly TaskCompletionSource<bool> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task _reader;

    private GameStreamClient(TcpClient client, NetworkStream stream)
    {
        _client = client;
        _stream = stream;
        _reader = ReadLoopAsync();
    }

    public static async Task<GameStreamClient> ConnectAsync(int port, string token)
    {
        var client = new TcpClient(AddressFamily.InterNetwork);
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromSeconds(5));
            NetworkStream stream = client.GetStream();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await MessageFramer.SendAsync(stream, new RequestPacket
            {
                Command = Commands.AuthToken,
                Token = token
            }, timeout.Token);
            ResponsePacket? auth = await MessageFramer.ReceiveAsync<ResponsePacket>(stream, timeout.Token);
            if (auth?.Success != true)
                throw new TestFailureException("A game-stream client could not authenticate its TCP session.");

            await MessageFramer.SendAsync(stream, new RequestPacket { Command = Commands.GameStream }, timeout.Token);
            ResponsePacket? subscription = await MessageFramer.ReceiveAsync<ResponsePacket>(stream, timeout.Token);
            if (subscription?.Success != true || subscription.Command != Commands.GameStream)
                throw new TestFailureException("A game-stream client could not subscribe to the running race.");

            return new GameStreamClient(client, stream);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public async Task<ResponsePacket> WaitForSnapshotAsync(Func<ResponsePacket, bool> predicate, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        while (await _messages.Reader.WaitToReadAsync(deadline.Token))
        {
            while (_messages.Reader.TryRead(out ResponsePacket? response))
            {
                if (response.Command == Commands.GameSnapshot && predicate(response))
                    return response;
            }
        }
        throw new TestFailureException("The game stream did not produce the expected snapshot.");
    }

    public async Task<bool> WaitForDisconnectEventAndSnapshotAsync(int disconnectedUserId, TimeSpan timeout)
    {
        bool eventSeen = false;
        bool snapshotSeen = false;
        using var deadline = new CancellationTokenSource(timeout);

        while (!eventSeen || !snapshotSeen)
        {
            ResponsePacket response;
            try
            {
                response = await _messages.Reader.ReadAsync(deadline.Token);
            }
            catch (ChannelClosedException)
            {
                return false;
            }

            if (response.Command == Commands.GameEvent &&
                response.GameEvent?.Type == "PLAYER_DISCONNECTED" &&
                response.GameEvent.UserId == disconnectedUserId)
                eventSeen = true;

            if (response.Command == Commands.GameSnapshot &&
                response.GamePlayers?.Any(player =>
                    player.UserId == disconnectedUserId && player.IsDisconnected) == true)
                snapshotSeen = true;
        }

        return true;
    }

    public async Task<bool> WaitForCloseAsync(TimeSpan timeout)
    {
        try
        {
            await _closed.Task.WaitAsync(timeout);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public void AbortAbruptly()
    {
        _client.Client.LingerState = new LingerOption(true, 0);
        _client.Dispose();
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                ResponsePacket? response = await MessageFramer.ReceiveAsync<ResponsePacket>(_stream, _stop.Token);
                if (response == null)
                    break;
                await _messages.Writer.WriteAsync(response, _stop.Token);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        finally
        {
            _messages.Writer.TryComplete();
            _closed.TrySetResult(true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _client.Dispose();
        await _reader.ConfigureAwait(false);
        _stop.Dispose();
    }
}

internal sealed class UdpInputProbe : IDisposable
{
    private const int ContentLength = 42;
    private readonly UdpClient _client = new(AddressFamily.InterNetwork);
    private readonly IPEndPoint _endpoint;
    private readonly byte[] _sessionId;
    private readonly byte[] _key;
    private uint _nextSequence;

    public UdpInputProbe(IPAddress address, int port, byte[] sessionId, byte[] key, uint nextSequence)
    {
        if (sessionId.Length != 16 || key.Length < 32 || nextSequence == 0)
            throw new ArgumentException("The UDP binding material has an invalid length.");

        _endpoint = new IPEndPoint(address, port);
        _sessionId = sessionId;
        _key = key;
        _nextSequence = nextSequence;
    }

    public Task SendNextAsync(float throttle, float steer)
    {
        uint sequence = _nextSequence;
        if (sequence == 0)
            throw new InvalidOperationException("The UDP sequence wrapped and requires a new binding.");
        _nextSequence = sequence == uint.MaxValue ? 0 : sequence + 1;
        return SendAsync(sequence, throttle, steer);
    }

    private async Task SendAsync(uint sequence, float throttle, float steer)
    {
        byte[] datagram = new byte[ContentLength + 32];
        "DTP1"u8.CopyTo(datagram);
        _sessionId.CopyTo(datagram, 4);
        BinaryPrimitives.WriteUInt32BigEndian(datagram.AsSpan(20, 4), sequence);
        BinaryPrimitives.WriteInt64BigEndian(datagram.AsSpan(24, 8), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        BinaryPrimitives.WriteInt32BigEndian(datagram.AsSpan(32, 4), BitConverter.SingleToInt32Bits(throttle));
        BinaryPrimitives.WriteInt32BigEndian(datagram.AsSpan(36, 4), BitConverter.SingleToInt32Bits(steer));
        datagram[40] = 0;
        datagram[41] = 0;
        HMACSHA256.HashData(_key, datagram.AsSpan(0, ContentLength)).CopyTo(datagram, ContentLength);
        await _client.SendAsync(datagram, datagram.Length, _endpoint);
    }

    public void Dispose()
    {
        _client.Dispose();
        CryptographicOperations.ZeroMemory(_key);
    }
}

internal sealed class TestServer : IAsyncDisposable
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output = new();
    private bool _started;

    public int Port { get; }
    public int UdpPort { get; }

    private TestServer(Process process, int port, int udpPort)
    {
        _process = process;
        Port = port;
        UdpPort = udpPort;
    }

    public static async Task<TestServer> StartAsync(
        string serverAssembly,
        string connectionString,
        int? requestedPort = null,
        bool useTls = false,
        string? certificatePath = null,
        string? certificatePassword = null,
        int? secondaryPort = null)
    {
        int port = requestedPort ?? GetConfiguredTestPort();
        int udpPort = GetConfiguredTestUdpPort();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(serverAssembly)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(serverAssembly);
        startInfo.Environment["DANTOPHONG_DB_CONNECTION"] = connectionString;
        startInfo.Environment["DANTOPHONG_USE_TLS"] = useTls ? "true" : "false";
        startInfo.Environment["DANTOPHONG_PORT"] = port.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["DANTOPHONG_CERT_PATH"] = useTls ? certificatePath ?? string.Empty : string.Empty;
        startInfo.Environment["DANTOPHONG_CERT_PASSWORD"] = useTls ? certificatePassword ?? string.Empty : string.Empty;
        startInfo.Environment["DANTOPHONG_SECONDARY_PORT"] = secondaryPort?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        startInfo.Environment["DANTOPHONG_UDP_PORT"] = udpPort.ToString(CultureInfo.InvariantCulture);

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var server = new TestServer(process, port, udpPort);
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data != null)
                server._output.Enqueue(eventArgs.Data);
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data != null)
                server._output.Enqueue(eventArgs.Data);
        };

        try
        {
            if (!process.Start())
                throw new TestFailureException("The test server process could not be started.");
            server._started = true;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Stopwatch startup = Stopwatch.StartNew();
            while (!server.HasOutput(line => line.Contains($":{port}; TLS=", StringComparison.Ordinal)))
            {
                if (process.HasExited)
                    throw new TestFailureException($"The test server exited before listening (exit code {process.ExitCode}).");
                if (startup.Elapsed >= StartupTimeout)
                    throw new TestFailureException("The test server did not start listening within 30 seconds.");
                await Task.Delay(50);
            }

            return server;
        }
        catch
        {
            await server.DisposeAsync();
            throw;
        }
    }

    public async Task WaitForSecondaryListenerAsync(int port, TimeSpan timeout)
    {
        Stopwatch wait = Stopwatch.StartNew();
        while (wait.Elapsed < timeout)
        {
            if (_output.Any(line => line.Contains($"Secondary listener enabled on ", StringComparison.Ordinal) &&
                                    line.Contains($":{port}; TLS=", StringComparison.Ordinal)))
                return;
            if (_process.HasExited)
                break;
            await Task.Delay(25);
        }
        throw new TestFailureException("The optional secondary TLS listener did not start.");
    }

    public async Task WaitForAcceptedClientsAsync(int count, TimeSpan timeout)
    {
        Stopwatch wait = Stopwatch.StartNew();
        while (wait.Elapsed < timeout)
        {
            int accepted = _output.Count(line => line.Contains("Connected from 127.0.0.1:", StringComparison.Ordinal));
            if (accepted >= count)
                return;
            if (_process.HasExited)
                break;
            await Task.Delay(25);
        }

        throw new TestFailureException("The server did not log both accepted TCP client sessions.");
    }

    public async Task WaitForDisconnectedUserAsync(string username, TimeSpan timeout)
    {
        string expected = $"Session removed ({username}).";
        Stopwatch wait = Stopwatch.StartNew();
        while (wait.Elapsed < timeout)
        {
            if (HasOutput(line => line.Contains(expected, StringComparison.Ordinal)))
                return;
            if (_process.HasExited)
                break;
            await Task.Delay(25);
        }
        throw new TestFailureException("The server did not log cleanup for the disconnected test account.");
    }

    private bool HasOutput(Func<string, bool> predicate) => _output.Any(predicate);

    public async Task StopAbruptlyAsync()
    {
        if (!_started)
            return;
        if (!_process.HasExited)
        {
            _process.Kill();
            await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAbruptlyAsync();
        _process.Dispose();
    }

    private static int GetConfiguredTestPort()
    {
        string? configured = Environment.GetEnvironmentVariable("DANTOPHONG_TEST_PORT");
        if (string.IsNullOrWhiteSpace(configured))
            return 19000;
        if (!int.TryParse(configured, out int port) || port is < 1024 or > 65535 || port == 9000)
            throw new TestFailureException("DANTOPHONG_TEST_PORT must be a non-privileged test port other than 9000.");
        return port;
    }

    private static int GetConfiguredTestUdpPort()
    {
        string? configured = Environment.GetEnvironmentVariable("DANTOPHONG_TEST_UDP_PORT");
        if (string.IsNullOrWhiteSpace(configured))
            return 19001;
        if (!int.TryParse(configured, out int port) ||
            port is < 1024 or > 65535 ||
            port is 9000 or 9001 ||
            port == GetConfiguredTestPort())
            throw new TestFailureException("DANTOPHONG_TEST_UDP_PORT must be a non-privileged UDP test port distinct from the TCP ports.");
        return port;
    }
}

internal sealed class EvidenceLog : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _gate = new();

    public EvidenceLog(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
        Console.WriteLine($"Evidence: {path}");
    }

    public void Write(string level, string message)
    {
        string line = $"{DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)} [{level}] {message}";
        lock (_gate)
        {
            Console.WriteLine(line);
            _writer.WriteLine(line);
        }
    }

    public void Dispose() => _writer.Dispose();
}

internal sealed class TestFailureException : Exception
{
    public TestFailureException(string message) : base(message) { }
}

internal sealed class TestBlockedException : Exception
{
    public TestBlockedException(string message) : base(message) { }
}
