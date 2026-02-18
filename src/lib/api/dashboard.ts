import { apiClient } from './client';
import type { ApiResponse, DashboardStats, SaleListItem, LeaderboardEntry, Announcement } from '@/types';

export async function getDashboardStats(params?: {
  periodStart?: string;
  periodEnd?: string;
}): Promise<ApiResponse<DashboardStats>> {
  return apiClient.get<ApiResponse<DashboardStats>>('/api/dashboard/stats', params);
}

export async function getRecentActivity(count?: number): Promise<ApiResponse<SaleListItem[]>> {
  return apiClient.get<ApiResponse<SaleListItem[]>>('/api/dashboard/recent-activity', {
    count,
  });
}

export async function getLeaderboard(params?: {
  periodStart?: string;
  periodEnd?: string;
  limit?: number;
}): Promise<ApiResponse<LeaderboardEntry[]>> {
  return apiClient.get<ApiResponse<LeaderboardEntry[]>>('/api/dashboard/leaderboard', params);
}

export async function getDashboardAnnouncements(): Promise<ApiResponse<Announcement[]>> {
  return apiClient.get<ApiResponse<Announcement[]>>('/api/dashboard/announcements');
}
