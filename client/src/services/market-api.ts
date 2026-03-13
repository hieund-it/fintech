import apiClient from './api-client';
import type { OhlcvBar, StockInfo, Timeframe } from '../types/market';

export async function fetchStocks(params?: {
  exchange?: string;
  q?: string;
  sector?: string;
}): Promise<StockInfo[]> {
  const { data } = await apiClient.get<StockInfo[]>('/stocks', { params });
  return data;
}

export async function fetchStock(symbol: string): Promise<StockInfo> {
  const { data } = await apiClient.get<StockInfo>(`/stocks/${symbol}`);
  return data;
}

export async function fetchSectors(): Promise<string[]> {
  const { data } = await apiClient.get<string[]>('/stocks/sectors');
  return data;
}

/** Derive ISO date range string from a Timeframe label. */
function getDateRange(timeframe: Timeframe): { from: string; to: string } {
  const to = new Date();
  const from = new Date(to);

  switch (timeframe) {
    case '1W': from.setDate(to.getDate() - 7); break;
    case '1M': from.setMonth(to.getMonth() - 1); break;
    case '3M': from.setMonth(to.getMonth() - 3); break;
    case '1Y': from.setFullYear(to.getFullYear() - 1); break;
  }

  const fmt = (d: Date) => d.toISOString().slice(0, 10);
  return { from: fmt(from), to: fmt(to) };
}

export async function fetchOhlcv(
  symbol: string,
  timeframe: Timeframe,
): Promise<OhlcvBar[]> {
  const { from, to } = getDateRange(timeframe);
  const { data } = await apiClient.get<OhlcvBar[]>(`/stocks/${symbol}/ohlcv`, {
    params: { from, to },
  });
  return data;
}
