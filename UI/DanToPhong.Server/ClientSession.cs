using System;
using System.Net.Sockets;
using System.Threading;

namespace DanToPhong.Server
{
    public sealed class ClientSession
    {
        public string SessionId { get; } = Guid.NewGuid().ToString("N")[..8];
        public string RemoteEndPoint { get; init; } = "unknown";
        public TcpClient Client { get; init; } = null!;
        public int? AuthenticatedUserId { get; set; }
        public string? AuthenticatedUsername { get; set; }
        public string? AuthToken { get; set; }
        public DateTime ConnectedAt { get; } = DateTime.Now;
        public SemaphoreSlim SendLock { get; } = new(1, 1);
    }
}
