import type { Metadata } from 'next';
import './globals.css';
import { QueryProvider } from '@/lib/providers/QueryProvider';
import { AuthProvider } from '@/lib/providers/AuthProvider';

export const metadata: Metadata = {
  title: 'SparkPro',
  description: 'Commission tracking and payroll management',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body>
        <QueryProvider>
          <AuthProvider>{children}</AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
