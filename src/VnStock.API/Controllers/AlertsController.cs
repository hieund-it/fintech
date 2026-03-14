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

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IEnumerable<AlertDto>> Get(CancellationToken ct)
        => await _service.GetAsync(UserId, ct);

    [HttpPost]
    public async Task<ActionResult<AlertDto>> Create([FromBody] CreateAlertRequest req, CancellationToken ct)
    {
        try
        {
            var alert = await _service.CreateAsync(UserId, req, ct);
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
        var deleted = await _service.DeleteAsync(UserId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var updated = await _service.DeactivateAsync(UserId, id, ct);
        return updated ? NoContent() : NotFound();
    }
}
