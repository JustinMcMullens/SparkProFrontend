import { Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';

export function LoadingSpinner({ className }: { className?: string }) {
  return (
    <div className={cn('flex items-center justify-center py-16', className)}>
      <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
    </div>
  );
}

export function PageLoader() {
  return (
    <div className="flex h-full items-center justify-center">
      <Loader2 className="h-10 w-10 animate-spin text-muted-foreground" />
    </div>
  );
}
