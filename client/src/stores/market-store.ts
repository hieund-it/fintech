import { create } from 'zustand';
import type { TickData } from '../types/market';

interface MarketState {
  /** Map from symbol → latest tick. Using Map for O(1) per-symbol updates. */
  ticks: Map<string, TickData>;
  updateTick: (tick: TickData) => void;
  clearTicks: () => void;
}

export const useMarketStore = create<MarketState>()((set) => ({
  ticks: new Map(),

  updateTick: (tick) =>
    set((state) => {
      // Clone the Map so React detects the change
      const next = new Map(state.ticks);
      next.set(tick.symbol, tick);
      return { ticks: next };
    }),

  clearTicks: () => set({ ticks: new Map() }),
}));
