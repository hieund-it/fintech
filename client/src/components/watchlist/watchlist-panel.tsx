import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  fetchWatchlist,
  addToWatchlist,
  removeFromWatchlist,
  type WatchlistItem,
} from '../../services/watchlist-api';
import { ensureConnected, getConnection } from '../../services/signalr-connection';
import { useMarketStore } from '../../stores/market-store';
import type { TickData } from '../../types/market';

/** Watchlist panel with add/remove and real-time price ticks via SignalR. */
export function WatchlistPanel() {
  const qc = useQueryClient();
  const [input, setInput] = useState('');
  const updateTick = useMarketStore((s) => s.updateTick);
  const ticks = useMarketStore((s) => s.ticks); // Map<symbol, TickData>

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['watchlist'],
    queryFn: fetchWatchlist,
  });

  const addMutation = useMutation({
    mutationFn: (symbol: string) => addToWatchlist(symbol),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['watchlist'] });
      setInput('');
    },
  });

  const removeMutation = useMutation({
    mutationFn: (id: string) => removeFromWatchlist(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['watchlist'] }),
  });

  // Subscribe to SignalR ticks for all watched symbols
  useEffect(() => {
    if (items.length === 0) return;
    let active = true;

    const handler = (tick: TickData) => { if (active) updateTick(tick); };

    async function setup() {
      await ensureConnected();
      if (!active) return;
      const conn = getConnection();
      await Promise.all(items.map((i) => conn.invoke('SubscribeSymbol', i.symbol)));
      conn.on('ReceiveTick', handler);
    }

    setup().catch(console.error);

    return () => {
      active = false;
      const conn = getConnection();
      conn.off('ReceiveTick', handler);
      items.forEach((i) => conn.invoke('UnsubscribeSymbol', i.symbol).catch(() => {}));
    };
  }, [items, updateTick]);

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault();
    const symbol = input.trim().toUpperCase();
    if (symbol) addMutation.mutate(symbol);
  };

  if (isLoading) return <div className="p-4 text-sm text-slate-400">Đang tải watchlist…</div>;

  return (
    <div className="bg-slate-900 rounded-lg border border-slate-700 overflow-hidden">
      <div className="px-4 py-3 border-b border-slate-700 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white">Watchlist</h3>
        <span className="text-xs text-slate-500">{items.length} mã</span>
      </div>

      {/* Add symbol form */}
      <form onSubmit={handleAdd} className="px-4 py-3 border-b border-slate-700 flex gap-2">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value.toUpperCase())}
          placeholder="Thêm mã (VD: VCB)"
          maxLength={10}
          className="flex-1 bg-slate-800 text-white text-sm px-3 py-1.5 rounded border border-slate-600
                     focus:outline-none focus:border-blue-500 placeholder-slate-500"
        />
        <button
          type="submit"
          disabled={!input.trim() || addMutation.isPending}
          className="px-3 py-1.5 text-sm bg-blue-600 hover:bg-blue-500 disabled:opacity-50
                     text-white rounded transition-colors"
        >
          Thêm
        </button>
      </form>

      {/* Symbol list */}
      {items.length === 0 ? (
        <p className="px-4 py-6 text-center text-slate-500 text-sm">
          Chưa có mã nào. Thêm mã để theo dõi giá thực tế.
        </p>
      ) : (
        <ul>
          {items.map((item) => (
            <WatchlistRow
              key={item.id}
              item={item}
              tick={ticks.get(item.symbol)}
              onRemove={() => removeMutation.mutate(item.id)}
            />
          ))}
        </ul>
      )}
    </div>
  );
}

interface WatchlistRowProps {
  item: WatchlistItem;
  tick: TickData | undefined;
  onRemove: () => void;
}

function WatchlistRow({ item, tick, onRemove }: WatchlistRowProps) {
  const isUp = (tick?.changePct ?? 0) >= 0;
  return (
    <li className="flex items-center justify-between px-4 py-2.5 border-b border-slate-800
                   hover:bg-slate-800/50 group">
      <span className="text-sm font-medium text-white">{item.symbol}</span>
      <div className="flex items-center gap-4">
        {tick ? (
          <>
            <span className="text-sm font-mono text-white">{tick.price.toLocaleString('vi-VN')}</span>
            <span className={`text-xs font-medium ${isUp ? 'text-green-400' : 'text-red-400'}`}>
              {isUp ? '+' : ''}{tick.changePct.toFixed(2)}%
            </span>
          </>
        ) : (
          <span className="text-xs text-slate-500">—</span>
        )}
        <button
          onClick={onRemove}
          className="text-slate-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity text-xs"
          title="Xóa khỏi watchlist"
        >
          ✕
        </button>
      </div>
    </li>
  );
}
