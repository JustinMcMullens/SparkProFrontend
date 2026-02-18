import { useQuery } from '@tanstack/react-query';
import { teamApi } from '@/lib/api';
import type { SaleFilters } from '@/types';

export function useTeamSales(filters: SaleFilters = {}) {
  return useQuery({
    queryKey: ['team', 'sales', filters],
    queryFn: () => teamApi.getTeamSales(filters),
  });
}

export function useTeamMembers() {
  return useQuery({
    queryKey: ['team', 'members'],
    queryFn: () => teamApi.getTeamMembers(),
  });
}

export function useTeamPerformance(params?: { periodStart?: string; periodEnd?: string }) {
  return useQuery({
    queryKey: ['team', 'performance', params],
    queryFn: () => teamApi.getTeamPerformance(params),
  });
}

export function usePendingApprovals() {
  return useQuery({
    queryKey: ['team', 'pending-approvals'],
    queryFn: () => teamApi.getPendingApprovals(),
  });
}
