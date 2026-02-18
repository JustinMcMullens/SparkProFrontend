import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatCurrency(
  amount: number,
  options?: Intl.NumberFormatOptions,
): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    ...options,
  }).format(amount);
}

export function formatDate(dateStr: string | undefined | null): string {
  if (!dateStr) return '—';
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(dateStr));
}

export function formatDateShort(dateStr: string | undefined | null): string {
  if (!dateStr) return '—';
  return new Intl.DateTimeFormat('en-US', {
    month: 'numeric',
    day: 'numeric',
    year: '2-digit',
  }).format(new Date(dateStr));
}

export function getInitials(firstName?: string, lastName?: string): string {
  const f = (firstName ?? '').charAt(0).toUpperCase();
  const l = (lastName ?? '').charAt(0).toUpperCase();
  return `${f}${l}` || '?';
}

export function buildImageUrl(
  path: string | null | undefined,
  baseUrl?: string,
): string | null {
  if (!path) return null;
  if (path.startsWith('http')) return path;
  const base = baseUrl ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';
  return `${base}${path.startsWith('/') ? path : `/${path}`}`;
}
