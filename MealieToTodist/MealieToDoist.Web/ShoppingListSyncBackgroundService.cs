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
                // Recreate the timer if it has been disposed or is in an invalid state
                if (_timer == null)
                {
                    _timer = new PeriodicTimer(TimeSpan.FromHours(2));
                }
                timerTask = _timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
            catch (InvalidOperationException)
            {
                // Timer has been disposed or is in an invalid state, restart the timer and continue
                _timer?.Dispose();
                _timer = new PeriodicTimer(TimeSpan.FromHours(2));
                continue;
            }

            var syncTask = _syncTriggerChannel.Reader.ReadAsync(stoppingToken).AsTask();

            var completedTask = await Task.WhenAny(timerTask, syncTask);

            // Debounce logic: wait for 5 seconds of inactivity before syncing
            var debounceCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            try
            {
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
                        // Timer is in an invalid state, break out of debounce and recreate timer in outer catch
                        break;
                    }
                    var nextTriggerTask = _syncTriggerChannel.Reader.WaitToReadAsync(debounceCts.Token).AsTask();

                    var finished = await Task.WhenAny(delayTask, nextTimerTask, nextTriggerTask);
                    if (finished == delayTask)
                    {
                        // No new trigger or timer within debounce window, proceed to sync
                        break;
                    }
                    else if (finished == nextTimerTask)
                    {
                        // Timer ticked again, continue debounce
                        continue;
                    }
                    else if (finished == nextTriggerTask)
                    {
                        // Drain the trigger (consume the value)
                        if (_syncTriggerChannel.Reader.TryRead(out _)) { }
                        // Continue waiting for another debounce window
                        continue;
                    }
                }

                await _syncService.SyncShoppingList();
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