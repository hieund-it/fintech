namespace VnStock.Application.Watchlist.DTOs;

/// <summary>Response DTO for a watchlist item with live price data.</summary>
public record WatchlistItemDto(
    Guid Id,
    string Symbol,
    DateTime AddedAt);

/// <summary>Request to add a symbol to the watchlist.</summary>
public record AddWatchlistRequest(string Symbol);
