'use client';

import { useRouter } from 'next/navigation';
import { LogOut, User, ChevronDown } from 'lucide-react';
import { useAuth, useCurrentUser } from '@/lib/providers/AuthProvider';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { getInitials, buildImageUrl } from '@/lib/utils';

export function Header() {
  const router = useRouter();
  const { logout } = useAuth();
  const { user } = useCurrentUser();

  async function handleLogout() {
    await logout();
    router.push('/login');
  }

  const initials = getInitials(user?.firstName, user?.lastName);
  const avatarUrl = buildImageUrl(user?.profileImageUrl);

  return (
    <header className="flex h-14 items-center justify-between border-b bg-background px-6">
      <div className="flex items-center gap-2">
        {/* Breadcrumb slot — children can override via portal or slot */}
      </div>

      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <Avatar className="h-8 w-8">
            {avatarUrl && <AvatarImage src={avatarUrl} alt={`${user?.firstName} ${user?.lastName}`} />}
            <AvatarFallback className="text-xs">{initials}</AvatarFallback>
          </Avatar>
          <div className="hidden sm:block">
            <p className="text-sm font-medium leading-none">
              {user?.firstName} {user?.lastName}
            </p>
          </div>
        </div>

        <Button variant="ghost" size="icon" onClick={handleLogout} title="Sign out">
          <LogOut className="h-4 w-4" />
        </Button>
      </div>
    </header>
  );
}
