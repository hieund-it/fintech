using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PortfolioDto>>> GetAll(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return Ok(await _service.GetPortfoliosAsync(userId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioDto>> Create([FromBody] CreatePortfolioRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var portfolio = await _service.CreatePortfolioAsync(userId, req.Name, ct);
        return CreatedAtAction(nameof(GetAll), portfolio);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var deleted = await _service.DeletePortfolioAsync(userId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // --- Transactions ---

    [HttpGet("{portfolioId:guid}/transactions")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(Guid portfolioId, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return Ok(await _service.GetTransactionsAsync(userId, portfolioId, ct));
    }

    [HttpPost("{portfolioId:guid}/transactions")]
    public async Task<ActionResult<TransactionDto>> AddTransaction(
        Guid portfolioId, [FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        try
        {
            var txn = await _service.AddTransactionAsync(userId, portfolioId, req, ct);
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
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var deleted = await _service.DeleteTransactionAsync(userId, portfolioId, txnId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // --- P&L ---

    [HttpGet("{portfolioId:guid}/pnl")]
    public async Task<ActionResult<PortfolioPnLDto>> GetPnL(Guid portfolioId, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var currentPrices = await FetchCurrentPricesFromRedisAsync(ct);
        try
        {
            var pnl = await _service.GetPnLAsync(userId, portfolioId, currentPrices, ct);
            return Ok(pnl);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Reads latest prices from Redis keys "price:{symbol}" set by the Python data service.
    /// Uses a single MGET batch call to avoid O(N) round trips (H1 fix).
    /// Falls back to empty dict on no keys; individual symbols missing return 0 unrealized P&L.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, decimal>> FetchCurrentPricesFromRedisAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        // Collect all price keys from SCAN
        var keys = new List<RedisKey>();
        await foreach (var key in server.KeysAsync(pattern: "price:*").WithCancellation(ct))
            keys.Add(key);

        if (keys.Count == 0)
            return new Dictionary<string, decimal>();

        // Single MGET batch — one round trip regardless of symbol count
        var values = await db.StringGetAsync(keys.ToArray());
        var result = new Dictionary<string, decimal>(keys.Count);
        for (var i = 0; i < keys.Count; i++)
        {
            if (values[i].HasValue && decimal.TryParse(values[i].ToString(), out var price))
            {
                var symbol = keys[i].ToString()["price:".Length..].ToUpper();
                result[symbol] = price;
            }
        }

        return result;
    }
}
