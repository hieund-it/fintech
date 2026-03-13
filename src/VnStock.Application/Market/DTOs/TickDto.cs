namespace VnStock.Application.Market.DTOs;

/// <summary>
/// Real-time tick data broadcast via SignalR to subscribed clients.
/// </summary>
public record TickDto(
    string Symbol,
    DateTime Timestamp,
    decimal Price,
    long Volume,
    decimal ChangePct
);
