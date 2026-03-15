using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VnStock.API.Hubs;
using VnStock.Application.Market.DTOs;

namespace VnStock.API.Services;

/// <summary>
/// Background service that subscribes to Redis "ticks:*" pub/sub channels and
/// forwards messages to SignalR groups, throttled to 1 update/second per symbol.
/// </summary>
public class RedisMarketDataSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<RedisMarketDataSubscriber> _logger;

    // Last broadcast time (Ticks) per symbol — ConcurrentDictionary for thread-safety
    private readonly ConcurrentDictionary<string, long> _lastBroadcast = new();
    private const long ThrottleIntervalTicks = TimeSpan.TicksPerSecond; // 1 second

    public RedisMarketDataSubscriber(
        IConnectionMultiplexer redis,
        IHubContext<MarketHub> hubContext,
        ILogger<RedisMarketDataSubscriber> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        // Subscribe to all tick channels: "ticks:{symbol}"
        await subscriber.SubscribeAsync(
            RedisChannel.Pattern("ticks:*"),
            OnTickReceived);

        _logger.LogInformation("RedisMarketDataSubscriber started — listening on ticks:*");

        // Hold until the host shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    private void OnTickReceived(RedisChannel channel, RedisValue message)
    {
        // Channel format: "ticks:{SYMBOL}"
        var channelStr = channel.ToString();
        var symbol = channelStr.Length > 6 ? channelStr[6..].ToUpper() : null;

        if (symbol is null || message.IsNullOrEmpty)
            return;

        // Throttle: skip if last broadcast was less than 1 second ago.
        // TryUpdate performs an atomic CAS — only the first thread to win the update proceeds,
        // preventing two concurrent callbacks from both broadcasting the same tick.
        var now = DateTime.UtcNow.Ticks;
        var last = _lastBroadcast.GetOrAdd(symbol, 0L);
        if (now - last < ThrottleIntervalTicks)
            return;

        if (!_lastBroadcast.TryUpdate(symbol, now, last))
            return;

        try
        {
            var tick = JsonSerializer.Deserialize<TickDto>(message.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tick is null)
                return;

            // Fire-and-forget broadcast to SignalR group for this symbol
            _ = _hubContext.Clients
                .Group(symbol)
                .SendAsync("ReceiveTick", tick);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize or broadcast tick for {Symbol}", symbol);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.UnsubscribeAsync(RedisChannel.Pattern("ticks:*"));
        _logger.LogInformation("RedisMarketDataSubscriber stopped.");
        await base.StopAsync(cancellationToken);
    }
}
