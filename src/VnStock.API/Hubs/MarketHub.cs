using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace VnStock.API.Hubs;

/// <summary>
/// Real-time market data hub. Clients subscribe/unsubscribe per symbol.
/// Requires JWT auth. Max symbols per connection controlled by MarketHub:MaxSymbolsPerConnection config.
/// </summary>
[Authorize]
public class MarketHub : Hub
{
    private readonly int _maxSymbolsPerConnection;

    // Track how many symbols each connection is subscribed to
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _subscriptionCount = new();

    // Whitelist: 1–10 uppercase alphanumeric chars — prevents arbitrary string injection into SignalR group names
    private static readonly Regex SymbolRegex = new(@"^[A-Z0-9]{1,10}$", RegexOptions.Compiled);

    public MarketHub(IConfiguration config)
    {
        _maxSymbolsPerConnection = config.GetValue<int>("MarketHub:MaxSymbolsPerConnection", 50);
    }

    public async Task SubscribeSymbol(string symbol)
    {
        var connectionId = Context.ConnectionId;
        var normalised = symbol.Trim().ToUpper();

        // Validate symbol format to prevent arbitrary strings polluting SignalR group names
        if (!SymbolRegex.IsMatch(normalised))
            throw new HubException("Invalid symbol. Use 1–10 uppercase alphanumeric characters.");

        // CAS loop: atomically check-and-increment to prevent exceeding the cap
        // under concurrent calls from the same connection.
        while (true)
        {
            var current = _subscriptionCount.GetOrAdd(connectionId, 0);
            if (current >= _maxSymbolsPerConnection)
                throw new HubException($"Max {_maxSymbolsPerConnection} symbols per connection.");

            if (_subscriptionCount.TryUpdate(connectionId, current + 1, current))
                break;
        }

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
