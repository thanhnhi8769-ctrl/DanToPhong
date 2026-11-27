using System.Threading;
using System.Threading.Channels;
using DanToPhong.Protocol;

namespace DanToPhong.Server;

internal sealed class GameClientMailbox : IDisposable
{
    private readonly Channel<ResponsePacket> _importantEvents;
    private readonly Channel<ResponsePacket> _snapshots;
    private readonly SemaphoreSlim _ready = new(0);
    private int _snapshotSignalPending;

    public GameClientMailbox(int eventCapacity = 64)
    {
        _importantEvents = Channel.CreateBounded<ResponsePacket>(new BoundedChannelOptions(eventCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });
        _snapshots = Channel.CreateBounded<ResponsePacket>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });
    }

    public ValueTask QueueImportantEventAsync(ResponsePacket message, CancellationToken token) =>
        QueueEventAndSignalAsync(message, token);

    private async ValueTask QueueEventAndSignalAsync(ResponsePacket message, CancellationToken token)
    {
        await _importantEvents.Writer.WriteAsync(message, token).ConfigureAwait(false);
        _ready.Release();
    }

    public bool TryQueueSnapshot(ResponsePacket message)
    {
        if (!_snapshots.Writer.TryWrite(message))
            return false;

        if (Interlocked.Exchange(ref _snapshotSignalPending, 1) == 0)
            _ready.Release();
        return true;
    }

    public bool TryReadImportantEvent(out ResponsePacket message) =>
        _importantEvents.Reader.TryRead(out message!);

    public bool TryReadSnapshot(out ResponsePacket message)
    {
        Interlocked.Exchange(ref _snapshotSignalPending, 0);
        return _snapshots.Reader.TryRead(out message!);
    }

    public bool IsCompleted =>
        _importantEvents.Reader.Completion.IsCompleted &&
        _snapshots.Reader.Completion.IsCompleted;

    public Task WaitForDataAsync(CancellationToken token) => _ready.WaitAsync(token);

    public void Complete()
    {
        _importantEvents.Writer.TryComplete();
        _snapshots.Writer.TryComplete();
        _ready.Release();
    }

    public void Dispose() => _ready.Dispose();
}
