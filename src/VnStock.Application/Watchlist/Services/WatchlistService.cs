using Microsoft.EntityFrameworkCore;
using VnStock.Application.Watchlist.DTOs;
using VnStock.Domain.Entities;

namespace VnStock.Application.Watchlist.Services;

public class WatchlistService : IWatchlistService
{
    private readonly IWatchlistDbContext _db;

    public WatchlistService(IWatchlistDbContext db) => _db = db;

    public async Task<IEnumerable<WatchlistItemDto>> GetAsync(Guid userId, CancellationToken ct = default)
        => await _db.Watchlists
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.CreatedAt)
            .Select(w => new WatchlistItemDto(w.Id, w.Symbol, w.CreatedAt))
            .ToListAsync(ct);

    public async Task<WatchlistItemDto> AddAsync(Guid userId, string symbol, CancellationToken ct = default)
    {
        var upper = symbol.ToUpper();

        // Idempotent: return existing if already present
        var existing = await _db.Watchlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Symbol == upper, ct);

        if (existing is not null)
            return new WatchlistItemDto(existing.Id, existing.Symbol, existing.CreatedAt);

        var item = new WatchlistItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = upper,
            CreatedAt = DateTime.UtcNow
        };

        _db.Watchlists.Add(item);
        await _db.SaveChangesAsync(ct);
        return new WatchlistItemDto(item.Id, item.Symbol, item.CreatedAt);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.Watchlists
            .FirstOrDefaultAsync(w => w.Id == itemId && w.UserId == userId, ct);

        if (item is null) return false;

        _db.Watchlists.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
