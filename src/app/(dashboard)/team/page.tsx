'use client';

import { Users } from 'lucide-react';
import { useTeamMembers, useTeamPerformance } from './hooks';
import { PageHeader } from '@/components/shared/page-header';
import { StatCard } from '@/components/shared/stat-card';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { formatCurrency, getInitials, buildImageUrl } from '@/lib/utils';

export default function TeamPage() {
  const { data: membersData, isLoading: membersLoading } = useTeamMembers();
  const { data: perfData, isLoading: perfLoading } = useTeamPerformance();

  const members = membersData?.data ?? [];
  const perf = perfData?.data;

  return (
    <div className="space-y-6">
      <PageHeader title="My Team" description="Performance and overview of your direct reports." />

      {/* Team KPIs */}
      {!perfLoading && perf && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard title="Team Size" value={perf.teamSize} icon={Users} />
          <StatCard title="Total Sales" value={perf.totalSales} subtitle={formatCurrency(perf.totalValue)} />
          <StatCard title="Total Commissions" value={formatCurrency(perf.totalCommissions)} />
        </div>
      )}

      {/* Members */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Team Members</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {membersLoading ? (
            <LoadingSpinner />
          ) : members.length === 0 ? (
            <EmptyState icon={Users} title="No team members" />
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Member</th>
                  <th className="px-6 py-3 text-left font-medium text-muted-foreground">Role</th>
                  <th className="px-6 py-3 text-right font-medium text-muted-foreground">Sales</th>
                  <th className="px-6 py-3 text-right font-medium text-muted-foreground">Commissions</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {members.map((m) => (
                  <tr key={m.userId} className="hover:bg-muted/30 transition-colors">
                    <td className="px-6 py-3">
                      <div className="flex items-center gap-3">
                        <Avatar className="h-8 w-8">
                          {m.profileImageUrl && (
                            <AvatarImage src={buildImageUrl(m.profileImageUrl) ?? ''} />
                          )}
                          <AvatarFallback className="text-xs">
                            {getInitials(m.firstName, m.lastName)}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-medium">{m.firstName} {m.lastName}</p>
                          {m.teamName && <p className="text-xs text-muted-foreground">{m.teamName}</p>}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-3 text-muted-foreground">{m.title ?? '—'}</td>
                    <td className="px-6 py-3 text-right">{m.saleCount}</td>
                    <td className="px-6 py-3 text-right font-semibold">
                      {formatCurrency(m.totalCommissions)}
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
