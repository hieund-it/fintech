import { useEffect, useRef } from 'react';
import {
  createChart,
  CandlestickSeries,
  HistogramSeries,
  LineSeries,
  type IChartApi,
  type ISeriesApi,
  type CandlestickSeriesOptions,
  type CandlestickStyleOptions,
  type CandlestickData,
  type WhitespaceData,
  type DeepPartial,
  type Time,
} from 'lightweight-charts';
import type { OhlcvBar } from '../../types/market';

/** Full v5 ISeriesApi type for a candlestick series. */
export type CandleSeriesRef = ISeriesApi<
  'Candlestick',
  Time,
  CandlestickData<Time> | WhitespaceData<Time>,
  CandlestickSeriesOptions,
  DeepPartial<CandlestickStyleOptions>
>;

// Internal refs for series instances shared between the two effects
type SeriesRefs = {
  candle: CandleSeriesRef | null;
  volume: ISeriesApi<'Histogram', Time> | null;
  ma20: ISeriesApi<'Line', Time> | null;
  ma50: ISeriesApi<'Line', Time> | null;
};

interface StockChartProps {
  data: OhlcvBar[];
  /** Forwarded ref so the parent hook can call series.update() on new ticks */
  seriesRef: React.MutableRefObject<CandleSeriesRef | null>;
}

/** Calculate simple moving average over `period` bars. */
function calculateMA(bars: OhlcvBar[], period: number) {
  return bars.slice(period - 1).map((_, i) => ({
    time: bars[i + period - 1].date as Time,
    value:
      bars.slice(i, i + period).reduce((sum, b) => sum + b.close, 0) / period,
  }));
}

export function StockChart({ data, seriesRef }: StockChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const internalSeriesRef = useRef<SeriesRefs>({
    candle: null,
    volume: null,
    ma20: null,
    ma50: null,
  });

  // Effect 1: create/destroy chart — runs once on mount, never on data changes
  useEffect(() => {
    if (!containerRef.current) return;

    const chart = createChart(containerRef.current, {
      width: containerRef.current.clientWidth,
      height: 420,
      layout: {
        background: { color: '#0f172a' },
        textColor: '#94a3b8',
      },
      grid: {
        vertLines: { color: '#1e293b' },
        horzLines: { color: '#1e293b' },
      },
      crosshair: { mode: 1 },
      rightPriceScale: { borderColor: '#334155' },
      timeScale: { borderColor: '#334155', timeVisible: true },
    });

    chartRef.current = chart;

    // v5 API: addSeries(SeriesType, options)
    const candleSeries = chart.addSeries(CandlestickSeries, {
      upColor: '#22c55e',
      downColor: '#ef4444',
      borderUpColor: '#22c55e',
      borderDownColor: '#ef4444',
      wickUpColor: '#22c55e',
      wickDownColor: '#ef4444',
    });
    seriesRef.current = candleSeries;
    internalSeriesRef.current.candle = candleSeries;

    const volumeSeries = chart.addSeries(HistogramSeries, {
      priceScaleId: 'volume',
      color: '#334155',
    });
    chart.priceScale('volume').applyOptions({
      scaleMargins: { top: 0.8, bottom: 0 },
    });
    internalSeriesRef.current.volume = volumeSeries;

    internalSeriesRef.current.ma20 = chart.addSeries(LineSeries, {
      color: '#f59e0b',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
    });
    internalSeriesRef.current.ma50 = chart.addSeries(LineSeries, {
      color: '#818cf8',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
    });

    // Responsive resize
    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (entry) chart.resize(entry.contentRect.width, 420);
    });
    observer.observe(containerRef.current!);

    return () => {
      observer.disconnect();
      seriesRef.current = null;
      internalSeriesRef.current = { candle: null, volume: null, ma20: null, ma50: null };
      chart.remove();
      chartRef.current = null;
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [seriesRef]);

  // Effect 2: update series data when `data` prop changes — no chart recreation
  useEffect(() => {
    const { candle, volume, ma20, ma50 } = internalSeriesRef.current;
    if (!candle || !volume || !ma20 || !ma50 || data.length === 0) return;

    candle.setData(
      data.map((b) => ({
        time: b.date as Time,
        open: b.open,
        high: b.high,
        low: b.low,
        close: b.close,
      })),
    );
    volume.setData(
      data.map((b) => ({
        time: b.date as Time,
        value: b.volume,
        color: b.close >= b.open ? '#22c55e33' : '#ef444433',
      })),
    );
    if (data.length >= 20) ma20.setData(calculateMA(data, 20));
    if (data.length >= 50) ma50.setData(calculateMA(data, 50));
  }, [data]);

  return <div ref={containerRef} className="w-full" />;
}
