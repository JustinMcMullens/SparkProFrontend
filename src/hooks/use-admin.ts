import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '@/lib/api';

export function useInstallers() {
  return useQuery({
    queryKey: ['admin', 'installers'],
    queryFn: () => adminApi.getInstallers(),
  });
}

export function useCreateInstaller() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createInstaller,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'installers'] });
    },
  });
}

export function useUpdateInstaller() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof adminApi.updateInstaller>[1] }) =>
      adminApi.updateInstaller(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'installers'] });
    },
  });
}

export function useDeleteInstaller() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.deleteInstaller,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'installers'] });
    },
  });
}

export function useDealers() {
  return useQuery({
    queryKey: ['admin', 'dealers'],
    queryFn: () => adminApi.getDealers(),
  });
}

export function useCreateDealer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createDealer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'dealers'] });
    },
  });
}

export function useUpdateDealer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof adminApi.updateDealer>[1] }) =>
      adminApi.updateDealer(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'dealers'] });
    },
  });
}

export function useDeleteDealer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.deleteDealer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'dealers'] });
    },
  });
}

export function useFinanceCompanies() {
  return useQuery({
    queryKey: ['admin', 'finance-companies'],
    queryFn: () => adminApi.getFinanceCompanies(),
  });
}

export function useCreateFinanceCompany() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createFinanceCompany,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'finance-companies'] });
    },
  });
}

export function useUpdateFinanceCompany() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof adminApi.updateFinanceCompany>[1] }) =>
      adminApi.updateFinanceCompany(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'finance-companies'] });
    },
  });
}

export function useDeleteFinanceCompany() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.deleteFinanceCompany,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'finance-companies'] });
    },
  });
}

export function usePartners() {
  return useQuery({
    queryKey: ['admin', 'partners'],
    queryFn: () => adminApi.getPartners(),
  });
}

export function useCreatePartner() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createPartner,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'partners'] });
    },
  });
}

export function useUpdatePartner() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof adminApi.updatePartner>[1] }) =>
      adminApi.updatePartner(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'partners'] });
    },
  });
}

export function useDeletePartner() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.deletePartner,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'partners'] });
    },
  });
}

export function useCompanySettings() {
  return useQuery({
    queryKey: ['admin', 'company-settings'],
    queryFn: () => adminApi.getCompanySettings(),
  });
}
