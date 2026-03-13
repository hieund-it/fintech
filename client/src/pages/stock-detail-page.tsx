import { lazy, Suspense, useRef, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import type { CandleSeriesRef } from '../components/stock-chart/stock-chart';
import { ChartToolbar } from '../components/stock-chart/chart-toolbar';
import { StockInfoHeader } from '../components/stock-chart/stock-info-header';
import { useStockOhlcv } from '../hooks/use-stock-ohlcv';
import { fetchStock } from '../services/market-api';
import type { Timeframe } from '../types/market';

// Lazy-load chart (~700 KB bundle) to keep initial page load fast
const StockChart = lazy(() =>
  import('../components/stock-chart/stock-chart').then((m) => ({
    default: m.StockChart,
  })),
);

export function StockDetailPage() {
  const { symbol } = useParams<{ symbol: string }>();
  const [timeframe, setTimeframe] = useState<Timeframe>('3M');
  const seriesRef = useRef<CandleSeriesRef | null>(null);

  const stockQuery = useQuery({
    queryKey: ['stock', symbol],
    queryFn: () => fetchStock(symbol!),
    enabled: !!symbol,
  });

  const ohlcvQuery = useStockOhlcv(symbol ?? '', timeframe, { seriesRef });

  const stock = stockQuery.data;
  const bars = ohlcvQuery.data ?? [];
  const isLoading = stockQuery.isLoading || ohlcvQuery.isLoading;

  if (!symbol) return null;

  return (
    <div className="flex flex-col min-h-screen bg-slate-900 text-slate-100 p-4">
      {/* Breadcrumb */}
      <nav className="text-sm text-slate-500 mb-4">
        <Link to="/market" className="hover:text-blue-400">Bảng giá</Link>
        <span className="mx-2">/</span>
        <span className="text-slate-300">{symbol}</span>
      </nav>

      {/* Stock header */}
      {stock && <StockInfoHeader stock={stock} />}

      {/* Chart controls */}
      <div className="flex items-center gap-4 my-3">
        <ChartToolbar value={timeframe} onChange={setTimeframe} />
        <span className="text-xs text-slate-500 ml-auto">
          MA20 <span className="text-amber-400">●</span>&nbsp;
          MA50 <span className="text-indigo-400">●</span>
        </span>
      </div>

      {/* Chart */}
      <div className="rounded-lg overflow-hidden border border-slate-800 bg-slate-950">
        {isLoading ? (
          <div className="flex items-center justify-center h-[420px] text-slate-400">
            Đang tải…
          </div>
        ) : (
          <Suspense
            fallback={
              <div className="flex items-center justify-center h-[420px] text-slate-400">
                Đang tải biểu đồ…
              </div>
            }
          >
            <StockChart data={bars} seriesRef={seriesRef} />
          </Suspense>
        )}
      </div>
    </div>
  );
}
