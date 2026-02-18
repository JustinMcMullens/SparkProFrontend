import { apiClient } from './client';
import type { ApiResponse, PaginatedResponse, CommissionRate, RateFilters, CreateRateRequest, Industry } from '@/types';

type IndustrySlug = Lowercase<Industry>;

export async function getRates(
  industry: IndustrySlug,
  filters: RateFilters = {},
): Promise<PaginatedResponse<CommissionRate>> {
  return apiClient.get<PaginatedResponse<CommissionRate>>(
    `/api/rates/${industry}`,
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function getUserRates(
  industry: IndustrySlug,
  userId: number,
): Promise<ApiResponse<CommissionRate[]>> {
  return apiClient.get<ApiResponse<CommissionRate[]>>(
    `/api/rates/${industry}/user/${userId}`,
  );
}

export async function createRate(
  industry: IndustrySlug,
  data: CreateRateRequest,
): Promise<ApiResponse<CommissionRate>> {
  return apiClient.post<ApiResponse<CommissionRate>>(`/api/rates/${industry}`, data);
}

export async function updateRate(
  industry: IndustrySlug,
  rateId: number,
  data: Partial<CreateRateRequest>,
): Promise<ApiResponse<CommissionRate>> {
  return apiClient.put<ApiResponse<CommissionRate>>(`/api/rates/${industry}/${rateId}`, data);
}

export async function deleteRate(industry: IndustrySlug, rateId: number): Promise<void> {
  return apiClient.delete<void>(`/api/rates/${industry}/${rateId}`);
}
