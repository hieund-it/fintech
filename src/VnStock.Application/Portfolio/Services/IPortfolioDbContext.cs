using Microsoft.EntityFrameworkCore;
using VnStock.Domain.Entities;

namespace VnStock.Application.Portfolio.Services;

public interface IPortfolioDbContext
{
    DbSet<VnStock.Domain.Entities.Portfolio> Portfolios { get; }
    DbSet<Transaction> Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
