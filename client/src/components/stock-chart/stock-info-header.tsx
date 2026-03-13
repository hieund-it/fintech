import { useMarketStore } from '../../stores/market-store';
import type { StockInfo } from '../../types/market';

interface StockInfoHeaderProps {
  stock: StockInfo;
}

const fmtPrice = (v: number) => v.toLocaleString('vi-VN');
const fmtPct = (v: number) => `${v >= 0 ? '+' : ''}${v.toFixed(2)}%`;
const fmtVol = (v: number) =>
  v >= 1_000_000 ? `${(v / 1_000_000).toFixed(1)}M` : v.toLocaleString();

export function StockInfoHeader({ stock }: StockInfoHeaderProps) {
  const tick = useMarketStore((s) => s.ticks.get(stock.symbol));

  const changeColor =
    tick && tick.changePct > 0
      ? 'text-green-400'
      : tick && tick.changePct < 0
        ? 'text-red-400'
        : 'text-slate-400';

  return (
    <div className="flex flex-wrap items-baseline gap-4 px-1 py-2">
      {/* Symbol + name */}
      <div>
        <span className="text-2xl font-bold text-white">{stock.symbol}</span>
        <span className="ml-2 text-sm text-slate-400">{stock.name}</span>
      </div>

      {/* Live price */}
      {tick && (
        <>
          <span className="text-2xl font-semibold tabular-nums text-white">
            {fmtPrice(tick.price)}
          </span>
          <span className={`text-lg tabular-nums font-medium ${changeColor}`}>
            {fmtPct(tick.changePct)}
          </span>
          <span className="text-sm text-slate-500">
            Vol: {fmtVol(tick.volume)}
          </span>
        </>
      )}

      {/* Exchange + sector badges */}
      <div className="ml-auto flex gap-2 text-xs">
        <span className="px-2 py-0.5 rounded bg-slate-700 text-slate-300">
          {stock.exchange}
        </span>
        {stock.sector && (
          <span className="px-2 py-0.5 rounded bg-slate-700 text-slate-400">
            {stock.sector}
          </span>
        )}
      </div>
    </div>
  );
}
