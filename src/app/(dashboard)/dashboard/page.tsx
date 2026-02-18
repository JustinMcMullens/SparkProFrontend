'use client';

import Link from 'next/link';
import {
  TrendingUp,
  DollarSign,
  Clock,
  CheckCircle,
  Users,
  ArrowRight,
} from 'lucide-react';
import { useDashboardStats, useRecentActivity, useLeaderboard } from '@/hooks/use-dashboard';
import { useCurrentUser } from '@/lib/providers/AuthProvider';
import { StatCard } from '@/components/shared/stat-card';
import { PageHeader } from '@/components/shared/page-header';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { SaleStatusBadge } from '@/components/shared/sale-status-badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { formatCurrency, formatDateShort, getInitials, buildImageUrl } from '@/lib/utils';

export default function DashboardPage() {
  const { user, isManagement } = useCurrentUser();
  const { data: statsData, isLoading: statsLoading } = useDashboardStats();
  const { data: activityData, isLoading: activityLoading } = useRecentActivity(8);
  const { data: leaderboardData } = useLeaderboard({ limit: 5 });

  const stats = statsData?.data;
  const activity = activityData?.data ?? [];
  const leaderboard = leaderboardData?.data ?? [];

  const greeting = getGreeting();
  const firstName = user?.firstName ?? 'there';

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${greeting}, ${firstName} 👋`}
        description="Here's what's happening with your sales today."
      />

      {/* KPI Stats */}
      {statsLoading ? (
        <LoadingSpinner />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            title="Total Sales"
            value={stats?.sales.totalCount ?? 0}
            subtitle={formatCurrency(stats?.sales.totalValue ?? 0)}
            icon={TrendingUp}
          />
          <StatCard
            title="Pending Commission"
            value={formatCurrency(stats?.commissions.pending ?? 0)}
            subtitle="Awaiting approval"
            icon={Clock}
            iconClassName="bg-amber-100"
          />
          <StatCard
            title="Approved Commission"
            value={formatCurrency(stats?.commissions.approved ?? 0)}
            subtitle="Ready for payroll"
            icon={CheckCircle}
            iconClassName="bg-emerald-100"
          />
          <StatCard
            title="Paid Commission"
            value={formatCurrency(stats?.commissions.paid ?? 0)}
            subtitle="All time"
            icon={DollarSign}
            iconClassName="bg-blue-100"
          />
          {isManagement && stats?.approvalQueueCount != null && stats.approvalQueueCount > 0 && (
            <Card className="sm:col-span-2 lg:col-span-4 border-amber-200 bg-amber-50">
              <CardContent className="flex items-center justify-between p-4">
                <div className="flex items-center gap-3">
                  <Clock className="h-5 w-5 text-amber-600" />
                  <div>
                    <p className="text-sm font-semibold text-amber-900">
                      {stats.approvalQueueCount} allocation{stats.approvalQueueCount !== 1 ? 's' : ''} awaiting approval
                    </p>
                    <p className="text-xs text-amber-700">Review and approve pending commissions</p>
                  </div>
                </div>
                <Link
                  href="/commissions?tab=pending"
                  className="flex items-center gap-1 text-sm font-medium text-amber-700 hover:text-amber-900"
                >
                  Review <ArrowRight className="h-4 w-4" />
                </Link>
              </CardContent>
            </Card>
          )}
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Recent Activity */}
        <Card className="lg:col-span-2">
          <CardHeader className="flex flex-row items-center justify-between pb-3">
            <CardTitle className="text-base">Recent Sales</CardTitle>
            <Link href="/sales" className="text-xs text-primary hover:underline">
              View all
            </Link>
          </CardHeader>
          <CardContent className="p-0">
            {activityLoading ? (
              <LoadingSpinner className="py-8" />
            ) : activity.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">No recent sales</p>
            ) : (
              <div className="divide-y">
                {activity.map((sale) => (
                  <Link
                    key={sale.saleId}
                    href={`/sales/${sale.saleId}`}
                    className="flex items-center justify-between px-6 py-3 hover:bg-muted/50 transition-colors"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">
                        {sale.customer.firstName} {sale.customer.lastName}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {sale.projectType} · {formatDateShort(sale.saleDate)}
                      </p>
                    </div>
                    <div className="ml-4 flex items-center gap-3">
                      <span className="text-sm font-semibold">
                        {formatCurrency(sale.contractAmount)}
                      </span>
                      <SaleStatusBadge status={sale.saleStatus} />
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Leaderboard */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-3">
            <CardTitle className="text-base">
              <span className="flex items-center gap-2">
                <Users className="h-4 w-4" /> Top Earners
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {leaderboard.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">No data</p>
            ) : (
              <div className="divide-y">
                {leaderboard.map((entry, i) => (
                  <div key={entry.userId} className="flex items-center gap-3 px-6 py-3">
                    <span
                      className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold ${
                        i === 0
                          ? 'bg-amber-100 text-amber-700'
                          : i === 1
                            ? 'bg-slate-100 text-slate-600'
                            : i === 2
                              ? 'bg-orange-100 text-orange-700'
                              : 'bg-muted text-muted-foreground'
                      }`}
                    >
                      {i + 1}
                    </span>
                    <Avatar className="h-7 w-7">
                      {entry.profileImageUrl && (
                        <AvatarImage src={buildImageUrl(entry.profileImageUrl) ?? ''} />
                      )}
                      <AvatarFallback className="text-[10px]">
                        {entry.name.split(' ').map((n) => n[0]).join('').slice(0, 2)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{entry.name}</p>
                    </div>
                    <span className="text-sm font-semibold text-emerald-600">
                      {formatCurrency(entry.totalCommissions)}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 17) return 'Good afternoon';
  return 'Good evening';
}
