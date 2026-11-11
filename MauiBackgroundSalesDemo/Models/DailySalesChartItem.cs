public class DailySalesChartItem
{
    public string DateLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public double RelativeVolume { get; set; }
    public string RevenueDisplay => Revenue.ToString("C");
}