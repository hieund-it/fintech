using Microsoft.EntityFrameworkCore;
using VnStock.Application.Market.DTOs;
using VnStock.Domain.Entities;

namespace VnStock.Application.Market.Services;

public class MarketDataService : IMarketDataService
{
    private readonly IMarketDbContext _db;

    public MarketDataService(IMarketDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StockDto>> GetStocksAsync(
        string? exchange = null, string? q = null, string? sector = null)
    {
        var query = _db.Stocks.AsQueryable();

        if (exchange is not null)
            query = query.Where(s => s.Exchange == exchange.ToUpper());

        if (q is not null)
            query = query.Where(s =>
                s.Symbol.Contains(q.ToUpper()) ||
                s.Name.ToLower().Contains(q.ToLower()));

        if (sector is not null)
            query = query.Where(s => s.Sector == sector);

        return await query
            .OrderBy(s => s.Symbol)
            .Select(s => new StockDto(s.Symbol, s.Name, s.Exchange, s.Sector))
            .ToListAsync();
    }

    public async Task<StockDto?> GetStockAsync(string symbol)
    {
        var s = await _db.Stocks.FindAsync(symbol);
        return s is null ? null : new StockDto(s.Symbol, s.Name, s.Exchange, s.Sector);
    }

    public async Task<IEnumerable<OhlcvDto>> GetOhlcvAsync(string symbol, DateOnly from, DateOnly to)
    {
        return await _db.OhlcvDaily
            .Where(o => o.Symbol == symbol && o.Date >= from && o.Date <= to)
            .OrderBy(o => o.Date)
            .Select(o => new OhlcvDto(o.Symbol, o.Date, o.Open, o.High, o.Low, o.Close, o.Volume))
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetSectorsAsync()
    {
        return await _db.Stocks
            .Where(s => s.Sector != null)
            .Select(s => s.Sector!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }
}
