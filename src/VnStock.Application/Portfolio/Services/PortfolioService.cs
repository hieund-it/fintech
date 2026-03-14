using Microsoft.EntityFrameworkCore;
using VnStock.Application.Portfolio.DTOs;
using VnStock.Domain.Entities;

namespace VnStock.Application.Portfolio.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioDbContext _db;

    public PortfolioService(IPortfolioDbContext db) => _db = db;

    public async Task<IEnumerable<PortfolioDto>> GetPortfoliosAsync(Guid userId, CancellationToken ct = default)
        => await _db.Portfolios
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PortfolioDto(p.Id, p.Name, p.CreatedAt))
            .ToListAsync(ct);

    public async Task<PortfolioDto> CreatePortfolioAsync(Guid userId, string name, CancellationToken ct = default)
    {
        var portfolio = new Domain.Entities.Portfolio
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync(ct);
        return new PortfolioDto(portfolio.Id, portfolio.Name, portfolio.CreatedAt);
    }

    public async Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId, CancellationToken ct = default)
    {
        var portfolio = await _db.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId, ct);
        if (portfolio is null) return false;

        _db.Portfolios.Remove(portfolio);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(
        Guid userId, Guid portfolioId, CancellationToken ct = default)
    {
        // Verify ownership
        var owned = await _db.Portfolios
            .AnyAsync(p => p.Id == portfolioId && p.UserId == userId, ct);
        if (!owned) return [];

        return await _db.Transactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactedAt)
            .Select(t => new TransactionDto(
                t.Id, t.PortfolioId, t.Symbol,
                t.Type.ToString(), t.Quantity, t.Price, t.Fee, t.TransactedAt))
            .ToListAsync(ct);
    }

    public async Task<TransactionDto> AddTransactionAsync(
        Guid userId, Guid portfolioId, CreateTransactionRequest req, CancellationToken ct = default)
    {
        var owned = await _db.Portfolios
            .AnyAsync(p => p.Id == portfolioId && p.UserId == userId, ct);
        if (!owned)
            throw new UnauthorizedAccessException("Portfolio not found.");

        if (!Enum.TryParse<TransactionType>(req.Type.ToUpper(), out var txType))
            throw new ArgumentException($"Invalid transaction type '{req.Type}'. Use BUY or SELL.");

        var txn = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            Symbol = req.Symbol.ToUpper(),
            Type = txType,
            Quantity = req.Quantity,
            Price = req.Price,
            Fee = req.Fee,
            TransactedAt = req.TransactedAt,
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(txn);
        await _db.SaveChangesAsync(ct);
        return new TransactionDto(txn.Id, txn.PortfolioId, txn.Symbol,
            txn.Type.ToString(), txn.Quantity, txn.Price, txn.Fee, txn.TransactedAt);
    }

    public async Task<bool> DeleteTransactionAsync(
        Guid userId, Guid portfolioId, Guid transactionId, CancellationToken ct = default)
    {
        // Join to verify ownership
        var txn = await _db.Transactions
            .Where(t => t.Id == transactionId && t.PortfolioId == portfolioId)
            .Join(_db.Portfolios.Where(p => p.UserId == userId),
                t => t.PortfolioId, p => p.Id, (t, _) => t)
            .FirstOrDefaultAsync(ct);

        if (txn is null) return false;

        _db.Transactions.Remove(txn);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PortfolioPnLDto> GetPnLAsync(
        Guid userId, Guid portfolioId,
        IReadOnlyDictionary<string, decimal> currentPrices,
        CancellationToken ct = default)
    {
        var portfolio = await _db.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId, ct)
            ?? throw new UnauthorizedAccessException("Portfolio not found.");

        var transactions = await _db.Transactions
            .Where(t => t.PortfolioId == portfolioId)
            .ToListAsync(ct);

        var positions = transactions
            .GroupBy(t => t.Symbol)
            .Select(g =>
            {
                var price = currentPrices.TryGetValue(g.Key, out var p) ? p : 0;
                return PnLCalculator.Calculate(g.Key, g, price);
            })
            .Where(pos => pos.Quantity > 0 || pos.RealizedPnL != 0)
            .ToList();

        return new PortfolioPnLDto(
            portfolio.Id,
            portfolio.Name,
            positions,
            positions.Sum(p => p.RealizedPnL),
            positions.Sum(p => p.UnrealizedPnL));
    }
}
