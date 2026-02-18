import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  Ticket,
  TicketDetail,
  CreateTicketRequest,
  TicketFilters,
  TicketStatus,
} from '@/types';

export async function getTickets(
  filters: TicketFilters = {},
): Promise<PaginatedResponse<Ticket>> {
  return apiClient.get<PaginatedResponse<Ticket>>(
    '/api/tickets',
    filters as Record<string, string | number | boolean | undefined | null>,
  );
}

export async function getTicket(ticketId: number): Promise<ApiResponse<TicketDetail>> {
  return apiClient.get<ApiResponse<TicketDetail>>(`/api/tickets/${ticketId}`);
}

export async function createTicket(
  data: CreateTicketRequest,
): Promise<ApiResponse<Ticket>> {
  return apiClient.post<ApiResponse<Ticket>>('/api/tickets', data);
}

export async function updateTicket(
  ticketId: number,
  data: Partial<CreateTicketRequest>,
): Promise<ApiResponse<Ticket>> {
  return apiClient.put<ApiResponse<Ticket>>(`/api/tickets/${ticketId}`, data);
}

export async function addTicketComment(
  ticketId: number,
  body: string,
): Promise<ApiResponse<unknown>> {
  return apiClient.post<ApiResponse<unknown>>(
    `/api/tickets/${ticketId}/comments`,
    { body },
  );
}

export async function changeTicketStatus(
  ticketId: number,
  status: TicketStatus,
): Promise<ApiResponse<Ticket>> {
  return apiClient.post<ApiResponse<Ticket>>(
    `/api/tickets/${ticketId}/status`,
    { status },
  );
}

export async function assignTicket(
  ticketId: number,
  assignedUserId: number | null,
): Promise<ApiResponse<Ticket>> {
  return apiClient.post<ApiResponse<Ticket>>(
    `/api/tickets/${ticketId}/assign`,
    { assignedUserId },
  );
}
