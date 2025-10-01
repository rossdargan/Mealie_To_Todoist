using MealieToTodoist.Domain;
using MealieToTodoist.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            Task timerTask;
            try
            {
                timerTask = _timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
            catch (InvalidOperationException)
            {
                // Timer has been disposed or is in an invalid state, restart the timer and continue
                _timer.Dispose();
                _timer = new PeriodicTimer(TimeSpan.FromHours(2));
                continue;
            }

            var syncTask = _syncTriggerChannel.Reader.ReadAsync(stoppingToken).AsTask();

            var completedTask = await Task.WhenAny(timerTask, syncTask);

            if (completedTask == timerTask || completedTask == syncTask)
            {
                try
                {
                    await _syncService.SyncShoppingList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during shopping list sync.");
                }
            }
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}