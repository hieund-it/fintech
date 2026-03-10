using VnStock.Domain.Entities;

namespace VnStock.Domain.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    string HashToken(string token);
}
