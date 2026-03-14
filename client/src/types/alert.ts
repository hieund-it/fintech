export interface PriceAlert {
  id: string;
  symbol: string;
  direction: 'ABOVE' | 'BELOW';
  threshold: number;
  isActive: boolean;
  triggeredAt: string | null;
  createdAt: string;
}

export interface CreateAlertRequest {
  symbol: string;
  direction: 'ABOVE' | 'BELOW';
  threshold: number;
}
