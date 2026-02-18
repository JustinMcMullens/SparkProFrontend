import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  SaleListItem,
  SaleDetail,
  SaleFilters,
  UnifiedAllocation,
  OverrideAllocation,
  SaleCustomerNote,
  SaleNote,
} from '@/types';

export async function getSales(filters: SaleFilters = {}): Promise<PaginatedResponse<SaleListItem>> {
  const { page, pageSize, startDate, endDate, ...rest } = filters as Record<string, unknown>;
  return apiClient.get<PaginatedResponse<SaleListItem>>('/api/sales', {
    page,
    pageSize,
    dateFrom: startDate,
    dateTo: endDate,
    ...rest,
  } as Record<string, string | number | boolean | undefined | null>);
}

export async function getSaleDetail(saleId: number): Promise<ApiResponse<SaleDetail>> {
  return apiClient.get<ApiResponse<SaleDetail>>(`/api/sales/${saleId}`);
}

export async function cancelSale(saleId: number, reason: string): Promise<ApiResponse<unknown>> {
  return apiClient.post<ApiResponse<unknown>>(`/api/sales/${saleId}/cancel`, { reason });
}

export async function getSaleAllocations(saleId: number): Promise<ApiResponse<{
  allocations: UnifiedAllocation[];
  overrides: OverrideAllocation[];
}>> {
  return apiClient.get<ApiResponse<{ allocations: UnifiedAllocation[]; overrides: OverrideAllocation[] }>>(
    `/api/sales/${saleId}/allocations`,
  );
}

export async function getSaleNotes(saleId: number): Promise<ApiResponse<{
  customerNotes: SaleCustomerNote[];
  projectNotes: SaleNote[];
}>> {
  return apiClient.get<ApiResponse<{ customerNotes: SaleCustomerNote[]; projectNotes: SaleNote[] }>>(
    `/api/sales/${saleId}/notes`,
  );
}
