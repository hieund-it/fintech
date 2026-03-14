export interface Portfolio {
  id: string;
  name: string;
  createdAt: string;
}

export interface Transaction {
  id: string;
  portfolioId: string;
  symbol: string;
  type: 'BUY' | 'SELL';
  quantity: number;
  price: number;
  fee: number;
  transactedAt: string;
}

export interface CreateTransactionRequest {
  symbol: string;
  type: 'BUY' | 'SELL';
  quantity: number;
  price: number;
  fee: number;
  transactedAt: string;
}

export interface Position {
  symbol: string;
  quantity: number;
  avgCost: number;
  realizedPnL: number;
  unrealizedPnL: number;
  currentPrice: number;
}

export interface PortfolioPnL {
  portfolioId: string;
  portfolioName: string;
  positions: Position[];
  totalRealizedPnL: number;
  totalUnrealizedPnL: number;
}
