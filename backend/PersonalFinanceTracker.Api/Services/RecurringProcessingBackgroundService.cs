using Microsoft.Extensions.DependencyInjection;

namespace PersonalFinanceTracker.Api.Services;

public class RecurringProcessingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringProcessingBackgroundService> _logger;

    public RecurringProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringProcessingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var recurringService = scope.ServiceProvider.GetRequiredService<IRecurringService>();
                await recurringService.ProcessDueItemsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process recurring transactions.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
