import { useEffect, useRef, useState } from 'react';

interface PriceCellProps {
  value: number | undefined;
  formatter?: (v: number) => string;
  colorize?: boolean; // apply green/red based on sign
}

type FlashDir = 'up' | 'down' | null;

/**
 * Table cell that flashes green/red for 500 ms when the value changes.
 */
export function PriceCell({ value, formatter, colorize = false }: PriceCellProps) {
  const prevRef = useRef<number | undefined>(undefined);
  const [flash, setFlash] = useState<FlashDir>(null);

  useEffect(() => {
    if (value === undefined || prevRef.current === undefined) {
      prevRef.current = value;
      return;
    }
    if (value !== prevRef.current) {
      setFlash(value > prevRef.current ? 'up' : 'down');
      const id = setTimeout(() => setFlash(null), 500);
      prevRef.current = value;
      return () => clearTimeout(id);
    }
    prevRef.current = value;
  }, [value]);

  const display = value === undefined
    ? '—'
    : (formatter ? formatter(value) : value.toLocaleString());

  // Base text color when colorize is true (for change%/change columns)
  const signColor =
    colorize && value !== undefined
      ? value > 0
        ? 'text-green-400'
        : value < 0
          ? 'text-red-400'
          : 'text-slate-400'
      : '';

  // Flash background overlay via Tailwind arbitrary CSS
  const flashClass =
    flash === 'up'
      ? 'animate-flash-up'
      : flash === 'down'
        ? 'animate-flash-down'
        : '';

  return (
    <span className={`tabular-nums transition-colors ${signColor} ${flashClass}`}>
      {display}
    </span>
  );
}
