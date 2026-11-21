using System;
using System.Net.Sockets;

namespace DanToPhong.Server
{
    public sealed class ClientSession
    {
        public string SessionId { get; } = Guid.NewGuid().ToString("N")[..8];
        public string RemoteEndPoint { get; init; } = "unknown";
        public TcpClient Client { get; init; } = null!;
        public string? AuthenticatedUsername { get; set; }
        public DateTime ConnectedAt { get; } = DateTime.Now;
    }
}