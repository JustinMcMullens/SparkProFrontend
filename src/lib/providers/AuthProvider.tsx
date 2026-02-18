'use client';

import { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { authApi } from '@/lib/api';
import type { SessionResponse, AuthorityLevel } from '@/types';

interface AuthState {
  user: SessionResponse | null;
  isLoading: boolean;
  authorityLevel: AuthorityLevel;
  isAuthenticated: boolean;
}

interface AuthContextValue extends AuthState {
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refresh: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>({
    user: null,
    isLoading: true,
    authorityLevel: 1,
    isAuthenticated: false,
  });

  const refresh = useCallback(async () => {
    try {
      const session = await authApi.getSession();
      const level =
        (session.permissions?.[0]?.authorityLevel as AuthorityLevel) ?? 1;
      setState({
        user: session,
        isLoading: false,
        authorityLevel: level,
        isAuthenticated: true,
      });
    } catch {
      setState({ user: null, isLoading: false, authorityLevel: 1, isAuthenticated: false });
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const login = useCallback(
    async (username: string, password: string) => {
      await authApi.login({ username, password });
      await refresh();
    },
    [refresh],
  );

  const logout = useCallback(async () => {
    await authApi.logout();
    setState({ user: null, isLoading: false, authorityLevel: 1, isAuthenticated: false });
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, logout, refresh }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export function useCurrentUser() {
  const { user, authorityLevel, isAuthenticated, isLoading } = useAuth();
  return {
    user,
    authorityLevel,
    isAuthenticated,
    isLoading,
    isAdmin: authorityLevel >= 5,
    isManagement: authorityLevel >= 4,
    isTeamLead: authorityLevel >= 3,
    isSalesRep: authorityLevel >= 2,
  };
}
