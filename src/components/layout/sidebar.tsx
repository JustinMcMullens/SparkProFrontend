'use client';

import Link from 'next/link';
import {
  LayoutDashboard,
  TrendingUp,
  DollarSign,
  Users,
  CreditCard,
  Target,
  Megaphone,
  Ticket,
  Settings,
  Zap,
  ChevronRight,
} from 'lucide-react';
import { NavItem } from './nav-item';
import { useCurrentUser } from '@/lib/providers/AuthProvider';
import { useUnreadAnnouncementCount } from '@/hooks/use-announcements';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';

export function Sidebar() {
  const { isManagement, isTeamLead, isAdmin } = useCurrentUser();
  const { data: unreadData } = useUnreadAnnouncementCount();
  const unreadCount = unreadData?.data?.unreadCount ?? 0;

  return (
    <aside className="flex h-full w-64 flex-col bg-sidebar">
      {/* Logo */}
      <div className="flex h-14 items-center gap-2 border-b border-sidebar-border px-4">
        <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary">
          <Zap className="h-4 w-4 text-primary-foreground" />
        </div>
        <span className="text-sm font-semibold text-sidebar-foreground">SparkPro</span>
      </div>

      <ScrollArea className="flex-1 px-3 py-4">
        <nav className="space-y-1">
          <NavItem href="/dashboard" icon={LayoutDashboard} label="Dashboard" />
          <NavItem href="/sales" icon={TrendingUp} label="Sales" />
          <NavItem href="/commissions" icon={DollarSign} label="Commissions" />

          {isTeamLead && (
            <>
              <Separator className="my-2 bg-sidebar-border" />
              <p className="px-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-sidebar-foreground/40">
                Team
              </p>
              <NavItem href="/team" icon={Users} label="My Team" />
            </>
          )}

          {isManagement && (
            <NavItem href="/payroll" icon={CreditCard} label="Payroll" />
          )}

          <Separator className="my-2 bg-sidebar-border" />
          <p className="px-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-sidebar-foreground/40">
            Personal
          </p>
          <NavItem href="/goals" icon={Target} label="Goals" />
          <NavItem
            href="/announcements"
            icon={Megaphone}
            label="Announcements"
            badge={unreadCount}
          />
          <NavItem href="/tickets" icon={Ticket} label="Tickets" />

          {isAdmin && (
            <>
              <Separator className="my-2 bg-sidebar-border" />
              <p className="px-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-sidebar-foreground/40">
                Admin
              </p>
              <NavItem href="/admin/rates" icon={Settings} label="Commission Rates" />
              <NavItem href="/admin/collaborators" icon={ChevronRight} label="Collaborators" />
            </>
          )}
        </nav>
      </ScrollArea>
    </aside>
  );
}
