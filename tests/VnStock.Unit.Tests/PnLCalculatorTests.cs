using VnStock.Application.Portfolio.Services;
using VnStock.Domain.Entities;

namespace VnStock.Unit.Tests;

public class PnLCalculatorTests
{
    private static Transaction Buy(decimal qty, decimal price, decimal fee = 0, int daysOffset = 0) =>
        new() { Type = TransactionType.BUY, Quantity = qty, Price = price, Fee = fee, TransactedAt = DateTime.UtcNow.AddDays(daysOffset) };

    private static Transaction Sell(decimal qty, decimal price, decimal fee = 0, int daysOffset = 1) =>
        new() { Type = TransactionType.SELL, Quantity = qty, Price = price, Fee = fee, TransactedAt = DateTime.UtcNow.AddDays(daysOffset) };

    [Fact]
    public void BuyOnly_ReturnsCorrectUnrealizedPnL()
    {
        var txns = new[] { Buy(100, 50_000m) };
        var result = PnLCalculator.Calculate("VNM", txns, 55_000m);

        Assert.Equal(100m, result.Quantity);
        Assert.Equal(50_000m, result.AvgCost);
        Assert.Equal(0m, result.RealizedPnL);
        Assert.Equal(500_000m, result.UnrealizedPnL); // (55000-50000)*100
    }

    [Fact]
    public void SellAll_ReturnsCorrectRealizedPnL_ZeroUnrealized()
    {
        var txns = new[] { Buy(100, 50_000m, daysOffset: 0), Sell(100, 60_000m, daysOffset: 1) };
        var result = PnLCalculator.Calculate("VNM", txns, 62_000m);

        Assert.Equal(0m, result.Quantity);
        Assert.Equal(1_000_000m, result.RealizedPnL); // (60000-50000)*100
        Assert.Equal(0m, result.UnrealizedPnL);
    }

    [Fact]
    public void WeightedAverageCost_MultipleBuys()
    {
        var txns = new[]
        {
            Buy(100, 40_000m, daysOffset: 0),
            Buy(100, 60_000m, daysOffset: 1),
        };
        // avg = (100*40000 + 100*60000) / 200 = 50000
        var result = PnLCalculator.Calculate("VNM", txns, 50_000m);

        Assert.Equal(200m, result.Quantity);
        Assert.Equal(50_000m, result.AvgCost);
        Assert.Equal(0m, result.UnrealizedPnL);
    }

    [Fact]
    public void PartialSell_SplitsRealizedAndUnrealized()
    {
        var txns = new[]
        {
            Buy(200, 50_000m, daysOffset: 0),
            Sell(100, 60_000m, daysOffset: 1),
        };
        var result = PnLCalculator.Calculate("VNM", txns, 65_000m);

        Assert.Equal(100m, result.Quantity);
        Assert.Equal(50_000m, result.AvgCost);
        Assert.Equal(1_000_000m, result.RealizedPnL);     // (60000-50000)*100
        Assert.Equal(1_500_000m, result.UnrealizedPnL);   // (65000-50000)*100
    }

    [Fact]
    public void FeesReduceRealizedPnL()
    {
        var txns = new[]
        {
            Buy(100, 50_000m, fee: 100_000m, daysOffset: 0),
            Sell(100, 60_000m, fee: 50_000m, daysOffset: 1),
        };
        var result = PnLCalculator.Calculate("VNM", txns, 60_000m);

        // avgCost = (100*50000 + 100000) / 100 = 51000
        // realized = (60000 - 51000)*100 - 50000 = 900000 - 50000 = 850000
        Assert.Equal(850_000m, result.RealizedPnL);
        Assert.Equal(0m, result.UnrealizedPnL);
    }

    [Fact]
    public void EmptyTransactions_ReturnsZeroPosition()
    {
        var result = PnLCalculator.Calculate("VNM", Array.Empty<Transaction>(), 50_000m);

        Assert.Equal(0m, result.Quantity);
        Assert.Equal(0m, result.AvgCost);
        Assert.Equal(0m, result.RealizedPnL);
        Assert.Equal(0m, result.UnrealizedPnL);
    }

    [Fact]
    public void SellWithoutBuy_IsSkipped()
    {
        var txns = new[] { Sell(100, 60_000m) };
        var result = PnLCalculator.Calculate("VNM", txns, 60_000m);

        Assert.Equal(0m, result.Quantity);
        Assert.Equal(0m, result.RealizedPnL);
    }
}
