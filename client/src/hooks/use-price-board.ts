import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchStocks } from '../services/market-api';
import { ensureConnected, getConnection } from '../services/signalr-connection';
import { useMarketStore } from '../stores/market-store';
import type { StockInfo, TickData } from '../types/market';

const BATCH_SIZE = 50;

interface UsePriceBoardOptions {
  exchange?: string;
  q?: string;
  sector?: string;
}

export function usePriceBoard(options: UsePriceBoardOptions = {}) {
  const { exchange, q, sector } = options;
  const updateTick = useMarketStore((s) => s.updateTick);

  const stocksQuery = useQuery<StockInfo[]>({
    queryKey: ['stocks', exchange, q, sector],
    queryFn: () => fetchStocks({ exchange, q, sector }),
    staleTime: 5 * 60_000, // 5 min — symbol list rarely changes
  });

  const stocks = stocksQuery.data ?? [];

  useEffect(() => {
    if (stocks.length === 0) return;

    let active = true;

    // Store handler ref so cleanup only removes this hook's listener
    const handler = (tick: TickData) => {
      if (active) updateTick(tick);
    };

    async function setup() {
      await ensureConnected();
      if (!active) return;

      const conn = getConnection();

      // Register handler BEFORE invoking subscriptions so no ticks are missed
      conn.on('ReceiveTick', handler);

      // Subscribe in batches of 50 to avoid overloading the hub
      for (let i = 0; i < stocks.length; i += BATCH_SIZE) {
        const batch = stocks.slice(i, i + BATCH_SIZE);
        await Promise.all(
          batch.map((s) => conn.invoke('SubscribeSymbol', s.symbol)),
        );
      }
    }

    setup().catch(console.error);

    return () => {
      active = false;
      const conn = getConnection();
      conn.off('ReceiveTick', handler);
      stocks.forEach((s) => conn.invoke('UnsubscribeSymbol', s.symbol).catch(() => {}));
    };
  }, [stocks, updateTick]);

  return stocksQuery;
}
