using Microsoft.EntityFrameworkCore;
using VnStock.Application.Alerts.DTOs;
using VnStock.Domain.Entities;

namespace VnStock.Application.Alerts.Services;

public class AlertService : IAlertService
{
    private readonly IAlertDbContext _db;

    public AlertService(IAlertDbContext db) => _db = db;

    public async Task<IEnumerable<AlertDto>> GetAsync(Guid userId, CancellationToken ct = default)
        => await _db.PriceAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertDto(
                a.Id, a.Symbol, a.Direction.ToString(),
                a.Threshold, a.IsActive, a.TriggeredAt, a.CreatedAt))
            .ToListAsync(ct);

    private const int MaxAlertsPerUser = 100;

    public async Task<AlertDto> CreateAsync(Guid userId, CreateAlertRequest req, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AlertDirection>(req.Direction.ToUpper(), out var dir))
            throw new ArgumentException($"Invalid direction '{req.Direction}'. Use ABOVE or BELOW.");

        // Enforce per-user cap to prevent unbounded in-memory growth in AlertEngineService
        var activeCount = await _db.PriceAlerts.CountAsync(a => a.UserId == userId && a.IsActive, ct);
        if (activeCount >= MaxAlertsPerUser)
            throw new ArgumentException($"Maximum of {MaxAlertsPerUser} active alerts per user reached.");

        var alert = new PriceAlert
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = req.Symbol.ToUpper(),
            Direction = dir,
            Threshold = req.Threshold,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.PriceAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);
        return new AlertDto(alert.Id, alert.Symbol, alert.Direction.ToString(),
            alert.Threshold, alert.IsActive, alert.TriggeredAt, alert.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid alertId, CancellationToken ct = default)
    {
        var alert = await _db.PriceAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, ct);
        if (alert is null) return false;

        _db.PriceAlerts.Remove(alert);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid userId, Guid alertId, CancellationToken ct = default)
    {
        var alert = await _db.PriceAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId && a.IsActive, ct);
        if (alert is null) return false;

        alert.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
