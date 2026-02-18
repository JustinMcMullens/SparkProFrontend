import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  UnifiedAllocation,
  OverrideAllocation,
  Clawback,
  AllocationFilters,
  BatchApproveItem,
  BatchApproveResult,
  Industry,
} from '@/types';

export async function getAllocations(
  filters: AllocationFilters = {},
): Promise<PaginatedResponse<UnifiedAllocation>> {
  return apiClient.get<PaginatedResponse<UnifiedAllocation>>(
    '/api/allocations',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function approveAllocation(
  industry: Lowercase<Industry>,
  allocationId: number,
): Promise<ApiResponse<unknown>> {
  return apiClient.post<ApiResponse<unknown>>(
    `/api/allocations/${industry}/${allocationId}/approve`,
  );
}

export async function batchApproveAllocations(
  allocations: BatchApproveItem[],
): Promise<ApiResponse<BatchApproveResult>> {
  return apiClient.post<ApiResponse<BatchApproveResult>>(
    '/api/allocations/batch-approve',
    { allocations },
  );
}

export async function getOverrides(
  filters: AllocationFilters = {},
): Promise<PaginatedResponse<OverrideAllocation>> {
  return apiClient.get<PaginatedResponse<OverrideAllocation>>(
    '/api/overrides',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function approveOverride(overrideId: number): Promise<ApiResponse<unknown>> {
  return apiClient.post<ApiResponse<unknown>>(`/api/overrides/${overrideId}/approve`);
}

export async function getClawbacks(
  filters: AllocationFilters = {},
): Promise<PaginatedResponse<Clawback>> {
  return apiClient.get<PaginatedResponse<Clawback>>(
    '/api/clawbacks',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}
