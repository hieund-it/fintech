export interface StockInfo {
  symbol: string;
  name: string;
  exchange: 'HOSE' | 'HNX' | 'UPCOM' | string;
  sector: string | null;
}

export interface TickData {
  symbol: string;
  timestamp: string;
  price: number;
  volume: number;
  changePct: number;
}

export interface OhlcvBar {
  symbol: string;
  date: string;  // "YYYY-MM-DD"
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export type Timeframe = '1W' | '1M' | '3M' | '1Y';
