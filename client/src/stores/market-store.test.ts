import { describe, it, expect, beforeEach } from 'vitest';
import { useMarketStore } from './market-store';
import type { TickData } from '../types/market';

const tick1: TickData = {
  symbol: 'VCB',
  timestamp: '2026-03-13T10:00:00Z',
  price: 91500,
  volume: 100000,
  changePct: 1.25,
};

const tick2: TickData = {
  symbol: 'FPT',
  timestamp: '2026-03-13T10:00:00Z',
  price: 124000,
  volume: 50000,
  changePct: -0.5,
};

describe('market-store', () => {
  beforeEach(() => {
    // Reset store state between tests
    useMarketStore.setState({ ticks: new Map() });
  });

  it('updateTick adds a new symbol tick', () => {
    useMarketStore.getState().updateTick(tick1);
    const stored = useMarketStore.getState().ticks.get('VCB');
    expect(stored).toEqual(tick1);
  });

  it('updateTick updates existing tick for same symbol', () => {
    useMarketStore.getState().updateTick(tick1);
    const updated: TickData = { ...tick1, price: 92000, changePct: 2.0 };
    useMarketStore.getState().updateTick(updated);
    expect(useMarketStore.getState().ticks.get('VCB')?.price).toBe(92000);
  });

  it('updateTick stores multiple symbols independently', () => {
    useMarketStore.getState().updateTick(tick1);
    useMarketStore.getState().updateTick(tick2);
    expect(useMarketStore.getState().ticks.size).toBe(2);
    expect(useMarketStore.getState().ticks.get('FPT')?.changePct).toBe(-0.5);
  });

  it('clearTicks removes all entries', () => {
    useMarketStore.getState().updateTick(tick1);
    useMarketStore.getState().updateTick(tick2);
    useMarketStore.getState().clearTicks();
    expect(useMarketStore.getState().ticks.size).toBe(0);
  });
});
