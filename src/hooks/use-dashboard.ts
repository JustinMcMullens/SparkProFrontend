import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '@/lib/api';

export function useDashboardStats(params?: { periodStart?: string; periodEnd?: string }) {
  return useQuery({
    queryKey: ['dashboard', 'stats', params],
    queryFn: () => dashboardApi.getDashboardStats(params),
  });
}

export function useRecentActivity(count?: number) {
  return useQuery({
    queryKey: ['dashboard', 'recent-activity', count],
    queryFn: () => dashboardApi.getRecentActivity(count),
  });
}

export function useLeaderboard(params?: {
  periodStart?: string;
  periodEnd?: string;
  limit?: number;
}) {
  return useQuery({
    queryKey: ['dashboard', 'leaderboard', params],
    queryFn: () => dashboardApi.getLeaderboard(params),
  });
}

export function useDashboardAnnouncements() {
  return useQuery({
    queryKey: ['dashboard', 'announcements'],
    queryFn: () => dashboardApi.getDashboardAnnouncements(),
  });
}
