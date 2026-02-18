import { apiClient } from './client';
import type { PaystubsResponse, CommissionHistoryResponse } from '@/types';

export async function getPaystubs(): Promise<PaystubsResponse> {
  return apiClient.get<PaystubsResponse>('/api/paystubs');
}

export async function getCommissionHistory(): Promise<CommissionHistoryResponse> {
  return apiClient.get<CommissionHistoryResponse>('/api/paystubs/commissions');
}
