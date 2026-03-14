using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VnStock.Application.Alerts.Services;
using VnStock.Application.Auth.Services;
using VnStock.Application.Market.Services;
using VnStock.Application.Portfolio.Services;
using VnStock.Application.Watchlist.Services;
using VnStock.Domain.Entities;
using VnStock.Domain.Interfaces;
using VnStock.Infrastructure.Data;
using VnStock.Infrastructure.Email;

namespace VnStock.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IMarketDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IWatchlistDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IPortfolioDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAlertDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddSingleton<IEmailService, SmtpEmailService>();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var redisConn = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);

        // Singleton multiplexer shared by RedisMarketDataSubscriber and SignalR backplane
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
            _ => StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn));

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
