import type { Timeframe } from '../../types/market';

const TIMEFRAMES: Timeframe[] = ['1W', '1M', '3M', '1Y'];

interface ChartToolbarProps {
  value: Timeframe;
  onChange: (tf: Timeframe) => void;
}

export function ChartToolbar({ value, onChange }: ChartToolbarProps) {
  return (
    <div className="flex gap-1">
      {TIMEFRAMES.map((tf) => (
        <button
          key={tf}
          onClick={() => onChange(tf)}
          className={`px-3 py-1 rounded text-sm font-medium transition-colors
            ${value === tf
              ? 'bg-blue-600 text-white'
              : 'bg-slate-800 text-slate-400 hover:bg-slate-700 hover:text-slate-200'
            }`}
        >
          {tf}
        </button>
      ))}
    </div>
  );
}
