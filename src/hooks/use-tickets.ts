import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ticketsApi } from '@/lib/api';
import type { CreateTicketRequest, TicketFilters, TicketStatus } from '@/types';

export function useTickets(filters: TicketFilters = {}) {
  return useQuery({
    queryKey: ['tickets', filters],
    queryFn: () => ticketsApi.getTickets(filters),
  });
}

export function useTicket(ticketId: number) {
  return useQuery({
    queryKey: ['tickets', ticketId],
    queryFn: () => ticketsApi.getTicket(ticketId),
    enabled: !!ticketId,
  });
}

export function useCreateTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTicketRequest) => ticketsApi.createTicket(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tickets'] }),
  });
}

export function useAddTicketComment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ticketId, body }: { ticketId: number; body: string }) =>
      ticketsApi.addTicketComment(ticketId, body),
    onSuccess: (_, { ticketId }) => {
      qc.invalidateQueries({ queryKey: ['tickets', ticketId] });
    },
  });
}

export function useChangeTicketStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      ticketId,
      status,
    }: {
      ticketId: number;
      status: TicketStatus;
    }) => ticketsApi.changeTicketStatus(ticketId, status),
    onSuccess: (_, { ticketId }) => {
      qc.invalidateQueries({ queryKey: ['tickets'] });
      qc.invalidateQueries({ queryKey: ['tickets', ticketId] });
    },
  });
}

export function useAssignTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      ticketId,
      assignedUserId,
    }: {
      ticketId: number;
      assignedUserId: number | null;
    }) => ticketsApi.assignTicket(ticketId, assignedUserId),
    onSuccess: (_, { ticketId }) => {
      qc.invalidateQueries({ queryKey: ['tickets', ticketId] });
    },
  });
}
