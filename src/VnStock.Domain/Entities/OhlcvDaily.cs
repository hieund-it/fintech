namespace VnStock.Domain.Entities;

/// <summary>
/// Daily OHLCV (Open/High/Low/Close/Volume) price bar for a stock.
/// </summary>
public class OhlcvDaily
{
    public int Id { get; set; }
    public string Symbol { get; set; } = default!;
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }

    public Stock Stock { get; set; } = default!;
}
