namespace VnStock.Application.Market.DTOs;

public record StockDto(
    string Symbol,
    string Name,
    string Exchange,
    string? Sector
);
