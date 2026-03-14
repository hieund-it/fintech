using VnStock.Application.Watchlist.DTOs;

namespace VnStock.Application.Watchlist.Services;

public interface IWatchlistService
{
    Task<IEnumerable<WatchlistItemDto>> GetAsync(Guid userId, CancellationToken ct = default);
    Task<WatchlistItemDto> AddAsync(Guid userId, string symbol, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default);
}
