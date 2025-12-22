using MealieToTodoist.Domain;
public class ShoppingListSyncBackgroundService : BackgroundService
{
    private PeriodicTimer _timer = new(TimeSpan.FromHours(2));
    private readonly SyncService _syncService;
    private readonly SyncTriggerChannel _syncTriggerChannel;
    private readonly ILogger<ShoppingListSyncBackgroundService> _logger;

    public ShoppingListSyncBackgroundService(SyncService syncService, SyncTriggerChannel syncTriggerChannel, ILogger<ShoppingListSyncBackgroundService> logger)
    {
        _syncService = syncService;
        _syncTriggerChannel = syncTriggerChannel;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int debounceMilliseconds = 5000;
        while (!stoppingToken.IsCancellationRequested)
        {
            Task timerTask;
            try
            {
                if (_timer == null)
                {
                    _timer = new PeriodicTimer(TimeSpan.FromHours(2));
                }
                timerTask = _timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
            catch (InvalidOperationException)
            {
                _timer?.Dispose();
                _timer = new PeriodicTimer(TimeSpan.FromHours(2));
                continue;
            }

            // CRITICAL FIX: Use WaitToReadAsync instead of ReadAsync
            // This checks if data is available without consuming it
            var syncTask = _syncTriggerChannel.Reader.WaitToReadAsync(stoppingToken).AsTask();

            var completedTask = await Task.WhenAny(timerTask, syncTask);

            // Now determine what triggered and consume if necessary
            if (completedTask == syncTask)
            {
                try
                {
                    // WaitToReadAsync returns true if data is available
                    if (await syncTask)
                    {
                        // NOW consume the value since we know it's there
                        if (_syncTriggerChannel.Reader.TryRead(out _))
                        {
                            _logger.LogInformation("Manual trigger received via notification");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
            else
            {
                _logger.LogInformation("Timer trigger fired");
            }

            // Debounce logic: wait for 5 seconds of inactivity before syncing
            var debounceCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            try
            {
                _logger.LogInformation("Starting debounce window");

                while (true)
                {
                    var delayTask = Task.Delay(debounceMilliseconds, debounceCts.Token);
                    Task nextTimerTask = null;
                    try
                    {
                        nextTimerTask = _timer.WaitForNextTickAsync(debounceCts.Token).AsTask();
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }

                    var nextTriggerTask = _syncTriggerChannel.Reader.WaitToReadAsync(debounceCts.Token).AsTask();
                    var finished = await Task.WhenAny(delayTask, nextTimerTask, nextTriggerTask);

                    if (finished == delayTask)
                    {
                        _logger.LogInformation("Debounce complete, syncing now");
                        break;
                    }
                    else if (finished == nextTimerTask)
                    {
                        _logger.LogInformation("Timer ticked during debounce");
                        continue;
                    }
                    else if (finished == nextTriggerTask)
                    {
                        // Drain the trigger (consume the value)
                        if (_syncTriggerChannel.Reader.TryRead(out _))
                        {
                            _logger.LogInformation("Additional trigger during debounce, resetting timer");
                        }
                        continue;
                    }
                }

                _logger.LogInformation("Executing SyncShoppingList");
                await _syncService.SyncShoppingList();
                _logger.LogInformation("SyncShoppingList completed");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PeriodicTimer was in an invalid state during debounce. Recreating timer.");
                _timer?.Dispose();
                _timer = new PeriodicTimer(TimeSpan.FromHours(2));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during shopping list sync.");
            }
            finally
            {
                debounceCts.Dispose();
            }
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}