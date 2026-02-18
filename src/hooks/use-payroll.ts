import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { payrollApi } from '@/lib/api';
import type { BatchStatus, CreatePayrollBatchRequest, BatchApproveItem } from '@/types';

export function usePayrollBatches(params?: {
  page?: number;
  pageSize?: number;
  status?: BatchStatus;
}) {
  return useQuery({
    queryKey: ['payroll', 'batches', params],
    queryFn: () => payrollApi.getPayrollBatches(params),
  });
}

export function usePayrollBatch(batchId: number) {
  return useQuery({
    queryKey: ['payroll', 'batches', batchId],
    queryFn: () => payrollApi.getPayrollBatch(batchId),
    enabled: !!batchId,
  });
}

export function useCreatePayrollBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreatePayrollBatchRequest) => payrollApi.createPayrollBatch(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'batches'] }),
  });
}

export function useAddAllocationsToBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      batchId,
      allocations,
    }: {
      batchId: number;
      allocations: BatchApproveItem[];
    }) => payrollApi.addAllocationsToBatch(batchId, allocations),
    onSuccess: (_, { batchId }) => {
      qc.invalidateQueries({ queryKey: ['payroll', 'batches', batchId] });
    },
  });
}

export function useSubmitBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (batchId: number) => payrollApi.submitBatch(batchId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'batches'] }),
  });
}

export function useApproveBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (batchId: number) => payrollApi.approveBatch(batchId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'batches'] }),
  });
}

export function useExportBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (batchId: number) => payrollApi.exportBatch(batchId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'batches'] }),
  });
}

export function useMarkBatchPaid() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (batchId: number) => payrollApi.markBatchPaid(batchId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['payroll', 'batches'] });
      qc.invalidateQueries({ queryKey: ['allocations'] });
    },
  });
}
