namespace VnStock.Domain.Entities;

public enum TransactionType { BUY, SELL }

/// <summary>
/// A buy or sell transaction within a portfolio.
/// Price and Fee are in VND. Quantity supports fractional shares.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;
    public string Symbol { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    /// <summary>Number of shares (supports fractional).</summary>
    public decimal Quantity { get; set; }
    /// <summary>Price per share in VND.</summary>
    public decimal Price { get; set; }
    /// <summary>Brokerage fee in VND. Defaults to 0.</summary>
    public decimal Fee { get; set; } = 0;
    /// <summary>Actual trade date (user input, not record creation time).</summary>
    public DateTime TransactedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
