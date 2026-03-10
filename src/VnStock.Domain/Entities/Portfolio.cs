namespace VnStock.Domain.Entities;

/// <summary>A named portfolio belonging to a user. Contains multiple transactions.</summary>
public class Portfolio
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    /// <summary>User-defined portfolio name.</summary>
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Transaction> Transactions { get; set; } = [];
}
