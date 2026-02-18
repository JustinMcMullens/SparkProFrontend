import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  Installer,
  Dealer,
  FinanceCompany,
  Partner,
  CompanySettings,
} from '@/types';

interface CollaboratorFilters {
  page?: number;
  pageSize?: number;
  isActive?: boolean;
}

interface InstallerFilters extends CollaboratorFilters {
  projectType?: string;
  state?: string;
  isPreferred?: boolean;
}

// ---- Installers ----

export async function getInstallers(
  filters: InstallerFilters = {},
): Promise<PaginatedResponse<Installer>> {
  return apiClient.get<PaginatedResponse<Installer>>(
    '/api/admin/installers',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function createInstaller(
  data: Omit<Installer, 'id'>,
): Promise<ApiResponse<Installer>> {
  return apiClient.post<ApiResponse<Installer>>('/api/admin/installers', data);
}

export async function updateInstaller(
  id: number,
  data: Partial<Installer>,
): Promise<ApiResponse<Installer>> {
  return apiClient.put<ApiResponse<Installer>>(`/api/admin/installers/${id}`, data);
}

export async function deleteInstaller(id: number): Promise<void> {
  return apiClient.delete<void>(`/api/admin/installers/${id}`);
}

export async function getInstallerCoverage(
  id: number,
): Promise<ApiResponse<Array<{ projectType: string; stateCode: string }>>> {
  return apiClient.get(`/api/admin/installers/${id}/coverage`);
}

// ---- Dealers ----

export async function getDealers(
  filters: CollaboratorFilters = {},
): Promise<PaginatedResponse<Dealer>> {
  return apiClient.get<PaginatedResponse<Dealer>>(
    '/api/admin/dealers',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function createDealer(data: Omit<Dealer, 'id'>): Promise<ApiResponse<Dealer>> {
  return apiClient.post<ApiResponse<Dealer>>('/api/admin/dealers', data);
}

export async function updateDealer(
  id: number,
  data: Partial<Dealer>,
): Promise<ApiResponse<Dealer>> {
  return apiClient.put<ApiResponse<Dealer>>(`/api/admin/dealers/${id}`, data);
}

export async function deleteDealer(id: number): Promise<void> {
  return apiClient.delete<void>(`/api/admin/dealers/${id}`);
}

// ---- Finance Companies ----

export async function getFinanceCompanies(
  filters: CollaboratorFilters = {},
): Promise<PaginatedResponse<FinanceCompany>> {
  return apiClient.get<PaginatedResponse<FinanceCompany>>(
    '/api/admin/finance-companies',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function createFinanceCompany(
  data: Omit<FinanceCompany, 'id'>,
): Promise<ApiResponse<FinanceCompany>> {
  return apiClient.post<ApiResponse<FinanceCompany>>('/api/admin/finance-companies', data);
}

export async function updateFinanceCompany(
  id: number,
  data: Partial<FinanceCompany>,
): Promise<ApiResponse<FinanceCompany>> {
  return apiClient.put<ApiResponse<FinanceCompany>>(
    `/api/admin/finance-companies/${id}`,
    data,
  );
}

export async function deleteFinanceCompany(id: number): Promise<void> {
  return apiClient.delete<void>(`/api/admin/finance-companies/${id}`);
}

// ---- Partners ----

export async function getPartners(
  filters: CollaboratorFilters = {},
): Promise<PaginatedResponse<Partner>> {
  return apiClient.get<PaginatedResponse<Partner>>(
    '/api/admin/partners',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function createPartner(
  data: Omit<Partner, 'id'>,
): Promise<ApiResponse<Partner>> {
  return apiClient.post<ApiResponse<Partner>>('/api/admin/partners', data);
}

export async function updatePartner(
  id: number,
  data: Partial<Partner>,
): Promise<ApiResponse<Partner>> {
  return apiClient.put<ApiResponse<Partner>>(`/api/admin/partners/${id}`, data);
}

export async function deletePartner(id: number): Promise<void> {
  return apiClient.delete<void>(`/api/admin/partners/${id}`);
}

// ---- Company Settings ----

export async function getCompanySettings(): Promise<ApiResponse<CompanySettings>> {
  return apiClient.get<ApiResponse<CompanySettings>>('/api/settings/company');
}
