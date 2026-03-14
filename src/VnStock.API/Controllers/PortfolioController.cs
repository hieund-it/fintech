using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using VnStock.Application.Market.DTOs;
using VnStock.Application.Portfolio.DTOs;
using VnStock.Application.Portfolio.Services;

namespace VnStock.API.Controllers;

[ApiController]
[Route("api/portfolios")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _service;
    private readonly IConnectionMultiplexer _redis;

    public PortfolioController(IPortfolioService service, IConnectionMultiplexer redis)
    {
        _service = service;
        _redis = redis;
    }

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IEnumerable<PortfolioDto>> GetAll(CancellationToken ct)
        => await _service.GetPortfoliosAsync(UserId, ct);

    [HttpPost]
    public async Task<ActionResult<PortfolioDto>> Create([FromBody] CreatePortfolioRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var portfolio = await _service.CreatePortfolioAsync(UserId, req.Name, ct);
        return CreatedAtAction(nameof(GetAll), portfolio);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeletePortfolioAsync(UserId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // --- Transactions ---

    [HttpGet("{portfolioId:guid}/transactions")]
    public async Task<IEnumerable<TransactionDto>> GetTransactions(Guid portfolioId, CancellationToken ct)
        => await _service.GetTransactionsAsync(UserId, portfolioId, ct);

    [HttpPost("{portfolioId:guid}/transactions")]
    public async Task<ActionResult<TransactionDto>> AddTransaction(
        Guid portfolioId, [FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        try
        {
            var txn = await _service.AddTransactionAsync(UserId, portfolioId, req, ct);
            return CreatedAtAction(nameof(GetTransactions), new { portfolioId }, txn);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{portfolioId:guid}/transactions/{txnId:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid portfolioId, Guid txnId, CancellationToken ct)
    {
        var deleted = await _service.DeleteTransactionAsync(UserId, portfolioId, txnId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // --- P&L ---

    [HttpGet("{portfolioId:guid}/pnl")]
    public async Task<ActionResult<PortfolioPnLDto>> GetPnL(Guid portfolioId, CancellationToken ct)
    {
        // Fetch current prices from Redis (key pattern: "price:{SYMBOL}")
        var currentPrices = await FetchCurrentPricesFromRedisAsync(ct);
        try
        {
            var pnl = await _service.GetPnLAsync(UserId, portfolioId, currentPrices, ct);
            return Ok(pnl);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Reads latest prices from Redis keys "price:{symbol}" set by the Python data service.
    /// Falls back to 0 for symbols not yet cached (P&L will show 0 unrealized until price arrives).
    /// </summary>
    private async Task<IReadOnlyDictionary<string, decimal>> FetchCurrentPricesFromRedisAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var result = new Dictionary<string, decimal>();

        // Get all price keys in one scan pass (non-blocking)
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: "price:*").WithCancellation(ct))
        {
            var value = await db.StringGetAsync(key);
            if (value.HasValue && decimal.TryParse(value.ToString(), out var price))
            {
                var symbol = key.ToString()["price:".Length..].ToUpper();
                result[symbol] = price;
            }
        }

        return result;
    }
}
