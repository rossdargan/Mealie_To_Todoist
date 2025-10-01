using System.Threading.Channels;

public class SyncTriggerChannel
{
    private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>();

    public void Trigger()
    {
        _channel.Writer.TryWrite(true);
    }

    public ChannelReader<bool> Reader => _channel.Reader;
}