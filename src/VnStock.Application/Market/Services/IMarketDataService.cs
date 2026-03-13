using VnStock.Application.Market.DTOs;

namespace VnStock.Application.Market.Services;

/// <summary>
/// Market data provider abstraction — Phase 5: swap implementation for international markets.
/// </summary>
public interface IMarketDataService
{
    Task<IEnumerable<StockDto>> GetStocksAsync(string? exchange = null, string? q = null, string? sector = null);
    Task<StockDto?> GetStockAsync(string symbol);
    Task<IEnumerable<OhlcvDto>> GetOhlcvAsync(string symbol, DateOnly from, DateOnly to);
    Task<IEnumerable<string>> GetSectorsAsync();
}
