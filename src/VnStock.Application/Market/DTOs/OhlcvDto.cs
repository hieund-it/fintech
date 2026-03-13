namespace VnStock.Application.Market.DTOs;

public record OhlcvDto(
    string Symbol,
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);
