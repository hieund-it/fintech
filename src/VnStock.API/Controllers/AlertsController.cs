using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VnStock.Application.Alerts.DTOs;
using VnStock.Application.Alerts.Services;

namespace VnStock.API.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _service;

    public AlertsController(IAlertService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> Get(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return Ok(await _service.GetAsync(userId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AlertDto>> Create([FromBody] CreateAlertRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        try
        {
            var alert = await _service.CreateAsync(userId, req, ct);
            return CreatedAtAction(nameof(Get), alert);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var deleted = await _service.DeleteAsync(userId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        var updated = await _service.DeactivateAsync(userId, id, ct);
        return updated ? NoContent() : NotFound();
    }
}
