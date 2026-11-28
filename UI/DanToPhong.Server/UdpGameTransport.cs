using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DanToPhong.Protocol;

namespace DanToPhong.Server
{
    internal sealed class UdpGameTransport : IDisposable
    {
        private const int SignedPayloadLength = 42;
        private const int MacLength = 32;
        private const int DatagramLength = SignedPayloadLength + MacLength;
        private static readonly byte[] Magic = { (byte)'D', (byte)'T', (byte)'P', (byte)'1' };
        private readonly UdpClient _client;
        private readonly int _port;
        private readonly ConcurrentDictionary<string, Binding> _bindings = new(StringComparer.Ordinal);
        private readonly Func<int, CancellationToken, Task> _awardWinner;

        public UdpGameTransport(IPAddress listenAddress, int port, Func<int, CancellationToken, Task> awardWinner)
        {
            _port = port;
            _awardWinner = awardWinner;
            _client = new UdpClient(new IPEndPoint(listenAddress, port));
        }

        public ResponsePacket Bind(ClientSession session)
        {
            if (!session.AuthenticatedUserId.HasValue)
                return new ResponsePacket { Success = false, Code = ErrorCodes.Unauthorized, Message = "Session is not authenticated." };

            ResponsePacket state = TcpGameRoomRegistry.State(session.AuthenticatedUserId.Value);
            if (!state.Success || !state.IsRacing || state.IsSpectator ||
                !TcpGameRoomRegistry.TryGetNextUdpSequence(session.AuthenticatedUserId.Value, out uint nextSequence))
                return new ResponsePacket { Success = false, Code = ErrorCodes.InvalidInput, Message = "Only an active racer can bind a UDP movement channel." };

            RemoveSession(session);
            string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            byte[] key = RandomNumberGenerator.GetBytes(32);
            var binding = new Binding(session, key);
            session.UdpSessionId = id;
            session.UdpKey = key;
            _bindings[id] = binding;
            return new ResponsePacket
            {
                Success = true,
                Code = ErrorCodes.Ok,
                Command = Commands.GameUdpBind,
                Message = "Authenticated UDP movement channel bound.",
                UdpPort = _port,
                UdpSessionId = id,
                UdpKey = Convert.ToBase64String(key),
                UdpNextSequence = nextSequence
            };
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult received = await _client.ReceiveAsync(token).ConfigureAwait(false);
                await ProcessDatagramAsync(received.Buffer, token).ConfigureAwait(false);
            }
        }

        private async Task ProcessDatagramAsync(byte[] datagram, CancellationToken token)
        {
            if (datagram.Length != DatagramLength ||
                datagram[0] != Magic[0] || datagram[1] != Magic[1] ||
                datagram[2] != Magic[2] || datagram[3] != Magic[3])
                return;

            string id = Convert.ToHexString(datagram.AsSpan(4, 16));
            if (!_bindings.TryGetValue(id, out Binding? binding) ||
                !binding.Session.AuthenticatedUserId.HasValue ||
                binding.Session.UdpSessionId != id)
                return;

            byte[] expectedMac = HMACSHA256.HashData(binding.Key, datagram.AsSpan(0, SignedPayloadLength));
            if (!CryptographicOperations.FixedTimeEquals(
                expectedMac,
                datagram.AsSpan(SignedPayloadLength, MacLength)))
                return;

            uint sequence = BinaryPrimitives.ReadUInt32BigEndian(datagram.AsSpan(20, 4));
            float throttle = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(datagram.AsSpan(32, 4)));
            float steer = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(datagram.AsSpan(36, 4)));
            byte brakeByte = datagram[40];
            int vehicleIndex = datagram[41];
            if (sequence == 0 || brakeByte > 1 || vehicleIndex > 100)
                return;

            DateTime now = DateTime.UtcNow;
            if (binding.LastAcceptedAt != default &&
                now - binding.LastAcceptedAt < TimeSpan.FromMilliseconds(5))
                return;

            if (!TcpGameRoomRegistry.SubmitUdpInput(
                binding.Session.AuthenticatedUserId.Value,
                sequence,
                throttle,
                steer,
                brakeByte == 1,
                vehicleIndex))
                return;

            binding.LastAcceptedAt = now;
            await _awardWinner(binding.Session.AuthenticatedUserId.Value, token).ConfigureAwait(false);
        }

        public void RemoveSession(ClientSession session)
        {
            string? id = session.UdpSessionId;
            session.UdpSessionId = null;
            byte[]? sessionKey = session.UdpKey;
            session.UdpKey = null;
            if (id != null && _bindings.TryRemove(id, out Binding? binding))
                CryptographicOperations.ZeroMemory(binding.Key);
            else if (sessionKey != null)
                CryptographicOperations.ZeroMemory(sessionKey);
        }

        public void Dispose()
        {
            _client.Dispose();
            foreach (Binding binding in _bindings.Values)
                CryptographicOperations.ZeroMemory(binding.Key);
            _bindings.Clear();
        }

        private sealed class Binding
        {
            public Binding(ClientSession session, byte[] key)
            {
                Session = session;
                Key = key;
            }

            public ClientSession Session { get; }
            public byte[] Key { get; }
            public DateTime LastAcceptedAt { get; set; }
        }
    }
}
