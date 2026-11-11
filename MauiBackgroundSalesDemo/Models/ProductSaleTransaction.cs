namespace MauiBackgroundSalesDemo.Models;

public class ProductSaleTransaction
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal Price { get; set; } = 0m;
    public decimal Total => Quantity * Price;
}