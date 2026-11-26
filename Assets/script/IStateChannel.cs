using System.Threading.Tasks;

public interface IStateChannel
{
    Task<Resp> SendStateAsync(Req input);
    Task<Resp> SendEventAsync(Req gameEvent);
}

public sealed class TcpStateChannel : IStateChannel
{
    public Task<Resp> SendStateAsync(Req input)
    {
        return ServerClient.CallAsync(input);
    }

    public Task<Resp> SendEventAsync(Req gameEvent)
    {
        return ServerClient.CallAsync(gameEvent);
    }
}
