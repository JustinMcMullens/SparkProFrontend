import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  SaleListItem,
  SaleFilters,
  TeamMember,
  TeamPerformance,
  UnifiedAllocation,
} from '@/types';

export async function getTeamSales(filters: SaleFilters = {}): Promise<PaginatedResponse<SaleListItem>> {
  const { startDate, endDate, ...rest } = filters as Record<string, unknown>;
  return apiClient.get<PaginatedResponse<SaleListItem>>('/api/team/sales', {
    dateFrom: startDate,
    dateTo: endDate,
    ...rest,
  } as Record<string, string | number | boolean | undefined | null>);
}

export async function getTeamMembers(): Promise<ApiResponse<TeamMember[]>> {
  return apiClient.get<ApiResponse<TeamMember[]>>('/api/team/members');
}

export async function getTeamPerformance(params?: {
  periodStart?: string;
  periodEnd?: string;
}): Promise<ApiResponse<TeamPerformance>> {
  return apiClient.get<ApiResponse<TeamPerformance>>('/api/team/performance', params);
}

export async function getPendingApprovals(): Promise<ApiResponse<UnifiedAllocation[]>> {
  return apiClient.get<ApiResponse<UnifiedAllocation[]>>('/api/team/pending-approvals');
}
