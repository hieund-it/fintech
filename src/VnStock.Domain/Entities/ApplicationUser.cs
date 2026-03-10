using Microsoft.AspNetCore.Identity;

namespace VnStock.Domain.Entities;

/// <summary>
/// Extended application user with additional profile fields.
/// Inherits from IdentityUser with Guid primary key.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
