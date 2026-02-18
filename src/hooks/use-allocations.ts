import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { allocationsApi } from '@/lib/api';
import type { AllocationFilters, BatchApproveItem, Industry } from '@/types';

export function useAllocations(filters: AllocationFilters = {}) {
  return useQuery({
    queryKey: ['allocations', filters],
    queryFn: () => allocationsApi.getAllocations(filters),
  });
}

export function useOverrides(filters: AllocationFilters = {}) {
  return useQuery({
    queryKey: ['overrides', filters],
    queryFn: () => allocationsApi.getOverrides(filters),
  });
}

export function useClawbacks(filters: AllocationFilters = {}) {
  return useQuery({
    queryKey: ['clawbacks', filters],
    queryFn: () => allocationsApi.getClawbacks(filters),
  });
}

export function useApproveAllocation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      industry,
      allocationId,
    }: {
      industry: Lowercase<Industry>;
      allocationId: number;
    }) => allocationsApi.approveAllocation(industry, allocationId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['allocations'] });
      qc.invalidateQueries({ queryKey: ['team', 'pending-approvals'] });
    },
  });
}

export function useBatchApproveAllocations() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (allocations: BatchApproveItem[]) =>
      allocationsApi.batchApproveAllocations(allocations),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['allocations'] });
      qc.invalidateQueries({ queryKey: ['team', 'pending-approvals'] });
    },
  });
}
