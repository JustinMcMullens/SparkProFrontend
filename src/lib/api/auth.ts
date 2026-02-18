import { apiClient } from './client';
import type { LoginRequest, LoginResponse, SessionResponse } from '@/types';

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>('/login', credentials);
}

export async function getSession(): Promise<SessionResponse> {
  return apiClient.get<SessionResponse>('/auth/session');
}

export async function logout(): Promise<void> {
  return apiClient.post<void>('/auth/logout');
}

export async function updateLastCompany(companyId: number): Promise<void> {
  return apiClient.put<void>(`/companies/${companyId}/last-accessed`);
}
