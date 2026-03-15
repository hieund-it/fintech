using System.ComponentModel.DataAnnotations;

namespace VnStock.Application.Portfolio.DTOs;

public record PortfolioDto(Guid Id, string Name, DateTime CreatedAt);

public record CreatePortfolioRequest(string Name);

public record TransactionDto(
    Guid Id,
    Guid PortfolioId,
    string Symbol,
    string Type,   // "BUY" | "SELL"
    decimal Quantity,
    decimal Price,
    decimal Fee,
    DateTime TransactedAt);

public record CreateTransactionRequest(
    [Required, MinLength(2), MaxLength(10)] string Symbol,
    [Required] string Type,
    [Range(0.0001, 1_000_000_000.0)] decimal Quantity,
    [Range(0.0001, 1_000_000_000.0)] decimal Price,
    [Range(0, 1_000_000_000.0)] decimal Fee,
    DateTime TransactedAt);

/// <summary>P&amp;L summary for one symbol position within a portfolio.</summary>
public record PositionDto(
    string Symbol,
    decimal Quantity,
    decimal AvgCost,
    decimal RealizedPnL,
    decimal UnrealizedPnL,
    decimal CurrentPrice);

/// <summary>Aggregated P&amp;L for an entire portfolio.</summary>
public record PortfolioPnLDto(
    Guid PortfolioId,
    string PortfolioName,
    IEnumerable<PositionDto> Positions,
    decimal TotalRealizedPnL,
    decimal TotalUnrealizedPnL);
