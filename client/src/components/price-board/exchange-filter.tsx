const EXCHANGES = ['ALL', 'HOSE', 'HNX', 'UPCOM'] as const;
export type ExchangeValue = (typeof EXCHANGES)[number];

interface ExchangeFilterProps {
  value: ExchangeValue;
  onChange: (exchange: ExchangeValue) => void;
}

export function ExchangeFilter({ value, onChange }: ExchangeFilterProps) {
  return (
    <div className="flex gap-1">
      {EXCHANGES.map((ex) => (
        <button
          key={ex}
          onClick={() => onChange(ex)}
          className={`px-3 py-1 rounded text-sm font-medium transition-colors
            ${value === ex
              ? 'bg-blue-600 text-white'
              : 'bg-slate-800 text-slate-400 hover:bg-slate-700 hover:text-slate-200'
            }`}
        >
          {ex}
        </button>
      ))}
    </div>
  );
}
