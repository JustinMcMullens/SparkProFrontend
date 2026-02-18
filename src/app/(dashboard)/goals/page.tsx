'use client';

import { Target } from 'lucide-react';
import { useMyGoalProgress } from '@/hooks/use-goals';
import { PageHeader } from '@/components/shared/page-header';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function GoalsPage() {
  const { data, isLoading } = useMyGoalProgress();
  const goals = data?.data ?? [];

  return (
    <div className="space-y-6">
      <PageHeader title="Goals" description="Track your progress towards your targets." />

      {isLoading ? (
        <LoadingSpinner />
      ) : goals.length === 0 ? (
        <EmptyState icon={Target} title="No active goals" description="You have no goals assigned yet." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {goals.map((goal) => {
            const pct = Math.min(Math.round(goal.progressPercent), 100);
            return (
              <Card key={goal.goalId} className="overflow-hidden">
                <CardContent className="p-5 space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="font-semibold text-sm leading-tight">
                        {goal.goalName ?? `Goal #${goal.goalId}`}
                      </p>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {formatDate(goal.startDate)} – {formatDate(goal.endDate)}
                      </p>
                    </div>
                    <Badge variant="outline" className="shrink-0 text-xs">{goal.goalLevel}</Badge>
                  </div>

                  {/* Progress bar */}
                  <div className="space-y-1">
                    <div className="flex justify-between text-xs">
                      <span className="text-muted-foreground">Progress</span>
                      <span className="font-semibold">{pct}%</span>
                    </div>
                    <div className="h-2 w-full rounded-full bg-muted overflow-hidden">
                      <div
                        className="h-full rounded-full bg-primary transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <div className="flex justify-between text-xs text-muted-foreground">
                      <span>{goal.currentValue.toLocaleString()}</span>
                      <span>{goal.targetValue.toLocaleString()}</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
