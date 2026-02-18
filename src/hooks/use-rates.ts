import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ratesApi } from '@/lib/api';
import type { Industry, CreateCommissionRateRequest, UpdateCommissionRateRequest } from '@/types';

type IndustrySlug = Lowercase<Industry>;
const slug = (i: Industry): IndustrySlug => i.toLowerCase() as IndustrySlug;

export function useRates(industry: Industry) {
  return useQuery({
    queryKey: ['rates', industry],
    queryFn: () => ratesApi.getRates(slug(industry)),
    enabled: !!industry,
  });
}

export function useCreateRate(industry: Industry) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCommissionRateRequest) => ratesApi.createRate(slug(industry), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rates', industry] });
    },
  });
}

export function useUpdateRate(industry: Industry) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ rateId, data }: { rateId: number; data: UpdateCommissionRateRequest }) =>
      ratesApi.updateRate(slug(industry), rateId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rates', industry] });
    },
  });
}

export function useDeleteRate(industry: Industry) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (rateId: number) => ratesApi.deleteRate(slug(industry), rateId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rates', industry] });
    },
  });
}
