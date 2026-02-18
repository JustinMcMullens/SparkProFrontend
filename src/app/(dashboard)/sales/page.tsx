'use client';

import { useState } from 'react';
import Link from 'next/link';
import { TrendingUp, Search } from 'lucide-react';
import { useSales } from '@/hooks/use-sales';
import { PageHeader } from '@/components/shared/page-header';
import { SaleStatusBadge } from '@/components/shared/sale-status-badge';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { formatCurrency, formatDate } from '@/lib/utils';
import type { SaleFilters, SaleStatus } from '@/types';

const STATUS_OPTIONS: { value: SaleStatus | ''; label: string }[] = [
  { value: '', label: 'All' },
  { value: 'PENDING', label: 'Pending' },
  { value: 'APPROVED', label: 'Approved' },
  { value: 'INSTALLED', label: 'Installed' },
  { value: 'COMPLETED', label: 'Completed' },
  { value: 'CANCELLED', label: 'Cancelled' },
];

export default function SalesPage() {
  const [filters, setFilters] = useState<SaleFilters>({ page: 1, pageSize: 25 });
  const [statusFilter, setStatusFilter] = useState<SaleStatus | ''>('');

  const activeFilters: SaleFilters = {
    ...filters,
    ...(statusFilter ? { status: statusFilter } : {}),
  };

  const { data, isLoading } = useSales(activeFilters);
  const sales = data?.data ?? [];
  const meta = data?.meta;

  return (
    <div className="space-y-6">
      <PageHeader title="Sales" description="Track your sales and commission status." />

      {/* Filters */}
      <div className="flex flex-wrap gap-2">
        {STATUS_OPTIONS.map((opt) => (
          <button
            key={opt.value}
            onClick={() => setStatusFilter(opt.value)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors border ${
              statusFilter === opt.value
                ? 'bg-primary text-primary-foreground border-primary'
                : 'bg-background text-muted-foreground border-input hover:bg-muted'
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <LoadingSpinner />
          ) : sales.length === 0 ? (
            <EmptyState
              icon={TrendingUp}
              title="No sales found"
              description="No sales match your current filters."
            />
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Customer</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Type</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Date</th>
                      <th className="px-6 py-3 text-right font-medium text-muted-foreground">Amount</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Participants</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {sales.map((sale) => (
                      <tr key={sale.saleId} className="hover:bg-muted/30 transition-colors">
                        <td className="px-6 py-3">
                          <Link
                            href={`/sales/${sale.saleId}`}
                            className="font-medium hover:text-primary hover:underline"
                          >
                            {sale.customer.firstName} {sale.customer.lastName}
                          </Link>
                          {sale.customer.city && (
                            <p className="text-xs text-muted-foreground">
                              {sale.customer.city}, {sale.customer.stateCode}
                            </p>
                          )}
                        </td>
                        <td className="px-6 py-3">
                          <Badge variant="outline">{sale.projectType}</Badge>
                        </td>
                        <td className="px-6 py-3 text-muted-foreground">
                          {formatDate(sale.saleDate)}
                        </td>
                        <td className="px-6 py-3 text-right font-semibold">
                          {formatCurrency(sale.contractAmount)}
                        </td>
                        <td className="px-6 py-3">
                          <div className="flex flex-wrap gap-1">
                            {sale.participants.slice(0, 3).map((p) => (
                              <span
                                key={p.userId}
                                className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium"
                              >
                                {p.firstName} {p.lastName[0]}.
                              </span>
                            ))}
                            {sale.participants.length > 3 && (
                              <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">
                                +{sale.participants.length - 3}
                              </span>
                            )}
                          </div>
                        </td>
                        <td className="px-6 py-3">
                          <SaleStatusBadge status={sale.saleStatus} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {meta && meta.totalCount > meta.pageSize && (
                <div className="flex items-center justify-between border-t px-6 py-3">
                  <p className="text-xs text-muted-foreground">
                    Showing {(meta.page - 1) * meta.pageSize + 1}–
                    {Math.min(meta.page * meta.pageSize, meta.totalCount)} of {meta.totalCount}
                  </p>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) - 1 }))}
                      disabled={(filters.page ?? 1) <= 1}
                      className="rounded border px-3 py-1 text-xs disabled:opacity-50 hover:bg-muted"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) + 1 }))}
                      disabled={(filters.page ?? 1) * meta.pageSize >= meta.totalCount}
                      className="rounded border px-3 py-1 text-xs disabled:opacity-50 hover:bg-muted"
                    >
                      Next
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
