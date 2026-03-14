using Microsoft.EntityFrameworkCore;
using VnStock.Domain.Entities;

namespace VnStock.Application.Alerts.Services;

public interface IAlertDbContext
{
    DbSet<PriceAlert> PriceAlerts { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
