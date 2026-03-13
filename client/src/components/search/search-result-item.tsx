import type { StockInfo } from '../../types/market';

interface SearchResultItemProps {
  stock: StockInfo;
}

export function SearchResultItem({ stock }: SearchResultItemProps) {
  return (
    <div className="flex items-center justify-between px-3 py-2">
      <div className="flex items-center gap-3">
        <span className="font-semibold text-sm text-white w-14">{stock.symbol}</span>
        <span className="text-sm text-slate-400 truncate max-w-48">{stock.name}</span>
      </div>
      <span className="text-xs px-1.5 py-0.5 rounded bg-slate-700 text-slate-400 ml-2 shrink-0">
        {stock.exchange}
      </span>
    </div>
  );
}
