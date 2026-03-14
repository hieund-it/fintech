import apiClient from './api-client';

export interface WatchlistItem {
  id: string;
  symbol: string;
  addedAt: string;
}

export async function fetchWatchlist(): Promise<WatchlistItem[]> {
  const { data } = await apiClient.get<WatchlistItem[]>('/watchlist');
  return data;
}

export async function addToWatchlist(symbol: string): Promise<WatchlistItem> {
  const { data } = await apiClient.post<WatchlistItem>('/watchlist', { symbol });
  return data;
}

export async function removeFromWatchlist(id: string): Promise<void> {
  await apiClient.delete(`/watchlist/${id}`);
}
