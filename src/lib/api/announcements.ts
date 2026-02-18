import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  Announcement,
  CreateAnnouncementRequest,
} from '@/types';

export async function getAnnouncements(): Promise<PaginatedResponse<Announcement>> {
  return apiClient.get<PaginatedResponse<Announcement>>('/api/announcements');
}

export async function getAnnouncement(
  announcementId: number,
): Promise<ApiResponse<Announcement>> {
  return apiClient.get<ApiResponse<Announcement>>(`/api/announcements/${announcementId}`);
}

export async function getUnreadCount(): Promise<ApiResponse<{ unreadCount: number }>> {
  return apiClient.get<ApiResponse<{ unreadCount: number }>>(
    '/api/announcements/unread-count',
  );
}

export async function createAnnouncement(
  data: CreateAnnouncementRequest,
): Promise<ApiResponse<Announcement>> {
  return apiClient.post<ApiResponse<Announcement>>('/api/announcements', data);
}

export async function updateAnnouncement(
  announcementId: number,
  data: Partial<CreateAnnouncementRequest>,
): Promise<ApiResponse<Announcement>> {
  return apiClient.put<ApiResponse<Announcement>>(
    `/api/announcements/${announcementId}`,
    data,
  );
}

export async function deleteAnnouncement(announcementId: number): Promise<void> {
  return apiClient.delete<void>(`/api/announcements/${announcementId}`);
}

export async function acknowledgeAnnouncement(announcementId: number): Promise<void> {
  return apiClient.post<void>(`/api/announcements/${announcementId}/acknowledge`);
}
