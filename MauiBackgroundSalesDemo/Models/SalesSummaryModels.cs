namespace MauiBackgroundSalesDemo.Models;

public class DailySalesSummaryItem
{
    public string DateKey { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
}

public class ProductSalesSummaryItem
{
    public string Product { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int UnitsSold { get; set; }
}

public class CategorySalesSummaryItem
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int UnitsSold { get; set; }
}