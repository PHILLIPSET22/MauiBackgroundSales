using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiBackgroundSalesDemo.Services;

namespace MauiBackgroundSalesDemo.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly SqliteSalesAggregateStore _aggregateStore;

    public ObservableCollection<DailySalesChartItem> DailySales { get; } = new();
    public ObservableCollection<ProductSalesChartItem> ProductSales { get; } = new();
    public ObservableCollection<CategorySalesChartItem> CategorySales { get; } = new();

    [ObservableProperty]
    private decimal totalRevenue;

    [ObservableProperty]
    private int totalTransactions;

    [ObservableProperty]
    private decimal averageSale;

    [ObservableProperty]
    private string topProduct = "Waiting for data...";

    public DashboardViewModel(SqliteSalesAggregateStore aggregateStore)
    {
        _aggregateStore = aggregateStore;
    }

    public async Task RefreshAsync()
    {
        var dailySummary = await _aggregateStore.GetDailySalesAsync();
        var productSummary = await _aggregateStore.GetProductSalesAsync();
        var categorySummary = await _aggregateStore.GetCategorySalesAsync();

        DailySales.Clear();
        ProductSales.Clear();
        CategorySales.Clear();

        if (dailySummary.Count == 0)
        {
            TotalRevenue = 0m;
            TotalTransactions = 0;
            AverageSale = 0m;
            TopProduct = "No sales yet";
            return;
        }

        var maxDailyRevenue = dailySummary.Max(x => x.TotalRevenue);
        var maxProductRevenue = productSummary.Count > 0 ? productSummary.Max(x => x.TotalRevenue) : 1m;
        var maxCategoryRevenue = categorySummary.Count > 0 ? categorySummary.Max(x => x.TotalRevenue) : 1m;

        foreach (var item in dailySummary)
        {
            var displayedDate = DateTime.TryParse(item.DateKey, out var date)
                ? date.ToString("MMM d")
                : item.DateKey;

            DailySales.Add(new DailySalesChartItem
            {
                DateLabel = displayedDate,
                Revenue = item.TotalRevenue,
                RelativeVolume = maxDailyRevenue == 0 ? 0d : (double)(item.TotalRevenue / maxDailyRevenue)
            });
        }

        foreach (var item in productSummary)
        {
            ProductSales.Add(new ProductSalesChartItem
            {
                Product = item.Product,
                Revenue = item.TotalRevenue,
                UnitsSold = item.UnitsSold,
                RelativeVolume = maxProductRevenue == 0 ? 0d : (double)(item.TotalRevenue / maxProductRevenue)
            });
        }

        foreach (var item in categorySummary)
        {
            CategorySales.Add(new CategorySalesChartItem
            {
                Category = item.Category,
                Revenue = item.TotalRevenue,
                UnitsSold = item.UnitsSold,
                RelativeVolume = maxCategoryRevenue == 0 ? 0d : (double)(item.TotalRevenue / maxCategoryRevenue)
            });
        }

        TotalRevenue = dailySummary.Sum(x => x.TotalRevenue);
        TotalTransactions = dailySummary.Sum(x => x.TransactionCount);
        AverageSale = TotalTransactions == 0 ? 0m : TotalRevenue / TotalTransactions;
        TopProduct = productSummary.OrderByDescending(x => x.TotalRevenue).FirstOrDefault()?.Product ?? "No sales yet";
    }
}