using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VnStock.Domain.Entities;
using VnStock.Domain.Interfaces;

namespace VnStock.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IAuthDbContext _dbContext;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IAuthDbContext dbContext)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    public async Task<(ApplicationUser user, string accessToken, string refreshToken)> RegisterAsync(
        string email, string password, string displayName, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(email) != null)
            throw new InvalidOperationException("Email already registered.");

        var user = new ApplicationUser { Email = email, UserName = email, DisplayName = displayName, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await CreateTokensAsync(user, ct);
    }

    public async Task<(ApplicationUser user, string accessToken, string refreshToken)> LoginAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await CreateTokensAsync(user, ct);
    }

    public async Task<(string accessToken, string newRefreshToken)> RefreshAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!stored.IsActive)
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        var (_, newAccess, newRefresh) = await CreateTokensAsync(stored.User, ct);
        return (newAccess, newRefresh);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);
        if (stored != null)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task<(ApplicationUser user, string accessToken, string refreshToken)> CreateTokensAsync(
        ApplicationUser user, CancellationToken ct)
    {
        var refreshToken = _tokenService.GenerateRefreshToken();
        var tokenHash = _tokenService.HashToken(refreshToken);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _dbContext.SaveChangesAsync(ct);

        return (user, _tokenService.GenerateAccessToken(user), refreshToken);
    }
}
