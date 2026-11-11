using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MauiBackgroundSalesDemo.Services;

public sealed class SalesAggregationBackgroundService : BackgroundService
{
    private readonly LiteDbSalesStore _liteDbSalesStore;
    private readonly SqliteSalesAggregateStore _sqliteSalesAggregateStore;
    private readonly ILogger<SalesAggregationBackgroundService> _logger;

    public SalesAggregationBackgroundService(
        LiteDbSalesStore liteDbSalesStore,
        SqliteSalesAggregateStore sqliteSalesAggregateStore,
        ILogger<SalesAggregationBackgroundService> logger)
    {
        _liteDbSalesStore = liteDbSalesStore;
        _sqliteSalesAggregateStore = sqliteSalesAggregateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _liteDbSalesStore.InitializeAsync(stoppingToken);
            await _sqliteSalesAggregateStore.InitializeAsync(stoppingToken);
            await _liteDbSalesStore.EnsureSeedDataAsync(stoppingToken);
            await RefreshAggregateAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAggregateAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sales aggregation background service stopped.");
        }
    }

    private async Task RefreshAggregateAsync(CancellationToken stoppingToken)
    {
        var transactions = await _liteDbSalesStore.GetTransactionsAsync(stoppingToken);

        if (transactions.Count == 0)
        {
            return;
        }

        await _sqliteSalesAggregateStore.UpdateFromTransactionsAsync(transactions, stoppingToken);
        _logger.LogInformation("Updated aggregate SQLite database with {Count} sales transactions.", transactions.Count);
    }
}