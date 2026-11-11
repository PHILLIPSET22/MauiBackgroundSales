public class CategorySalesChartItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int UnitsSold { get; set; }
    public double RelativeVolume { get; set; }
    public string RevenueDisplay => Revenue.ToString("C");
}