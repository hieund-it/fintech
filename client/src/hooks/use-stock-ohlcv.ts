import { useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { Time } from 'lightweight-charts';
import type { CandleSeriesRef } from '../components/stock-chart/stock-chart';
import { fetchOhlcv } from '../services/market-api';
import { ensureConnected, getConnection } from '../services/signalr-connection';
import type { OhlcvBar, TickData, Timeframe } from '../types/market';

interface UseStockOhlcvOptions {
  /** Ref to the candlestick series — used to append real-time ticks */
  seriesRef: React.MutableRefObject<CandleSeriesRef | null>;
}

export function useStockOhlcv(
  symbol: string,
  timeframe: Timeframe,
  { seriesRef }: UseStockOhlcvOptions,
) {
  const query = useQuery<OhlcvBar[]>({
    queryKey: ['ohlcv', symbol, timeframe],
    queryFn: () => fetchOhlcv(symbol, timeframe),
    staleTime: 60_000,
  });

  // Track the last bar's OHLCV so we can update it in real-time
  const lastBarRef = useRef<OhlcvBar | null>(null);
  useEffect(() => {
    const bars = query.data;
    if (bars && bars.length > 0) {
      lastBarRef.current = bars[bars.length - 1];
    }
  }, [query.data]);

  useEffect(() => {
    let active = true;

    // Store handler ref so cleanup only removes this hook's listener
    const handler = (tick: TickData) => {
      if (!active || tick.symbol !== symbol) return;
      const series = seriesRef.current;
      if (!series) return;

      const today = new Date().toISOString().slice(0, 10);

      if (lastBarRef.current && lastBarRef.current.date === today) {
        // Update today's candle
        const prev = lastBarRef.current;
        const updated: OhlcvBar = {
          ...prev,
          close: tick.price,
          high: Math.max(prev.high, tick.price),
          low: Math.min(prev.low, tick.price),
          volume: tick.volume,
        };
        lastBarRef.current = updated;
        series.update({
          time: today as unknown as Time,
          open: updated.open,
          high: updated.high,
          low: updated.low,
          close: updated.close,
        });
      } else {
        // New day — append new candle
        const newBar: OhlcvBar = {
          symbol,
          date: today,
          open: tick.price,
          high: tick.price,
          low: tick.price,
          close: tick.price,
          volume: tick.volume,
        };
        lastBarRef.current = newBar;
        series.update({
          time: today as unknown as Time,
          open: newBar.open,
          high: newBar.high,
          low: newBar.low,
          close: newBar.close,
        });
      }
    };

    async function setup() {
      await ensureConnected();
      if (!active) return;

      const conn = getConnection();
      await conn.invoke('SubscribeSymbol', symbol);
      conn.on('ReceiveTick', handler);
    }

    setup().catch(console.error);

    return () => {
      active = false;
      const conn = getConnection();
      conn.off('ReceiveTick', handler);
      conn.invoke('UnsubscribeSymbol', symbol).catch(() => {});
    };
  }, [symbol, seriesRef]);

  return query;
}
