'use client';

import { useState } from 'react';
import { Ticket as TicketIcon, Plus } from 'lucide-react';
import { useTickets, useCreateTicket } from '@/hooks/use-tickets';
import { PageHeader } from '@/components/shared/page-header';
import { TicketStatusBadge } from '@/components/shared/sale-status-badge';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { formatDate } from '@/lib/utils';
import type { TicketPriority, TicketStatus } from '@/types';

const PRIORITY_COLOR: Record<NonNullable<TicketPriority>, string> = {
  LOW: 'text-slate-500',
  MEDIUM: 'text-amber-600',
  HIGH: 'text-orange-600',
  URGENT: 'text-red-600',
};

export default function TicketsPage() {
  const [statusFilter, setStatusFilter] = useState<TicketStatus | ''>('');
  const { data, isLoading } = useTickets(statusFilter ? { status: statusFilter } : {});
  const tickets = data?.data ?? [];

  const statuses: { value: TicketStatus | ''; label: string }[] = [
    { value: '', label: 'All' },
    { value: 'OPEN', label: 'Open' },
    { value: 'IN_PROGRESS', label: 'In Progress' },
    { value: 'PENDING', label: 'Pending' },
    { value: 'RESOLVED', label: 'Resolved' },
    { value: 'CLOSED', label: 'Closed' },
  ];

  return (
    <div className="space-y-6">
      <PageHeader title="Support Tickets" description="Submit and track support requests.">
        <Button size="sm" asChild>
          <a href="/tickets/new">
            <Plus className="mr-2 h-4 w-4" />
            New Ticket
          </a>
        </Button>
      </PageHeader>

      <div className="flex flex-wrap gap-2">
        {statuses.map((s) => (
          <button
            key={s.value}
            onClick={() => setStatusFilter(s.value)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors border ${
              statusFilter === s.value
                ? 'bg-primary text-primary-foreground border-primary'
                : 'bg-background text-muted-foreground border-input hover:bg-muted'
            }`}
          >
            {s.label}
          </button>
        ))}
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <LoadingSpinner />
          ) : tickets.length === 0 ? (
            <EmptyState icon={TicketIcon} title="No tickets" description="No tickets match your filter." />
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Subject</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Priority</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Status</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {tickets.map((t) => (
                  <tr key={t.ticketId} className="hover:bg-muted/30 transition-colors">
                    <td className="px-6 py-3">
                      <a
                        href={`/tickets/${t.ticketId}`}
                        className="font-medium hover:text-primary hover:underline"
                      >
                        {t.subject}
                      </a>
                      {t.description && (
                        <p className="text-xs text-muted-foreground mt-0.5 line-clamp-1">
                          {t.description}
                        </p>
                      )}
                    </td>
                    <td className="px-6 py-3">
                      {t.priority && (
                        <span className={`text-xs font-semibold ${PRIORITY_COLOR[t.priority] ?? ''}`}>
                          {t.priority}
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-3">
                      <TicketStatusBadge status={t.status} />
                    </td>
                    <td className="px-6 py-3 text-muted-foreground text-xs">
                      {formatDate(t.createdAt)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
