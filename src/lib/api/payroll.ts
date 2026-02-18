import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  PayrollBatch,
  PayrollBatchDetail,
  CreatePayrollBatchRequest,
  BatchApproveItem,
  BatchStatus,
} from '@/types';

export async function getPayrollBatches(params?: {
  page?: number;
  pageSize?: number;
  status?: BatchStatus;
}): Promise<PaginatedResponse<PayrollBatch>> {
  return apiClient.get<PaginatedResponse<PayrollBatch>>(
    '/api/payroll/batches',
    params as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function getPayrollBatch(batchId: number): Promise<ApiResponse<PayrollBatchDetail>> {
  return apiClient.get<ApiResponse<PayrollBatchDetail>>(`/api/payroll/batches/${batchId}`);
}

export async function createPayrollBatch(
  data: CreatePayrollBatchRequest,
): Promise<ApiResponse<PayrollBatch>> {
  return apiClient.post<ApiResponse<PayrollBatch>>('/api/payroll/batches', data);
}

export async function updatePayrollBatch(
  batchId: number,
  data: Partial<CreatePayrollBatchRequest>,
): Promise<ApiResponse<PayrollBatch>> {
  return apiClient.put<ApiResponse<PayrollBatch>>(`/api/payroll/batches/${batchId}`, data);
}

export async function addAllocationsToBatch(
  batchId: number,
  allocations: BatchApproveItem[],
): Promise<ApiResponse<unknown>> {
  return apiClient.post<ApiResponse<unknown>>(
    `/api/payroll/batches/${batchId}/add-allocations`,
    { allocations },
  );
}

export async function submitBatch(batchId: number): Promise<ApiResponse<PayrollBatch>> {
  return apiClient.post<ApiResponse<PayrollBatch>>(`/api/payroll/batches/${batchId}/submit`);
}

export async function approveBatch(batchId: number): Promise<ApiResponse<PayrollBatch>> {
  return apiClient.post<ApiResponse<PayrollBatch>>(`/api/payroll/batches/${batchId}/approve`);
}

export async function exportBatch(batchId: number): Promise<ApiResponse<PayrollBatch>> {
  return apiClient.post<ApiResponse<PayrollBatch>>(`/api/payroll/batches/${batchId}/export`);
}

export async function markBatchPaid(batchId: number): Promise<ApiResponse<PayrollBatch>> {
  return apiClient.post<ApiResponse<PayrollBatch>>(`/api/payroll/batches/${batchId}/mark-paid`);
}
