using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VnStock.Application.Alerts.Services;
using VnStock.Application.Market.DTOs;
using VnStock.Domain.Entities;
using VnStock.Infrastructure.Data;

namespace VnStock.API.Services;

/// <summary>
/// Background service that monitors active price alerts against real-time Redis ticks.
/// Algorithm:
///   - On start: load all active alerts into memory (ConcurrentDictionary keyed by symbol)
///   - On each Redis tick: check alerts for that symbol, fire &amp; deactivate if triggered
///   - Every 5 minutes: reload from DB to pick up newly created alerts
/// Alert cap: max 100 active alerts per user (enforced at creation via DB unique index).
/// </summary>
public class AlertEngineService : BackgroundService
{
    // symbol → list of in-memory alert snapshots
    private readonly ConcurrentDictionary<string, List<AlertCache>> _alertsBySymbol = new();

    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<AlertEngineService> _logger;
    private readonly int _reloadIntervalMinutes;

    public AlertEngineService(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        IEmailService emailService,
        ILogger<AlertEngineService> logger,
        IConfiguration config)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _emailService = emailService;
        _logger = logger;
        _reloadIntervalMinutes = config.GetValue<int>("AlertEngine:ReloadIntervalMinutes", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LoadAlertsFromDbAsync(stoppingToken);

        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(RedisChannel.Pattern("ticks:*"), OnTickReceived);

        _logger.LogInformation("AlertEngineService started.");

        // Periodic reload — interval controlled by AlertEngine:ReloadIntervalMinutes config
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_reloadIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await LoadAlertsFromDbAsync(stoppingToken);
        }
    }

    private void OnTickReceived(RedisChannel channel, RedisValue message)
    {
        var channelStr = channel.ToString();
        var symbol = channelStr.Length > 6 ? channelStr[6..].ToUpper() : null;

        if (symbol is null || message.IsNullOrEmpty) return;
        if (!_alertsBySymbol.TryGetValue(symbol, out var alerts) || alerts.Count == 0) return;

        TickDto? tick;
        try
        {
            tick = JsonSerializer.Deserialize<TickDto>(message.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return; }

        if (tick is null) return;

        // Scan and remove inside a single lock to prevent two concurrent callbacks
        // from both capturing the same alert into their local triggered list (C1 fix).
        List<AlertCache> triggered;
        lock (alerts)
        {
            triggered = alerts
                .Where(a => a.Direction == AlertDirection.ABOVE
                    ? tick.Price >= a.Threshold
                    : tick.Price <= a.Threshold)
                .ToList();

            if (triggered.Count > 0)
                triggered.ForEach(a => alerts.Remove(a));
        }

        if (triggered.Count == 0) return;

        // Fire-and-forget: persist + send email
        _ = Task.Run(() => FireAlertsAsync(triggered, tick.Price));
    }

    private async Task FireAlertsAsync(List<AlertCache> triggered, decimal currentPrice)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var alert in triggered)
        {
            try
            {
                var entity = await db.PriceAlerts.FindAsync(alert.Id);
                if (entity is null || !entity.IsActive) continue;

                entity.IsActive = false;
                entity.TriggeredAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Load user email
                var user = await db.Users.FindAsync(entity.UserId);
                if (user?.Email is not null)
                {
                    await _emailService.SendAlertEmailAsync(
                        user.Email, alert.Symbol,
                        alert.Direction.ToString(), alert.Threshold, currentPrice);
                }

                _logger.LogInformation(
                    "Alert {Id} fired: {Symbol} {Dir} {Threshold} @ {Price}",
                    alert.Id, alert.Symbol, alert.Direction, alert.Threshold, currentPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firing alert {Id}.", alert.Id);
            }
        }
    }

    private async Task LoadAlertsFromDbAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var alerts = await db.PriceAlerts
                .Where(a => a.IsActive)
                .Select(a => new AlertCache(a.Id, a.UserId, a.Symbol, a.Direction, a.Threshold))
                .ToListAsync(ct);

            // Rebuild index
            _alertsBySymbol.Clear();
            foreach (var alert in alerts)
            {
                _alertsBySymbol.AddOrUpdate(
                    alert.Symbol,
                    _ => [alert],
                    (_, list) => { lock (list) { list.Add(alert); } return list; });
            }

            _logger.LogInformation("AlertEngine loaded {Count} active alerts.", alerts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load alerts from DB.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.UnsubscribeAsync(RedisChannel.Pattern("ticks:*"));
        _logger.LogInformation("AlertEngineService stopped.");
        await base.StopAsync(cancellationToken);
    }

    // Immutable snapshot — safe to hold across async boundaries without EF tracking
    private record AlertCache(
        Guid Id, Guid UserId, string Symbol,
        AlertDirection Direction, decimal Threshold);
}
