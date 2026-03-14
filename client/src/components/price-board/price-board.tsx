import { useRef, useState } from 'react';
import { useVirtualizer } from '@tanstack/react-virtual';
import { usePriceBoard } from '../../hooks/use-price-board';
import { ExchangeFilter, type ExchangeValue } from './exchange-filter';
import { PriceRow } from './price-row';

const ROW_HEIGHT = 40;
const OVERSCAN = 10;

// Responsive columns: mobile shows Symbol+Price+Change, tablet adds Volume+Exchange, desktop adds Company+Sector
const COLUMNS = [
  { label: 'Symbol',   className: 'w-20' },
  { label: 'Company',  className: 'hidden md:block flex-1' },
  { label: 'Price',    className: 'w-24 text-right' },
  { label: 'Change%',  className: 'w-24 text-right' },
  { label: 'Volume',   className: 'hidden sm:block w-24 text-right' },
  { label: 'Sàn',      className: 'hidden sm:block w-24 text-center' },
  { label: 'Sector',   className: 'hidden lg:block w-24' },
];

export function PriceBoard() {
  const [exchange, setExchange] = useState<ExchangeValue>('ALL');
  const parentRef = useRef<HTMLDivElement>(null);

  const { data: stocks = [], isLoading, isError } = usePriceBoard({
    exchange: exchange === 'ALL' ? undefined : exchange,
  });

  const rowVirtualizer = useVirtualizer({
    count: stocks.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => ROW_HEIGHT,
    overscan: OVERSCAN,
  });

  return (
    <div className="flex flex-col h-full bg-slate-900 text-slate-100">
      {/* Toolbar */}
      <div className="flex items-center gap-4 px-4 py-3 border-b border-slate-700">
        <h2 className="text-base font-semibold">Bảng giá</h2>
        <ExchangeFilter value={exchange} onChange={setExchange} />
        <span className="ml-auto text-xs text-slate-500">
          {stocks.length} mã
        </span>
      </div>

      {/* Header row — grid matches PriceRow responsive layout */}
      <div className="flex items-center px-4 py-2 text-xs font-semibold text-slate-500 uppercase
                      border-b border-slate-800 bg-slate-900 sticky top-0 z-10 gap-2">
        {COLUMNS.map((c) => (
          <div key={c.label} className={c.className}>{c.label}</div>
        ))}
      </div>

      {/* Virtualized body */}
      {isLoading && (
        <div className="flex-1 flex items-center justify-center text-slate-400">
          Đang tải…
        </div>
      )}
      {isError && (
        <div className="flex-1 flex items-center justify-center text-red-400">
          Không tải được dữ liệu.
        </div>
      )}
      {!isLoading && !isError && (
        <div ref={parentRef} className="flex-1 overflow-auto">
          <div
            style={{ height: rowVirtualizer.getTotalSize() }}
            className="relative"
          >
            {rowVirtualizer.getVirtualItems().map((virtualRow) => {
              const stock = stocks[virtualRow.index];
              return (
                <PriceRow
                  key={stock.symbol}
                  stock={stock}
                  style={{
                    position: 'absolute',
                    top: 0,
                    transform: `translateY(${virtualRow.start}px)`,
                    width: '100%',
                    height: `${ROW_HEIGHT}px`,
                  }}
                />
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
