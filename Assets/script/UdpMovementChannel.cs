using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

public sealed class UdpMovementChannel : IDisposable
{
    private const int SignedPayloadLength = 42;
    private const int MacLength = 32;
    private readonly byte[] sessionId = new byte[16];
    private byte[] key;
    private UdpClient client;
    private IPEndPoint serverEndpoint;
    public uint InitialSequence { get; private set; }

    public async Task BindAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Resp binding = await ServerClient.CallAsync(new Req { command = "GAME_UDP_BIND" });
        if (binding == null || !binding.success ||
            binding.udpPort != LauncherArgs.UdpPort ||
            binding.udpNextSequence == 0 ||
            string.IsNullOrWhiteSpace(binding.udpSessionId) ||
            string.IsNullOrWhiteSpace(binding.udpKey))
            throw new IOException("Server did not return valid UDP movement credentials/configuration.");

        byte[] parsedId = ParseHex(binding.udpSessionId);
        byte[] parsedKey = Convert.FromBase64String(binding.udpKey);
        if (parsedId.Length != sessionId.Length || parsedKey.Length != 32)
            throw new InvalidDataException("Server returned malformed UDP movement credentials.");

        IPAddress[] addresses = await Dns.GetHostAddressesAsync(LauncherArgs.Host);
        IPAddress address = null;
        foreach (IPAddress candidate in addresses)
        {
            if (candidate.AddressFamily == AddressFamily.InterNetwork)
            {
                address = candidate;
                break;
            }
        }
        if (address == null && addresses.Length > 0)
            address = addresses[0];
        if (address == null)
            throw new SocketException((int)SocketError.HostNotFound);

        CloseSocket();
        Buffer.BlockCopy(parsedId, 0, sessionId, 0, sessionId.Length);
        key = parsedKey;
        InitialSequence = binding.udpNextSequence;
        client = new UdpClient(address.AddressFamily);
        serverEndpoint = new IPEndPoint(address, binding.udpPort);
    }

    public async Task SendInputAsync(
        uint sequence,
        float throttle,
        float steer,
        bool brake,
        int vehicleIndex,
        long clientTimestampMs,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (client == null || key == null || serverEndpoint == null)
            throw new InvalidOperationException("UDP movement channel is not bound.");

        byte[] datagram = new byte[SignedPayloadLength + MacLength];
        datagram[0] = (byte)'D';
        datagram[1] = (byte)'T';
        datagram[2] = (byte)'P';
        datagram[3] = (byte)'1';
        Buffer.BlockCopy(sessionId, 0, datagram, 4, sessionId.Length);
        BinaryPrimitives.WriteUInt32BigEndian(datagram.AsSpan(20, 4), sequence);
        BinaryPrimitives.WriteInt64BigEndian(datagram.AsSpan(24, 8), clientTimestampMs);
        BinaryPrimitives.WriteInt32BigEndian(datagram.AsSpan(32, 4), BitConverter.SingleToInt32Bits(throttle));
        BinaryPrimitives.WriteInt32BigEndian(datagram.AsSpan(36, 4), BitConverter.SingleToInt32Bits(steer));
        datagram[40] = brake ? (byte)1 : (byte)0;
        datagram[41] = (byte)Math.Max(0, Math.Min(100, vehicleIndex));

        byte[] mac;
        using (var hmac = new HMACSHA256(key))
            mac = hmac.ComputeHash(datagram, 0, SignedPayloadLength);
        Buffer.BlockCopy(mac, 0, datagram, SignedPayloadLength, MacLength);

        UdpClient activeClient = client;
        IPEndPoint activeEndpoint = serverEndpoint;
        await activeClient.SendAsync(datagram, datagram.Length, activeEndpoint);
        token.ThrowIfCancellationRequested();
    }

    public void Dispose()
    {
        CloseSocket();
        if (key != null)
            Array.Clear(key, 0, key.Length);
        key = null;
        Array.Clear(sessionId, 0, sessionId.Length);
        InitialSequence = 0;
    }

    private void CloseSocket()
    {
        UdpClient oldClient = client;
        client = null;
        serverEndpoint = null;
        oldClient?.Dispose();
    }

    private static byte[] ParseHex(string value)
    {
        if (value.Length % 2 != 0)
            throw new InvalidDataException("UDP session id is malformed.");
        byte[] bytes = new byte[value.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(value.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out bytes[i]))
                throw new InvalidDataException("UDP session id is malformed.");
        }
        return bytes;
    }
}
