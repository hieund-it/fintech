using VnStock.Domain.Entities;

namespace VnStock.Domain.Interfaces;

public interface IAuthService
{
    Task<(ApplicationUser user, string accessToken, string refreshToken)> RegisterAsync(string email, string password, string displayName, CancellationToken ct = default);
    Task<(ApplicationUser user, string accessToken, string refreshToken)> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<(ApplicationUser user, string accessToken, string refreshToken)> ExternalLoginAsync(string provider, string providerKey, string email, string displayName, string? avatarUrl = null, CancellationToken ct = default);
    Task<(string accessToken, string newRefreshToken)> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
