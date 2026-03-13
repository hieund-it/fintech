using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VnStock.Application.Market.Services;
using VnStock.Domain.Entities;
using VnStock.Infrastructure.Data;

namespace VnStock.Unit.Tests.Market;

/// <summary>
/// Tests for MarketDataService using an InMemory AppDbContext.
/// </summary>
public class MarketDataServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly MarketDataService _sut;

    public MarketDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new MarketDataService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedStocksAsync()
    {
        _db.Stocks.AddRange(
            new Stock { Symbol = "VCB", Name = "Vietcombank",  Exchange = "HOSE", Sector = "Banking" },
            new Stock { Symbol = "MBB", Name = "Military Bank", Exchange = "HOSE", Sector = "Banking" },
            new Stock { Symbol = "FPT", Name = "FPT Corp",      Exchange = "HOSE", Sector = "Technology" },
            new Stock { Symbol = "SHS", Name = "SHS Securities", Exchange = "HNX", Sector = "Financials" }
        );
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStocksAsync_NoFilter_ReturnsAll()
    {
        await SeedStocksAsync();
        var result = await _sut.GetStocksAsync();
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetStocksAsync_ExchangeFilter_ReturnsOnlyHNX()
    {
        await SeedStocksAsync();
        var result = await _sut.GetStocksAsync(exchange: "HNX");
        result.Should().ContainSingle(s => s.Symbol == "SHS");
    }

    [Fact]
    public async Task GetStocksAsync_QueryFilter_MatchesSymbolAndName()
    {
        await SeedStocksAsync();
        var bySymbol = await _sut.GetStocksAsync(q: "vcb");
        bySymbol.Should().ContainSingle(s => s.Symbol == "VCB");

        var byName = await _sut.GetStocksAsync(q: "Military");
        byName.Should().ContainSingle(s => s.Symbol == "MBB");
    }

    [Fact]
    public async Task GetStocksAsync_SectorFilter_ReturnsBankingOnly()
    {
        await SeedStocksAsync();
        var result = await _sut.GetStocksAsync(sector: "Banking");
        result.Should().HaveCount(2).And.OnlyContain(s => s.Sector == "Banking");
    }

    [Fact]
    public async Task GetStockAsync_ExistingSymbol_ReturnsDto()
    {
        await SeedStocksAsync();
        var result = await _sut.GetStockAsync("VCB");
        result.Should().NotBeNull();
        result!.Name.Should().Be("Vietcombank");
    }

    [Fact]
    public async Task GetStockAsync_UnknownSymbol_ReturnsNull()
    {
        await SeedStocksAsync();
        var result = await _sut.GetStockAsync("UNKNOWN");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOhlcvAsync_ValidRange_ReturnsOrderedBars()
    {
        await SeedStocksAsync();
        _db.OhlcvDaily.AddRange(
            new OhlcvDaily { Symbol = "VCB", Date = new DateOnly(2025, 1, 2), Open = 90, High = 92, Low = 89, Close = 91, Volume = 1_000_000 },
            new OhlcvDaily { Symbol = "VCB", Date = new DateOnly(2025, 1, 3), Open = 91, High = 93, Low = 90, Close = 92, Volume = 1_200_000 },
            new OhlcvDaily { Symbol = "FPT", Date = new DateOnly(2025, 1, 2), Open = 120, High = 125, Low = 119, Close = 124, Volume = 500_000 }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetOhlcvAsync(
            "VCB",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31));

        result.Should().HaveCount(2);
        result.First().Date.Should().Be(new DateOnly(2025, 1, 2));
        result.Last().Date.Should().Be(new DateOnly(2025, 1, 3));
    }

    [Fact]
    public async Task GetOhlcvAsync_UnknownSymbol_ReturnsEmpty()
    {
        await SeedStocksAsync();
        var result = await _sut.GetOhlcvAsync(
            "UNKNOWN",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31));
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSectorsAsync_ReturnsDistinctSectors()
    {
        await SeedStocksAsync();
        var sectors = await _sut.GetSectorsAsync();
        sectors.Should().Contain(["Banking", "Technology", "Financials"])
               .And.HaveCount(3); // Banking appears twice but distinct
    }
}
