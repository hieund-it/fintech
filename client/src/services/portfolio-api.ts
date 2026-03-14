import apiClient from './api-client';
import type {
  Portfolio,
  Transaction,
  CreateTransactionRequest,
  PortfolioPnL,
} from '../types/portfolio';

export async function fetchPortfolios(): Promise<Portfolio[]> {
  const { data } = await apiClient.get<Portfolio[]>('/portfolios');
  return data;
}

export async function createPortfolio(name: string): Promise<Portfolio> {
  const { data } = await apiClient.post<Portfolio>('/portfolios', { name });
  return data;
}

export async function deletePortfolio(id: string): Promise<void> {
  await apiClient.delete(`/portfolios/${id}`);
}

export async function fetchTransactions(portfolioId: string): Promise<Transaction[]> {
  const { data } = await apiClient.get<Transaction[]>(`/portfolios/${portfolioId}/transactions`);
  return data;
}

export async function addTransaction(
  portfolioId: string,
  req: CreateTransactionRequest,
): Promise<Transaction> {
  const { data } = await apiClient.post<Transaction>(
    `/portfolios/${portfolioId}/transactions`,
    req,
  );
  return data;
}

export async function deleteTransaction(portfolioId: string, txnId: string): Promise<void> {
  await apiClient.delete(`/portfolios/${portfolioId}/transactions/${txnId}`);
}

export async function fetchPnL(portfolioId: string): Promise<PortfolioPnL> {
  const { data } = await apiClient.get<PortfolioPnL>(`/portfolios/${portfolioId}/pnl`);
  return data;
}
