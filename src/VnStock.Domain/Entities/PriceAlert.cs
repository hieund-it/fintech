namespace VnStock.Domain.Entities;

public enum AlertDirection { ABOVE, BELOW }

/// <summary>
/// Price alert: triggers when symbol price crosses threshold in specified direction.
/// Partial index on (Symbol, IsActive) optimises alert engine scan per tick.
/// </summary>
public class PriceAlert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Symbol { get; set; } = string.Empty;
    public AlertDirection Direction { get; set; }
    /// <summary>VND price threshold to trigger alert.</summary>
    public decimal Threshold { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Null until the alert fires.</summary>
    public DateTime? TriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
