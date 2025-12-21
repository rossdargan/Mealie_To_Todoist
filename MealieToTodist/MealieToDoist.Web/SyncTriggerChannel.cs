using System.Threading.Channels;

public class SyncTriggerChannel
{
    private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>();
    private readonly ILogger<SyncTriggerChannel> _logger;

    public SyncTriggerChannel(ILogger<SyncTriggerChannel> logger)
    {
        _logger = logger;
    }

    public void Trigger()
    {
        try
        {
            if (_channel.Writer.TryWrite(true))
            {
                _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Trigger written successfully");
            }
            else
            {
                _logger.LogError($"[{DateTime.Now:HH:mm:ss}] Failed to write trigger! Channel may be completed or closed.");

                // Check the state of the channel
                if (_channel.Writer.TryComplete())
                {
                    _logger.LogError("Channel writer was not yet completed, but write still failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while trying to write to channel");
        }
    }

    public ChannelReader<bool> Reader => _channel.Reader;
}