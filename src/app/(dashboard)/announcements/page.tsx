'use client';

import { Megaphone } from 'lucide-react';
import { useAnnouncements, useAcknowledgeAnnouncement } from '@/hooks/use-announcements';
import { PageHeader } from '@/components/shared/page-header';
import { LoadingSpinner } from '@/components/shared/loading-spinner';
import { EmptyState } from '@/components/shared/empty-state';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatDate } from '@/lib/utils';

export default function AnnouncementsPage() {
  const { data, isLoading } = useAnnouncements();
  const acknowledge = useAcknowledgeAnnouncement();
  const announcements = data?.data ?? [];

  return (
    <div className="space-y-6">
      <PageHeader title="Announcements" description="Company updates and important notices." />

      {isLoading ? (
        <LoadingSpinner />
      ) : announcements.length === 0 ? (
        <EmptyState icon={Megaphone} title="No announcements" description="Nothing to see here yet." />
      ) : (
        <div className="space-y-4">
          {announcements.map((ann) => (
            <Card
              key={ann.announcementId}
              className={ann.isAcknowledged ? 'opacity-75' : ''}
            >
              <CardHeader className="pb-2 flex flex-row items-start justify-between gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    {ann.isPinned && <Badge variant="default">Pinned</Badge>}
                    {ann.priority && <Badge variant="outline">{ann.priority}</Badge>}
                    {!ann.isAcknowledged && (
                      <span className="inline-flex h-2 w-2 rounded-full bg-primary" />
                    )}
                    <span className="text-sm font-semibold">{ann.title}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">{formatDate(ann.postDate)}</p>
                </div>
                {!ann.isAcknowledged && (
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => acknowledge.mutate(ann.announcementId)}
                    disabled={acknowledge.isPending}
                  >
                    Mark read
                  </Button>
                )}
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground whitespace-pre-line">{ann.body}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
