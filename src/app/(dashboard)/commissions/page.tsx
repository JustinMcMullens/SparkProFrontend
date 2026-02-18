'use client';

import { useState } from 'react';
import { DollarSign } from 'lucide-react';
import { useAllocations, useOverrides, useClawbacks, useBatchApproveAllocations } from '@/hooks/use-allocations';
import { useCurrentUser } from '@/lib/providers/AuthProvider';
import { PageHeader } from '@/components/shared/page-header';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatCurrency, formatDate } from '@/lib/utils';
import type { AllocationFilters, BatchApproveItem, Industry } from '@/types';

type Tab = 'allocations' | 'overrides' | 'clawbacks';

export default function CommissionsPage() {
  const [tab, setTab] = useState<Tab>('allocations');
  const [selected, setSelected] = useState<BatchApproveItem[]>([]);
  const { isManagement } = useCurrentUser();

  const filters: AllocationFilters = { page: 1, pageSize: 50 };
  const { data: allocData, isLoading: allocLoading } = useAllocations(filters);
  const { data: overrideData, isLoading: overrideLoading } = useOverrides(filters);
  const { data: clawbackData, isLoading: clawbackLoading } = useClawbacks(filters);
  const batchApprove = useBatchApproveAllocations();

  const allocations = allocData?.data ?? [];
  const overrides = overrideData?.data ?? [];
  const clawbacks = clawbackData?.data ?? [];

  function toggleSelect(item: BatchApproveItem) {
    setSelected((prev) => {
      const exists = prev.find(
        (s) => s.allocationId === item.allocationId && s.industry === item.industry,
      );
      return exists
        ? prev.filter((s) => !(s.allocationId === item.allocationId && s.industry === item.industry))
        : [...prev, item];
    });
  }

  async function handleBatchApprove() {
    if (selected.length === 0) return;
    await batchApprove.mutateAsync(selected);
    setSelected([]);
  }

  const tabs: { value: Tab; label: string; count: number }[] = [
    { value: 'allocations', label: 'Commissions', count: allocations.length },
    { value: 'overrides', label: 'Overrides', count: overrides.length },
    { value: 'clawbacks', label: 'Clawbacks', count: clawbacks.length },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Commissions"
        description="View and manage your commission allocations."
      >
        {isManagement && selected.length > 0 && (
          <Button size="sm" onClick={handleBatchApprove} disabled={batchApprove.isPending}>
            Approve {selected.length} selected
          </Button>
        )}
      </PageHeader>

      {/* Tabs */}
      <div className="flex gap-1 border-b">
        {tabs.map((t) => (
          <button
            key={t.value}
            onClick={() => setTab(t.value)}
            className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t.value
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {t.label}
            <span className="rounded-full bg-muted px-1.5 py-0.5 text-xs">{t.count}</span>
          </button>
        ))}
      </div>

      <Card>
        <CardContent className="p-0">
          {tab === 'allocations' && (
            <>
              {allocLoading ? (
                <LoadingSpinner />
              ) : allocations.length === 0 ? (
                <EmptyState icon={DollarSign} title="No allocations" />
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      {isManagement && <th className="w-10 px-4 py-3" />}
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Sale</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Industry</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Milestone</th>
                      <th className="px-6 py-3 text-right font-medium text-muted-foreground">Amount</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Status</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Date</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {allocations.map((a) => {
                      const isChecked = selected.some(
                        (s) => s.allocationId === a.allocationId && s.industry === a.industry,
                      );
                      return (
                        <tr key={`${a.industry}-${a.allocationId}`} className="hover:bg-muted/30">
                          {isManagement && (
                            <td className="px-4 py-3">
                              {!a.isApproved && (
                                <input
                                  type="checkbox"
                                  checked={isChecked}
                                  onChange={() =>
                                    toggleSelect({ industry: a.industry, allocationId: a.allocationId })
                                  }
                                  className="rounded"
                                />
                              )}
                            </td>
                          )}
                          <td className="px-6 py-3 font-medium">#{a.saleId}</td>
                          <td className="px-6 py-3">
                            <Badge variant="outline">{a.industry}</Badge>
                          </td>
                          <td className="px-6 py-3 text-muted-foreground">MP{a.milestoneNumber}</td>
                          <td className="px-6 py-3 text-right font-semibold">
                            {formatCurrency(a.allocatedAmount)}
                          </td>
                          <td className="px-6 py-3">
                            {a.isPaid ? (
                              <Badge className="bg-emerald-50 text-emerald-700 border-emerald-200 border">Paid</Badge>
                            ) : a.isApproved ? (
                              <Badge className="bg-blue-50 text-blue-700 border-blue-200 border">Approved</Badge>
                            ) : (
                              <Badge className="bg-amber-50 text-amber-700 border-amber-200 border">Pending</Badge>
                            )}
                          </td>
                          <td className="px-6 py-3 text-muted-foreground">
                            {formatDate(a.createdAt)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              )}
            </>
          )}

          {tab === 'overrides' && (
            <>
              {overrideLoading ? (
                <LoadingSpinner />
              ) : overrides.length === 0 ? (
                <EmptyState icon={DollarSign} title="No overrides" />
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Sale</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Level</th>
                      <th className="px-6 py-3 text-right font-medium text-muted-foreground">Amount</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {overrides.map((o) => (
                      <tr key={o.allocationId} className="hover:bg-muted/30">
                        <td className="px-6 py-3 font-medium">#{o.saleId}</td>
                        <td className="px-6 py-3 text-muted-foreground">Level {o.overrideLevel}</td>
                        <td className="px-6 py-3 text-right font-semibold">
                          {formatCurrency(o.allocatedAmount)}
                        </td>
                        <td className="px-6 py-3">
                          {o.isPaid ? (
                            <Badge className="bg-emerald-50 text-emerald-700 border-emerald-200 border">Paid</Badge>
                          ) : o.isApproved ? (
                            <Badge className="bg-blue-50 text-blue-700 border-blue-200 border">Approved</Badge>
                          ) : (
                            <Badge className="bg-amber-50 text-amber-700 border-amber-200 border">Pending</Badge>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          )}

          {tab === 'clawbacks' && (
            <>
              {clawbackLoading ? (
                <LoadingSpinner />
              ) : clawbacks.length === 0 ? (
                <EmptyState icon={DollarSign} title="No clawbacks" />
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Sale</th>
                      <th className="px-6 py-3 text-right font-medium text-muted-foreground">Amount</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Reason</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Date</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Processed</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {clawbacks.map((c) => (
                      <tr key={c.clawbackId} className="hover:bg-muted/30">
                        <td className="px-6 py-3 font-medium">#{c.saleId}</td>
                        <td className="px-6 py-3 text-right font-semibold text-destructive">
                          -{formatCurrency(c.clawbackAmount)}
                        </td>
                        <td className="px-6 py-3 text-muted-foreground">{c.clawbackReason}</td>
                        <td className="px-6 py-3 text-muted-foreground">
                          {formatDate(c.clawbackDate)}
                        </td>
                        <td className="px-6 py-3">
                          {c.isProcessed ? (
                            <Badge className="bg-emerald-50 text-emerald-700 border-emerald-200 border">Yes</Badge>
                          ) : (
                            <Badge className="bg-amber-50 text-amber-700 border-amber-200 border">No</Badge>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
