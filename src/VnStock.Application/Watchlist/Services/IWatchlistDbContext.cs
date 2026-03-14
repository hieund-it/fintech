using Microsoft.EntityFrameworkCore;
using VnStock.Domain.Entities;

namespace VnStock.Application.Watchlist.Services;

public interface IWatchlistDbContext
{
    DbSet<WatchlistItem> Watchlists { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
