import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { announcementsApi } from '@/lib/api';
import type { CreateAnnouncementRequest } from '@/types';

export function useAnnouncements() {
  return useQuery({
    queryKey: ['announcements'],
    queryFn: () => announcementsApi.getAnnouncements(),
  });
}

export function useAnnouncement(id: number) {
  return useQuery({
    queryKey: ['announcements', id],
    queryFn: () => announcementsApi.getAnnouncement(id),
    enabled: !!id,
  });
}

export function useUnreadAnnouncementCount() {
  return useQuery({
    queryKey: ['announcements', 'unread-count'],
    queryFn: () => announcementsApi.getUnreadCount(),
    refetchInterval: 60 * 1000, // poll every minute
  });
}

export function useAcknowledgeAnnouncement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => announcementsApi.acknowledgeAnnouncement(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['announcements'] });
      qc.invalidateQueries({ queryKey: ['announcements', 'unread-count'] });
    },
  });
}

export function useCreateAnnouncement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAnnouncementRequest) =>
      announcementsApi.createAnnouncement(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['announcements'] }),
  });
}

export function useDeleteAnnouncement() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => announcementsApi.deleteAnnouncement(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['announcements'] }),
  });
}
