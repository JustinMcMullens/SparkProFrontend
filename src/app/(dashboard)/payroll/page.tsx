'use client';

import { useState } from 'react';
import Link from 'next/link';
import { CreditCard, Plus } from 'lucide-react';
import {
  usePayrollBatches,
  useCreatePayrollBatch,
  useSubmitBatch,
  useApproveBatch,
  useExportBatch,
  useMarkBatchPaid,
} from '@/hooks/use-payroll';
import { useCurrentUser } from '@/lib/providers/AuthProvider';
import { PageHeader } from '@/components/shared/page-header';
import { BatchStatusBadge } from '@/components/shared/sale-status-badge';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { formatCurrency, formatDate } from '@/lib/utils';
import type { BatchStatus } from '@/types';

export default function PayrollPage() {
  const { isAdmin } = useCurrentUser();
  const [statusFilter, setStatusFilter] = useState<BatchStatus | ''>('');
  const { data, isLoading } = usePayrollBatches(statusFilter ? { status: statusFilter } : {});
  const createBatch = useCreatePayrollBatch();
  const submitBatch = useSubmitBatch();
  const approveBatch = useApproveBatch();
  const exportBatch = useExportBatch();
  const markPaid = useMarkBatchPaid();

  const batches = data?.data ?? [];

  const statuses: { value: BatchStatus | ''; label: string }[] = [
    { value: '', label: 'All' },
    { value: 'DRAFT', label: 'Draft' },
    { value: 'SUBMITTED', label: 'Submitted' },
    { value: 'APPROVED', label: 'Approved' },
    { value: 'EXPORTED', label: 'Exported' },
    { value: 'PAID', label: 'Paid' },
  ];

  async function handleCreate() {
    await createBatch.mutateAsync({ description: `Batch ${new Date().toLocaleDateString()}` });
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Payroll Batches" description="Manage payroll for approved commissions.">
        <Button size="sm" onClick={handleCreate} disabled={createBatch.isPending}>
          <Plus className="mr-2 h-4 w-4" />
          New Batch
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
          ) : batches.length === 0 ? (
            <EmptyState icon={CreditCard} title="No batches" description="Create a batch to process payroll." />
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Batch</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Period</th>
                  <th className="px-6 py-3 text-right font-medium text-muted-foreground">Total</th>
                  <th className="px-6 py-3 text-right font-medium text-muted-foreground">Records</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Status</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {batches.map((b) => (
                  <tr key={b.batchId} className="hover:bg-muted/30 transition-colors">
                    <td className="px-6 py-3">
                      <Link href={`/payroll/${b.batchId}`} className="font-medium hover:text-primary hover:underline">
                        {b.description ?? `Batch #${b.batchId}`}
                      </Link>
                      <p className="text-xs text-muted-foreground">Created {formatDate(b.createdAt)}</p>
                    </td>
                    <td className="px-6 py-3 text-muted-foreground text-xs">
                      {b.periodStart && b.periodEnd
                        ? `${formatDate(b.periodStart)} – ${formatDate(b.periodEnd)}`
                        : '—'}
                    </td>
                    <td className="px-6 py-3 text-right font-semibold">
                      {formatCurrency(b.totalAmount)}
                    </td>
                    <td className="px-6 py-3 text-right text-muted-foreground">{b.recordCount}</td>
                    <td className="px-6 py-3">
                      <BatchStatusBadge status={b.status} />
                    </td>
                    <td className="px-6 py-3">
                      <div className="flex gap-1">
                        {b.status === 'DRAFT' && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => submitBatch.mutate(b.batchId)}
                            disabled={submitBatch.isPending}
                          >
                            Submit
                          </Button>
                        )}
                        {b.status === 'SUBMITTED' && isAdmin && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => approveBatch.mutate(b.batchId)}
                            disabled={approveBatch.isPending}
                          >
                            Approve
                          </Button>
                        )}
                        {b.status === 'APPROVED' && isAdmin && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => exportBatch.mutate(b.batchId)}
                            disabled={exportBatch.isPending}
                          >
                            Export
                          </Button>
                        )}
                        {b.status === 'EXPORTED' && isAdmin && (
                          <Button
                            size="sm"
                            onClick={() => markPaid.mutate(b.batchId)}
                            disabled={markPaid.isPending}
                          >
                            Mark Paid
                          </Button>
                        )}
                      </div>
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
