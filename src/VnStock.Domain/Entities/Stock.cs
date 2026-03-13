namespace VnStock.Domain.Entities;

/// <summary>
/// Represents a listed stock/security on VN exchanges.
/// </summary>
public class Stock
{
    public string Symbol { get; set; } = default!;     // e.g. "VCB"
    public string Name { get; set; } = default!;       // e.g. "Vietcombank"
    public string Exchange { get; set; } = default!;   // "HOSE" | "HNX" | "UPCOM"
    public string? Sector { get; set; }                // e.g. "Banking"

    public ICollection<OhlcvDaily> OhlcvHistory { get; set; } = [];
}
