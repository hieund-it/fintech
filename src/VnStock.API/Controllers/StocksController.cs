using Microsoft.AspNetCore.Mvc;
using VnStock.Application.Market.DTOs;
using VnStock.Application.Market.Services;

namespace VnStock.API.Controllers;

[ApiController]
[Route("api/stocks")]
public class StocksController : ControllerBase
{
    private readonly IMarketDataService _market;

    public StocksController(IMarketDataService market)
    {
        _market = market;
    }

    /// <summary>List all stocks, optionally filtered by exchange, text search, or sector.</summary>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "exchange", "q", "sector" })]
    public async Task<IEnumerable<StockDto>> GetStocks(
        [FromQuery] string? exchange,
        [FromQuery] string? q,
        [FromQuery] string? sector)
        => await _market.GetStocksAsync(exchange, q, sector);

    /// <summary>Get all distinct sectors.</summary>
    [HttpGet("sectors")]
    [ResponseCache(Duration = 3600)]
    public async Task<IEnumerable<string>> GetSectors()
        => await _market.GetSectorsAsync();

    /// <summary>Get metadata for a single stock.</summary>
    [HttpGet("{symbol}")]
    public async Task<ActionResult<StockDto>> GetStock(string symbol)
    {
        var stock = await _market.GetStockAsync(symbol.ToUpper());
        return stock is null ? NotFound() : Ok(stock);
    }

    /// <summary>Get OHLCV daily bars for a symbol within a date range.</summary>
    [HttpGet("{symbol}/ohlcv")]
    public async Task<ActionResult<IEnumerable<OhlcvDto>>> GetOhlcv(
        string symbol,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        if (from > to)
            return BadRequest("'from' must be before 'to'.");

        var data = await _market.GetOhlcvAsync(symbol.ToUpper(), from, to);
        return Ok(data);
    }
}
