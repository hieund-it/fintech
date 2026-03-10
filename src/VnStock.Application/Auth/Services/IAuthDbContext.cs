using Microsoft.EntityFrameworkCore;
using VnStock.Domain.Entities;

namespace VnStock.Application.Auth.Services;

/// <summary>Interface for DB access in Application layer — avoids Infrastructure dependency from tests.</summary>
public interface IAuthDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
