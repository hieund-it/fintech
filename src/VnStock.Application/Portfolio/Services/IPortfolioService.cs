using VnStock.Application.Portfolio.DTOs;

namespace VnStock.Application.Portfolio.Services;

public interface IPortfolioService
{
    Task<IEnumerable<PortfolioDto>> GetPortfoliosAsync(Guid userId, CancellationToken ct = default);
    Task<PortfolioDto> CreatePortfolioAsync(Guid userId, string name, CancellationToken ct = default);
    Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId, CancellationToken ct = default);

    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid userId, Guid portfolioId, CancellationToken ct = default);
    Task<TransactionDto> AddTransactionAsync(Guid userId, Guid portfolioId, CreateTransactionRequest req, CancellationToken ct = default);
    Task<bool> DeleteTransactionAsync(Guid userId, Guid portfolioId, Guid transactionId, CancellationToken ct = default);

    /// <param name="currentPrices">Map of symbol → current price (from Redis/caller). Missing symbols fall back to last close.</param>
    Task<PortfolioPnLDto> GetPnLAsync(Guid userId, Guid portfolioId, IReadOnlyDictionary<string, decimal> currentPrices, CancellationToken ct = default);
}
