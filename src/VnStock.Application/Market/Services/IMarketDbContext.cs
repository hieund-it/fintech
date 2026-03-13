using Microsoft.EntityFrameworkCore;
using VnStock.Domain.Entities;

namespace VnStock.Application.Market.Services;

/// <summary>Interface for market data DB access — decouples Application from Infrastructure.</summary>
public interface IMarketDbContext
{
    DbSet<Stock> Stocks { get; }
    DbSet<OhlcvDaily> OhlcvDaily { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
