import { apiClient } from './client';
import type {
  ApiResponse,
  User,
  UpdateProfileRequest,
  Employee,
  OrgUser,
  CommissionSummary,
  SaleListItem,
  GoalProgress,
  PayrollDealWithParticipants,
  Company,
} from '@/types';

export async function getProfile(userId: number): Promise<User> {
  return apiClient.get<User>(`/api/user/${userId}`);
}

export async function updateProfile(userId: number, data: UpdateProfileRequest): Promise<User> {
  return apiClient.put<User>(`/api/user/${userId}`, data);
}

export async function getUserCompanies(userId: number): Promise<{
  userId: number;
  companies: Company[];
  count: number;
}> {
  return apiClient.get(`/api/user/${userId}/companies`);
}

export async function updateLastCompany(userId: number, companyId: number): Promise<void> {
  return apiClient.put(`/api/user/${userId}/last-company`, { companyId });
}

export async function getManagedUsers(userId: number): Promise<Employee[]> {
  return apiClient.get<Employee[]>(`/managedUsers/${userId}`);
}

export async function getPayrollDeals(userId: number): Promise<PayrollDealWithParticipants[]> {
  return apiClient.get<PayrollDealWithParticipants[]>(`/payroll/${userId}`);
}

export async function getReferrals(userId: number): Promise<Employee[]> {
  return apiClient.get<Employee[]>(`/referrals/${userId}`);
}

export async function getOrgUsers(requesterUserId: number): Promise<OrgUser[]> {
  return apiClient.get<OrgUser[]>(`/org/users/${requesterUserId}`);
}

export async function getCommissionSummary(
  userId: number,
  params?: { periodStart?: string; periodEnd?: string },
): Promise<ApiResponse<CommissionSummary>> {
  return apiClient.get<ApiResponse<CommissionSummary>>(
    `/api/profile/${userId}/commission-summary`,
    params,
  );
}

export async function getRecentSales(
  userId: number,
  count?: number,
): Promise<ApiResponse<SaleListItem[]>> {
  return apiClient.get<ApiResponse<SaleListItem[]>>(
    `/api/profile/${userId}/recent-sales`,
    { count },
  );
}

export async function getGoalProgress(
  userId: number,
): Promise<ApiResponse<GoalProgress[]>> {
  return apiClient.get<ApiResponse<GoalProgress[]>>(
    `/api/profile/${userId}/goal-progress`,
  );
}

export async function uploadProfileImage(
  userId: number,
  file: File,
  kind: 'profile' | 'profilebanner' | 'dashboardbanner',
): Promise<{ message: string; kind: string; url: string; absoluteUrl: string }> {
  const form = new FormData();
  form.append('file', file);
  form.append('kind', kind);
  return apiClient.postForm(`/profile/${userId}/images`, form);
}
