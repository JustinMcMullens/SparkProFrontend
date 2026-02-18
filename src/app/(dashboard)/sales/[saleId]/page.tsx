'use client';

import { useParams, useRouter } from 'next/navigation';
import { ArrowLeft, Ban } from 'lucide-react';
import { useSale, useCancelSale } from '@/hooks/use-sales';
import { useCurrentUser } from '@/lib/providers/AuthProvider';
import { PageHeader } from '@/components/shared/page-header';
import { SaleStatusBadge } from '@/components/shared/sale-status-badge';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { formatCurrency, formatDate } from '@/lib/utils';

export default function SaleDetailPage() {
  const { saleId } = useParams<{ saleId: string }>();
  const router = useRouter();
  const { isManagement } = useCurrentUser();
  const { data, isLoading } = useSale(Number(saleId));
  const cancelMutation = useCancelSale();

  if (isLoading) return <LoadingSpinner />;

  const sale = data?.data;
  if (!sale) return <p className="text-sm text-muted-foreground">Sale not found.</p>;

  async function handleCancel() {
    const reason = prompt('Enter cancellation reason:');
    if (!reason) return;
    await cancelMutation.mutateAsync({ saleId: sale!.saleId, reason });
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => router.back()}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <PageHeader
          title={`${sale.customer.firstName} ${sale.customer.lastName}`}
          description={`Sale #${sale.saleId} · ${sale.projectType.projectTypeName}`}
          className="mb-0"
        >
          <SaleStatusBadge status={sale.saleStatus} />
          {isManagement && sale.saleStatus !== 'CANCELLED' && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleCancel}
              disabled={cancelMutation.isPending}
              className="text-destructive hover:text-destructive"
            >
              <Ban className="mr-2 h-4 w-4" />
              Cancel Sale
            </Button>
          )}
        </PageHeader>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Sale Info */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Sale Details</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <InfoItem label="Sale Date" value={formatDate(sale.saleDate)} />
            <InfoItem label="Contract Amount" value={formatCurrency(sale.contractAmount)} />
            <InfoItem
              label="Customer"
              value={`${sale.customer.firstName} ${sale.customer.lastName}`}
            />
            {sale.customer.city && (
              <InfoItem label="Location" value={`${sale.customer.city}, ${sale.customer.stateCode}`} />
            )}
            {sale.customer.email && <InfoItem label="Email" value={sale.customer.email} />}
            {sale.customer.phone && <InfoItem label="Phone" value={sale.customer.phone} />}
            {sale.cancellationReason && (
              <InfoItem
                label="Cancellation Reason"
                value={sale.cancellationReason}
                className="sm:col-span-2 text-destructive"
              />
            )}
          </CardContent>
        </Card>

        {/* Participants */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Participants</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {sale.participants.map((p) => (
              <div key={p.userId} className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium">
                    {p.firstName} {p.lastName}
                  </p>
                  <p className="text-xs text-muted-foreground">{p.role}</p>
                </div>
                {p.splitPercent != null && (
                  <Badge variant="outline">{p.splitPercent}%</Badge>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      </div>

      {/* Allocations */}
      {sale.allocations.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Commission Allocations</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Industry</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Milestone</th>
                  <th className="px-6 py-3 text-right font-medium text-muted-foreground">Amount</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {sale.allocations.map((a) => (
                  <tr key={`${a.industry}-${a.allocationId}`}>
                    <td className="px-6 py-3">
                      <Badge variant="outline">{a.industry}</Badge>
                    </td>
                    <td className="px-6 py-3 text-muted-foreground">MP{a.milestoneNumber}</td>
                    <td className="px-6 py-3 text-right font-semibold">
                      {formatCurrency(a.allocatedAmount)}
                    </td>
                    <td className="px-6 py-3">
                      {a.isPaid ? (
                        <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200">Paid</Badge>
                      ) : a.isApproved ? (
                        <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200">Approved</Badge>
                      ) : (
                        <Badge variant="outline" className="bg-amber-50 text-amber-700 border-amber-200">Pending</Badge>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function InfoItem({
  label,
  value,
  className,
}: {
  label: string;
  value: string;
  className?: string;
}) {
  return (
    <div>
      <p className="text-xs font-medium text-muted-foreground">{label}</p>
      <p className={`mt-0.5 text-sm ${className ?? ''}`}>{value}</p>
    </div>
  );
}
