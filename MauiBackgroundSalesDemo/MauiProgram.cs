using MauiBackgroundSalesDemo.Services;
using MauiBackgroundSalesDemo.ViewModels;
using MauiBackgroundSalesDemo.Views;
using Microsoft.Extensions.Logging;

namespace MauiBackgroundSalesDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Database stores (files live in the app data folder on every platform)
        builder.Services.AddSingleton(new LiteDbSalesStore(
            Path.Combine(FileSystem.AppDataDirectory, "sales-transactions.db")));
        builder.Services.AddSingleton(new SqliteSalesAggregateStore(
            Path.Combine(FileSystem.AppDataDirectory, "sales-aggregates.db")));

        // Background ETL service
        builder.Services.AddSingleton<SalesAggregationBackgroundService>();

        // View models
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<SalesViewModel>();

        // Pages
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<SalesPage>();

        var app = builder.Build();

        // MAUI does not auto-start IHostedService instances, so start it manually.
        var aggregationService = app.Services.GetRequiredService<SalesAggregationBackgroundService>();
        _ = aggregationService.StartAsync(CancellationToken.None);

        return app;
    }
}