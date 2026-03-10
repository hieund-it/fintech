namespace VnStock.Domain.Entities;

/// <summary>
/// A stock symbol in a user's watchlist.
/// Unique constraint: one entry per user+symbol pair.
/// </summary>
public class WatchlistItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    /// <summary>Stock ticker e.g. "VCB", "HPG"</summary>
    public string Symbol { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
