using VnStock.Application.Alerts.DTOs;

namespace VnStock.Application.Alerts.Services;

public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetAsync(Guid userId, CancellationToken ct = default);
    Task<AlertDto> CreateAsync(Guid userId, CreateAlertRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid alertId, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid userId, Guid alertId, CancellationToken ct = default);
}
