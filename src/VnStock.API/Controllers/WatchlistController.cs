using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VnStock.Application.Watchlist.DTOs;
using VnStock.Application.Watchlist.Services;

namespace VnStock.API.Controllers;

[ApiController]
[Route("api/watchlist")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _service;

    public WatchlistController(IWatchlistService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WatchlistItemDto>>> Get(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return Ok(await _service.GetAsync(userId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<WatchlistItemDto>> Add([FromBody] AddWatchlistRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Symbol))
            return BadRequest(new { error = "Symbol is required." });

        var item = await _service.AddAsync(userId, req.Symbol, ct);
        return CreatedAtAction(nameof(Get), item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var removed = await _service.RemoveAsync(userId, id, ct);
        return removed ? NoContent() : NotFound();
    }
}
