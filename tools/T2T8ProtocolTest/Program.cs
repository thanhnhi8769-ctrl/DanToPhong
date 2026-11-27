using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Channels;
using DanToPhong.Protocol;
using DanToPhong.Server;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private static async Task<int> Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string root = FindRepositoryRoot();
        string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string evidencePath = Path.Combine(root, "docs", "evidence", $"T2-T8-protocol-console-{stamp}.log");
        using var log = new EvidenceLog(evidencePath);
        log.Write("INFO", "Console TCP protocol integration run; this is protocol-level evidence, not GUI evidence.");

        TestAccounts? accounts = null;
        Exception? failure = null;
        bool blocked = false;

        try
        {
            string connectionString = ResolveTestConnectionString();
            string serverAssembly = ResolveServerAssembly(root);
            accounts = new TestAccounts();

            await using (TestServer server = await TestServer.StartAsync(serverAssembly, connectionString))
            {
                await accounts.SeedAsync(connectionString);
                await RunDisconnectPhaseAsync(server, accounts, log);
                await server.StopAbruptlyAsync();
            }

            await using (TestServer server = await TestServer.StartAsync(serverAssembly, connectionString))
            {
                await RunShutdownPhaseAsync(server, accounts, log);
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

        log.Write("RESULT", "PASS — T2-T8 protocol checks observed; GUI checks remain manual.");
        return 0;
    }

    private static async Task RunDisconnectPhaseAsync(
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
        await server.WaitForAcceptedClientsAsync(2, TimeSpan.FromSeconds(5));
        log.Write("PASS", "T2 both TCP clients connected, authenticated, and appeared in server session logs.");

        TimeSpan latencyA = await MeasurePingAsync(clientA);
        TimeSpan latencyB = await MeasurePingAsync(clientB);
        log.Write("PASS", $"T3 both authenticated clients received PONG in under 1 second (A {latencyA.TotalMilliseconds:F1} ms; B {latencyB.TotalMilliseconds:F1} ms).");

        string roomId = await CreateAndStartRoomAsync(clientA, clientB);
        await using var gameStreamB = await GameStreamClient.ConnectAsync(server.Port, clientB.Token);
        await gameStreamB.WaitForSnapshotAsync(
            snapshot => snapshot.RoomId == roomId && snapshot.GamePlayers?.Count == 2,
            TimeSpan.FromSeconds(5));

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

    }

    private static async Task RunShutdownPhaseAsync(
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

    private static async Task<string> CreateAndStartRoomAsync(TestTcpClient host, TestTcpClient guest)
    {
        ResponsePacket created = await host.SendAsync(new RequestPacket
        {
            Command = Commands.CreateGameRoom,
            MapName = "game 1",
            MaxPlayers = 2
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

        ResponsePacket started = await host.SendAsync(new RequestPacket
        {
            Command = Commands.StartGameRoom,
            MapName = "game 1"
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
    public bool HasCreatedAccounts => A.Created || B.Created;

    public async Task SeedAsync(string connectionString)
    {
        var repository = new NguoiDungRepository(connectionString);
        await CreateAsync(repository, A, "Protocol test A");
        await CreateAsync(repository, B, "Protocol test B");
    }

    public async Task CleanupAsync(string connectionString)
    {
        string[] usernames = new[] { A, B }.Where(account => account.Created).Select(account => account.Username).ToArray();
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
SELECT UserId FROM dbo.NguoiDung WHERE Username IN (@usernameA, @usernameB);
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
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        A.Created = false;
        B.Created = false;
    }

    private static async Task CreateAsync(NguoiDungRepository repository, TestAccount account, string fullName)
    {
        (bool success, string code, string _, string? _) = await repository.DangKyAsync(new RequestPacket
        {
            Username = account.Username,
            Password = account.Password,
            FullName = fullName
        });
        if (!success)
            throw new TestFailureException($"Could not create an isolated account fixture ({code}).");
        account.Created = true;
    }
}

internal sealed class TestAccount
{
    public string Username { get; }
    public string Password { get; }
    public bool Created { get; set; }

    private TestAccount(string username, string password)
    {
        Username = username;
        Password = password;
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

        Token = response.Token;
        UserId = response.User.UserId;
    }

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

    public async Task WaitForSnapshotAsync(Func<ResponsePacket, bool> predicate, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        while (await _messages.Reader.WaitToReadAsync(deadline.Token))
        {
            while (_messages.Reader.TryRead(out ResponsePacket? response))
            {
                if (response.Command == Commands.GameSnapshot && predicate(response))
                    return;
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

internal sealed class TestServer : IAsyncDisposable
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output = new();
    private bool _started;

    public int Port { get; }

    private TestServer(Process process, int port)
    {
        _process = process;
        Port = port;
    }

    public static async Task<TestServer> StartAsync(string serverAssembly, string connectionString)
    {
        int port = GetAvailableLoopbackPort();
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
        startInfo.Environment["DANTOPHONG_USE_TLS"] = "false";
        startInfo.Environment["DANTOPHONG_PORT"] = port.ToString(CultureInfo.InvariantCulture);

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var server = new TestServer(process, port);
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
            while (!server.HasOutput(line => line.Contains($"Server listening on 127.0.0.1:{port}", StringComparison.Ordinal)))
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

    private static int GetAvailableLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
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
