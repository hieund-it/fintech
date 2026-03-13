using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VnStock.API.Hubs;

/// <summary>
/// Real-time market data hub. Clients subscribe/unsubscribe per symbol.
/// Requires JWT auth. Max 50 symbols per connection to prevent abuse.
/// </summary>
[Authorize]
public class MarketHub : Hub
{
    private const int MaxSymbolsPerConnection = 50;

    // Track how many symbols each connection is subscribed to
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _subscriptionCount = new();

    public async Task SubscribeSymbol(string symbol)
    {
        var connectionId = Context.ConnectionId;

        // CAS loop: atomically check-and-increment to prevent exceeding the cap
        // under concurrent calls from the same connection.
        while (true)
        {
            var current = _subscriptionCount.GetOrAdd(connectionId, 0);
            if (current >= MaxSymbolsPerConnection)
                throw new HubException($"Max {MaxSymbolsPerConnection} symbols per connection.");

            if (_subscriptionCount.TryUpdate(connectionId, current + 1, current))
                break;
        }

        var normalised = symbol.ToUpper();
        await Groups.AddToGroupAsync(connectionId, normalised);
    }

    public async Task UnsubscribeSymbol(string symbol)
    {
        var normalised = symbol.ToUpper();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, normalised);

        _subscriptionCount.AddOrUpdate(
            Context.ConnectionId,
            0,
            (_, current) => Math.Max(0, current - 1));
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _subscriptionCount.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }
}
