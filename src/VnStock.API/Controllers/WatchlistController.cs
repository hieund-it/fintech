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

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IEnumerable<WatchlistItemDto>> Get(CancellationToken ct)
        => await _service.GetAsync(UserId, ct);

    [HttpPost]
    public async Task<ActionResult<WatchlistItemDto>> Add([FromBody] AddWatchlistRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Symbol))
            return BadRequest(new { error = "Symbol is required." });

        var item = await _service.AddAsync(UserId, req.Symbol, ct);
        return CreatedAtAction(nameof(Get), item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var removed = await _service.RemoveAsync(UserId, id, ct);
        return removed ? NoContent() : NotFound();
    }
}
