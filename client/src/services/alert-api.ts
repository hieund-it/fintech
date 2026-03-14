import apiClient from './api-client';
import type { PriceAlert, CreateAlertRequest } from '../types/alert';

export async function fetchAlerts(): Promise<PriceAlert[]> {
  const { data } = await apiClient.get<PriceAlert[]>('/alerts');
  return data;
}

export async function createAlert(req: CreateAlertRequest): Promise<PriceAlert> {
  const { data } = await apiClient.post<PriceAlert>('/alerts', req);
  return data;
}

export async function deleteAlert(id: string): Promise<void> {
  await apiClient.delete(`/alerts/${id}`);
}

export async function deactivateAlert(id: string): Promise<void> {
  await apiClient.patch(`/alerts/${id}/deactivate`);
}
