using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VnStock.Application.Auth.Services;
using VnStock.Domain.Entities;

namespace VnStock.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IAuthDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WatchlistItem> Watchlists => Set<WatchlistItem>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(r => r.TokenHash).IsUnique();
            e.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // WatchlistItem: unique per user+symbol
        builder.Entity<WatchlistItem>()
            .HasIndex(w => new { w.UserId, w.Symbol })
            .IsUnique();

        // Transaction: financial decimal precision
        builder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Quantity).HasPrecision(18, 4);
            e.Property(t => t.Price).HasPrecision(12, 2);
            e.Property(t => t.Fee).HasPrecision(12, 2);
            // Store enum as string for readability
            e.Property(t => t.Type).HasConversion<string>();
            e.HasIndex(t => t.PortfolioId);
            e.HasIndex(t => t.Symbol);
        });

        // PriceAlert: decimal precision + enum as string + partial index for active alerts
        builder.Entity<PriceAlert>(e =>
        {
            e.Property(a => a.Direction).HasConversion<string>();
            e.Property(a => a.Threshold).HasPrecision(12, 2);
            // Partial index: alert engine queries only active alerts per tick
            e.HasIndex(a => new { a.Symbol, a.IsActive })
             .HasFilter("\"IsActive\" = true");
        });
    }
}
