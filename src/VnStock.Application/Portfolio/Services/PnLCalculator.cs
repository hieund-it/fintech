using VnStock.Application.Portfolio.DTOs;
using VnStock.Domain.Entities;

namespace VnStock.Application.Portfolio.Services;

/// <summary>
/// Stateless P&amp;L engine using weighted-average cost basis.
/// Processes transactions chronologically; BUYs update avg cost, SELLs realize P&amp;L.
/// </summary>
public static class PnLCalculator
{
    public static PositionDto Calculate(string symbol, IEnumerable<Transaction> txns, decimal currentPrice)
    {
        decimal remainingQty = 0;
        decimal totalCost = 0;      // cost basis of remaining shares (qty × avgCost)
        decimal realizedPnL = 0;

        foreach (var t in txns.OrderBy(t => t.TransactedAt))
        {
            if (t.Type == TransactionType.BUY)
            {
                // Weighted-average recalculation
                totalCost += t.Quantity * t.Price + t.Fee;
                remainingQty += t.Quantity;
            }
            else // SELL
            {
                if (remainingQty <= 0) continue;

                var avgCost = totalCost / remainingQty;
                var sellQty = Math.Min(t.Quantity, remainingQty);

                realizedPnL += (t.Price - avgCost) * sellQty - t.Fee;
                totalCost -= avgCost * sellQty;
                remainingQty -= sellQty;
            }
        }

        var avgBuyPrice = remainingQty > 0 ? totalCost / remainingQty : 0;
        var unrealizedPnL = remainingQty > 0 ? (currentPrice - avgBuyPrice) * remainingQty : 0;

        return new PositionDto(symbol, remainingQty, avgBuyPrice, realizedPnL, unrealizedPnL, currentPrice);
    }
}
