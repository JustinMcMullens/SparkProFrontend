import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { salesApi } from '@/lib/api';
import type { SaleFilters } from '@/types';

export function useSales(filters: SaleFilters = {}) {
  return useQuery({
    queryKey: ['sales', filters],
    queryFn: () => salesApi.getSales(filters),
  });
}

export function useSale(saleId: number) {
  return useQuery({
    queryKey: ['sales', saleId],
    queryFn: () => salesApi.getSaleDetail(saleId),
    enabled: !!saleId,
  });
}

export function useSaleAllocations(saleId: number) {
  return useQuery({
    queryKey: ['sales', saleId, 'allocations'],
    queryFn: () => salesApi.getSaleAllocations(saleId),
    enabled: !!saleId,
  });
}

export function useSaleNotes(saleId: number) {
  return useQuery({
    queryKey: ['sales', saleId, 'notes'],
    queryFn: () => salesApi.getSaleNotes(saleId),
    enabled: !!saleId,
  });
}

export function useCancelSale() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ saleId, reason }: { saleId: number; reason: string }) =>
      salesApi.cancelSale(saleId, reason),
    onSuccess: (_, { saleId }) => {
      qc.invalidateQueries({ queryKey: ['sales'] });
      qc.invalidateQueries({ queryKey: ['sales', saleId] });
    },
  });
}
