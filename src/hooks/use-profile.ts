import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { profileApi } from '@/lib/api';
import type { UpdateProfileRequest } from '@/types';

export function useProfile(userId: number) {
  return useQuery({
    queryKey: ['profile', userId],
    queryFn: () => profileApi.getProfile(userId),
    enabled: !!userId,
  });
}

export function useUpdateProfile(userId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => profileApi.updateProfile(userId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile', userId] });
    },
  });
}

export function useUploadProfileImage(userId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return profileApi.uploadProfileImage(userId, formData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile', userId] });
    },
  });
}

export function useManagedUsers(userId: number) {
  return useQuery({
    queryKey: ['profile', 'managed-users', userId],
    queryFn: () => profileApi.getManagedUsers(userId),
    enabled: !!userId,
  });
}

export function useMyPaystubs(userId: number) {
  return useQuery({
    queryKey: ['paystubs', userId],
    queryFn: () => profileApi.getPayrollDeals(userId),
    enabled: !!userId,
  });
}

export function useReferrals(userId: number) {
  return useQuery({
    queryKey: ['referrals', userId],
    queryFn: () => profileApi.getReferrals(userId),
    enabled: !!userId,
  });
}

export function useOrgUsers(userId: number) {
  return useQuery({
    queryKey: ['org', 'users', userId],
    queryFn: () => profileApi.getOrgUsers(userId),
    enabled: !!userId,
  });
}
