import { memo } from 'react';
import { Link } from 'react-router-dom';
import { useMarketStore } from '../../stores/market-store';
import { PriceCell } from './price-cell';
import type { StockInfo } from '../../types/market';

interface PriceRowProps {
  stock: StockInfo;
  style?: React.CSSProperties; // injected by TanStack Virtual
}

const fmtPrice = (v: number) => v.toLocaleString('vi-VN');
const fmtVol = (v: number) =>
  v >= 1_000_000
    ? `${(v / 1_000_000).toFixed(1)}M`
    : v >= 1_000
      ? `${(v / 1_000).toFixed(0)}K`
      : v.toString();
const fmtPct = (v: number) => `${v >= 0 ? '+' : ''}${v.toFixed(2)}%`;

/**
 * Single price-board row. Memoized — only re-renders when its own tick changes.
 */
export const PriceRow = memo(function PriceRow({ stock, style }: PriceRowProps) {
  // Selector per symbol → prevents global re-render when other symbols update
  const tick = useMarketStore((s) => s.ticks.get(stock.symbol));

  return (
    <div
      style={style}
      className="grid grid-cols-[80px_1fr_100px_90px_90px_90px_90px] items-center
                 px-4 text-sm border-b border-slate-800 hover:bg-slate-800/50"
    >
      {/* Symbol — links to detail page */}
      <Link
        to={`/stocks/${stock.symbol}`}
        className="font-semibold text-blue-400 hover:underline"
      >
        {stock.symbol}
      </Link>

      {/* Company name */}
      <span className="text-slate-300 truncate pr-4">{stock.name}</span>

      {/* Price */}
      <div className="text-right">
        <PriceCell value={tick?.price} formatter={fmtPrice} />
      </div>

      {/* Change % */}
      <div className="text-right">
        <PriceCell value={tick?.changePct} formatter={fmtPct} colorize />
      </div>

      {/* Volume */}
      <div className="text-right text-slate-400">
        {tick ? fmtVol(tick.volume) : '—'}
      </div>

      {/* Exchange label */}
      <div className="text-center">
        <span className="text-xs px-1.5 py-0.5 rounded bg-slate-700 text-slate-300">
          {stock.exchange}
        </span>
      </div>

      {/* Sector */}
      <div className="text-slate-500 text-xs truncate">{stock.sector ?? '—'}</div>
    </div>
  );
});
