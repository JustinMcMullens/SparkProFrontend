import { cn } from '@/lib/utils';
import { Card, CardContent } from '@/components/ui/card';
import type { LucideIcon } from 'lucide-react';

interface StatCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: LucideIcon;
  iconClassName?: string;
  trend?: {
    value: number;
    label?: string;
  };
  className?: string;
}

export function StatCard({
  title,
  value,
  subtitle,
  icon: Icon,
  iconClassName,
  trend,
  className,
}: StatCardProps) {
  const isPositive = trend && trend.value >= 0;

  return (
    <Card className={cn('', className)}>
      <CardContent className="p-6">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            <p className="text-sm font-medium text-muted-foreground">{title}</p>
            <p className="text-2xl font-bold tracking-tight">{value}</p>
            {subtitle && <p className="text-xs text-muted-foreground">{subtitle}</p>}
            {trend && (
              <p
                className={cn(
                  'text-xs font-medium',
                  isPositive ? 'text-emerald-600' : 'text-destructive',
                )}
              >
                {isPositive ? '↑' : '↓'} {Math.abs(trend.value)}%
                {trend.label && <span className="ml-1 text-muted-foreground">{trend.label}</span>}
              </p>
            )}
          </div>
          {Icon && (
            <div
              className={cn(
                'flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10',
                iconClassName,
              )}
            >
              <Icon className="h-5 w-5 text-primary" />
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
