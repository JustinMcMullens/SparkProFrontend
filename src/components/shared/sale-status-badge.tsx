import { Badge } from '@/components/ui/badge';
import type { SaleStatus, BatchStatus, TicketStatus } from '@/types';
import { cn } from '@/lib/utils';

const saleStatusConfig: Record<SaleStatus, { label: string; className: string }> = {
  PENDING:   { label: 'Pending',   className: 'bg-amber-100 text-amber-800 border-amber-200' },
  APPROVED:  { label: 'Approved',  className: 'bg-blue-100 text-blue-800 border-blue-200' },
  INSTALLED: { label: 'Installed', className: 'bg-violet-100 text-violet-800 border-violet-200' },
  COMPLETED: { label: 'Completed', className: 'bg-emerald-100 text-emerald-800 border-emerald-200' },
  CANCELLED: { label: 'Cancelled', className: 'bg-slate-100 text-slate-600 border-slate-200' },
  ON_HOLD:   { label: 'On Hold',   className: 'bg-orange-100 text-orange-800 border-orange-200' },
};

const batchStatusConfig: Record<BatchStatus, { label: string; className: string }> = {
  DRAFT:     { label: 'Draft',     className: 'bg-slate-100 text-slate-600 border-slate-200' },
  SUBMITTED: { label: 'Submitted', className: 'bg-amber-100 text-amber-800 border-amber-200' },
  APPROVED:  { label: 'Approved',  className: 'bg-blue-100 text-blue-800 border-blue-200' },
  EXPORTED:  { label: 'Exported',  className: 'bg-violet-100 text-violet-800 border-violet-200' },
  PAID:      { label: 'Paid',      className: 'bg-emerald-100 text-emerald-800 border-emerald-200' },
  CANCELLED: { label: 'Cancelled', className: 'bg-red-100 text-red-800 border-red-200' },
};

const ticketStatusConfig: Record<TicketStatus, { label: string; className: string }> = {
  OPEN:        { label: 'Open',        className: 'bg-blue-100 text-blue-800 border-blue-200' },
  IN_PROGRESS: { label: 'In Progress', className: 'bg-amber-100 text-amber-800 border-amber-200' },
  PENDING:     { label: 'Pending',     className: 'bg-orange-100 text-orange-800 border-orange-200' },
  RESOLVED:    { label: 'Resolved',    className: 'bg-emerald-100 text-emerald-800 border-emerald-200' },
  CLOSED:      { label: 'Closed',      className: 'bg-slate-100 text-slate-600 border-slate-200' },
};

export function SaleStatusBadge({ status }: { status: SaleStatus }) {
  const cfg = saleStatusConfig[status] ?? { label: status, className: '' };
  return <Badge className={cn('border font-medium', cfg.className)}>{cfg.label}</Badge>;
}

export function BatchStatusBadge({ status }: { status: BatchStatus }) {
  const cfg = batchStatusConfig[status] ?? { label: status, className: '' };
  return <Badge className={cn('border font-medium', cfg.className)}>{cfg.label}</Badge>;
}

export function TicketStatusBadge({ status }: { status: TicketStatus }) {
  const cfg = ticketStatusConfig[status] ?? { label: status, className: '' };
  return <Badge className={cn('border font-medium', cfg.className)}>{cfg.label}</Badge>;
}
