# Commission Platform Web — Frontend Specification

## Overview

A Next.js 14+ application providing role-based dashboards for sales reps, managers, admins, and finance users to view sales, commissions, goals, and manage payroll.

## Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | Next.js 14+ (App Router) |
| Language | TypeScript 5 |
| Styling | Tailwind CSS + shadcn/ui |
| State Management | TanStack Query (React Query) |
| Forms | React Hook Form + Zod |
| Charts | Recharts or Tremor |
| Auth | NextAuth.js or custom JWT |
| API Client | Axios or fetch wrapper |

## Project Structure

```
commission-platform-web/
├── src/
│   ├── app/                          # Next.js App Router
│   │   ├── (auth)/                   # Auth routes (login, etc.)
│   │   │   ├── login/
│   │   │   │   └── page.tsx
│   │   │   └── layout.tsx
│   │   │
│   │   ├── (dashboard)/              # Authenticated routes
│   │   │   ├── layout.tsx            # Dashboard shell with sidebar
│   │   │   │
│   │   │   ├── dashboard/            # Role-based home
│   │   │   │   └── page.tsx
│   │   │   │
│   │   │   ├── sales/                # Sales views
│   │   │   │   ├── page.tsx          # Sales list
│   │   │   │   └── [id]/
│   │   │   │       └── page.tsx      # Sale detail
│   │   │   │
│   │   │   ├── commissions/          # Commission views
│   │   │   │   ├── page.tsx          # My commissions
│   │   │   │   ├── summary/
│   │   │   │   │   └── page.tsx      # Summary/stats
│   │   │   │   └── [id]/
│   │   │   │       └── page.tsx      # Commission detail
│   │   │   │
│   │   │   ├── team/                 # Manager views
│   │   │   │   ├── page.tsx          # Team overview
│   │   │   │   └── [userId]/
│   │   │   │       └── page.tsx      # Team member detail
│   │   │   │
│   │   │   ├── goals/                # Goals tracking
│   │   │   │   ├── page.tsx
│   │   │   │   └── [id]/
│   │   │   │       └── page.tsx
│   │   │   │
│   │   │   ├── payroll/              # Finance views
│   │   │   │   ├── page.tsx          # Batch list
│   │   │   │   ├── new/
│   │   │   │   │   └── page.tsx      # Create batch
│   │   │   │   └── [id]/
│   │   │   │       └── page.tsx      # Batch detail
│   │   │   │
│   │   │   ├── reports/              # Reports
│   │   │   │   ├── page.tsx
│   │   │   │   ├── statements/
│   │   │   │   │   └── page.tsx
│   │   │   │   └── team-performance/
│   │   │   │       └── page.tsx
│   │   │   │
│   │   │   ├── admin/                # Admin only
│   │   │   │   ├── users/
│   │   │   │   │   ├── page.tsx
│   │   │   │   │   ├── new/
│   │   │   │   │   │   └── page.tsx
│   │   │   │   │   └── [id]/
│   │   │   │   │       └── page.tsx
│   │   │   │   │
│   │   │   │   ├── offices/
│   │   │   │   │   └── page.tsx
│   │   │   │   │
│   │   │   │   ├── commission-plans/
│   │   │   │   │   ├── page.tsx
│   │   │   │   │   ├── new/
│   │   │   │   │   │   └── page.tsx
│   │   │   │   │   └── [id]/
│   │   │   │   │       ├── page.tsx  # Plan detail
│   │   │   │   │       └── rules/
│   │   │   │   │           └── page.tsx
│   │   │   │   │
│   │   │   │   └── crm-connections/
│   │   │   │       ├── page.tsx
│   │   │   │       └── [id]/
│   │   │   │           └── page.tsx
│   │   │   │
│   │   │   └── profile/
│   │   │       └── page.tsx
│   │   │
│   │   ├── api/                      # API routes (if needed)
│   │   │   └── auth/
│   │   │       └── [...nextauth]/
│   │   │           └── route.ts
│   │   │
│   │   ├── layout.tsx                # Root layout
│   │   ├── page.tsx                  # Landing/redirect
│   │   └── globals.css
│   │
│   ├── components/
│   │   ├── ui/                       # shadcn/ui components
│   │   │   ├── button.tsx
│   │   │   ├── card.tsx
│   │   │   ├── dialog.tsx
│   │   │   ├── table.tsx
│   │   │   ├── input.tsx
│   │   │   ├── select.tsx
│   │   │   └── ...
│   │   │
│   │   ├── layout/
│   │   │   ├── sidebar.tsx
│   │   │   ├── header.tsx
│   │   │   ├── nav-item.tsx
│   │   │   └── user-menu.tsx
│   │   │
│   │   ├── sales/
│   │   │   ├── sales-table.tsx
│   │   │   ├── sales-filters.tsx
│   │   │   ├── sale-detail-card.tsx
│   │   │   └── sale-commissions-list.tsx
│   │   │
│   │   ├── commissions/
│   │   │   ├── commissions-table.tsx
│   │   │   ├── commission-filters.tsx
│   │   │   ├── commission-summary-cards.tsx
│   │   │   ├── commission-chart.tsx
│   │   │   └── commission-detail-card.tsx
│   │   │
│   │   ├── payroll/
│   │   │   ├── batch-table.tsx
│   │   │   ├── batch-form.tsx
│   │   │   ├── payout-list.tsx
│   │   │   └── batch-status-badge.tsx
│   │   │
│   │   ├── admin/
│   │   │   ├── user-form.tsx
│   │   │   ├── commission-plan-form.tsx
│   │   │   ├── commission-rule-form.tsx
│   │   │   ├── rule-condition-builder.tsx
│   │   │   └── crm-connection-form.tsx
│   │   │
│   │   ├── goals/
│   │   │   ├── goal-card.tsx
│   │   │   ├── goal-progress-bar.tsx
│   │   │   └── goal-form.tsx
│   │   │
│   │   └── shared/
│   │       ├── data-table.tsx        # Reusable table with sort/filter
│   │       ├── pagination.tsx
│   │       ├── date-range-picker.tsx
│   │       ├── loading-spinner.tsx
│   │       ├── error-boundary.tsx
│   │       ├── empty-state.tsx
│   │       └── stat-card.tsx
│   │
│   ├── hooks/
│   │   ├── use-auth.ts
│   │   ├── use-current-user.ts
│   │   ├── use-sales.ts
│   │   ├── use-commissions.ts
│   │   ├── use-payroll.ts
│   │   ├── use-goals.ts
│   │   └── use-debounce.ts
│   │
│   ├── lib/
│   │   ├── api/
│   │   │   ├── client.ts             # Axios/fetch wrapper
│   │   │   ├── auth.ts
│   │   │   ├── sales.ts
│   │   │   ├── commissions.ts
│   │   │   ├── payroll.ts
│   │   │   ├── users.ts
│   │   │   └── admin.ts
│   │   │
│   │   ├── utils.ts                  # cn(), formatCurrency(), etc.
│   │   ├── constants.ts
│   │   └── validators.ts             # Zod schemas
│   │
│   ├── types/
│   │   ├── user.ts
│   │   ├── sale.ts
│   │   ├── commission.ts
│   │   ├── payroll.ts
│   │   ├── goal.ts
│   │   └── api.ts                    # API response types
│   │
│   └── providers/
│       ├── query-provider.tsx        # TanStack Query
│       ├── auth-provider.tsx
│       └── theme-provider.tsx
│
├── public/
│   └── ...
│
├── .env.local
├── .env.example
├── next.config.js
├── tailwind.config.ts
├── tsconfig.json
├── package.json
└── README.md
```

## Role-Based Navigation

### Sales Rep
```
📊 Dashboard
📋 My Sales
💰 My Commissions
  └── Summary
🎯 My Goals
👤 Profile
📄 Statements
```

### Manager (includes Sales Rep views)
```
📊 Dashboard (team metrics)
👥 My Team
  └── [Team Member]
📋 Team Sales
💰 Team Commissions
📈 Team Performance
```

### Finance
```
📊 Dashboard
💳 Payroll Batches
  ├── Create Batch
  └── [Batch Detail]
📋 All Sales (read-only)
💰 All Commissions (read-only)
📄 Reports
  ├── Commission Statements
  └── Payout History
```

### Admin (all access)
```
... all above plus:
⚙️ Admin
  ├── Users
  ├── Offices
  ├── Commission Plans
  │   └── Rules
  └── CRM Connections
```

## Page Specifications

### Dashboard (Sales Rep)

**Route:** `/dashboard`

**Components:**
- `StatCard` × 4: Sales this month, Commissions earned, Pending payout, Goal progress
- `CommissionChart`: Line chart of commissions over time
- `RecentSalesTable`: Last 5 sales
- `GoalProgressCard`: Current active goals

**API Calls:**
- `GET /commissions/summary?userId=me&period=monthly`
- `GET /sales?salesRepId=me&limit=5`
- `GET /goals?userId=me&isActive=true`

### Sales List

**Route:** `/sales`

**Components:**
- `SalesFilters`: Date range, status, sale type, amount range
- `SalesTable`: Sortable, paginated table
  - Columns: Date, Customer, Type, Amount, Status, Rep, Commission
  - Row click → `/sales/[id]`
- `Pagination`

**Permissions:**
- Sales Rep: sees own sales
- Manager: sees team sales
- Admin/Finance: sees all

### Sale Detail

**Route:** `/sales/[id]`

**Components:**
- `SaleDetailCard`: All sale info, customer, rep, office
- `SaleCommissionsList`: All commissions generated from this sale
- `SaleHistoryTimeline`: Sync events, status changes
- `RecalculateButton` (Admin only)

### Commissions List

**Route:** `/commissions`

**Components:**
- `CommissionFilters`: Date range, status, type
- `CommissionSummaryCards`: Totals by status
- `CommissionsTable`:
  - Columns: Sale Date, Sale, Amount, Type, Rate, Status, Batch
- `Pagination`

### Commission Summary

**Route:** `/commissions/summary`

**Components:**
- `PeriodSelector`: Month/Quarter/Year picker
- `SummaryCards`: Direct, Split, Override, Clawback, Total
- `CommissionTrendChart`: Bar/line chart over time
- `BreakdownByType`: Pie chart
- `TopEarningRules`: Which rules generated most

### Payroll Batches (Finance)

**Route:** `/payroll`

**Components:**
- `BatchFilters`: Status, date range
- `BatchTable`:
  - Columns: Name, Period, Status, Total, Recipients, Created
  - Actions: View, Approve, Export, Mark Paid
- `CreateBatchButton` → `/payroll/new`

### Create Payroll Batch

**Route:** `/payroll/new`

**Components:**
- `BatchForm`:
  - Name
  - Pay period start/end (date pickers)
  - Filters: Offices, Commission status
  - Preview of what will be included
- `PayoutPreviewTable`: Shows aggregated payouts before creation

### Batch Detail

**Route:** `/payroll/[id]`

**Components:**
- `BatchStatusCard`: Status, totals, dates
- `PayoutTable`:
  - Columns: Employee, Direct, Split, Override, Clawback, Total
  - Expandable row → individual commissions
- `ActionButtons`: Submit, Approve, Export, Mark Paid (based on status)
- `AuditLog`: Who did what when

### Commission Plan Editor (Admin)

**Route:** `/admin/commission-plans/[id]`

**Components:**
- `PlanHeader`: Name, dates, status
- `RulesTable`:
  - Columns: Name, Type, Conditions (preview), Rate, Priority
  - Actions: Edit, Duplicate, Delete
- `AddRuleButton` → Dialog

### Rule Editor (Admin)

**Route:** `/admin/commission-plans/[id]/rules` or Dialog

**Components:**
- `RuleForm`:
  - Name, Description
  - Rule Type selector (Percentage/Flat/Tiered)
  - `RateConfigEditor` (dynamic based on type)
  - `ConditionBuilder`: Add/remove conditions
  - Recipient Type selector
  - Split config (if split)
  - Override config (if override)
  - Priority
- `RulePreview`: Test with sample sale data

### Condition Builder

**Component:** `<RuleConditionBuilder />`

```
┌─────────────────────────────────────────────────────────────┐
│ Conditions (All must match)                                  │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│ │ sale_type ▼ │ │ equals    ▼ │ │ New         │  [🗑️]      │
│ └─────────────┘ └─────────────┘ └─────────────┘            │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│ │ rep.level ▼ │ │ is one of ▼ │ │ Senior,Lead │  [🗑️]      │
│ └─────────────┘ └─────────────┘ └─────────────┘            │
│                                                              │
│ [+ Add Condition]                                            │
└─────────────────────────────────────────────────────────────┘
```

## Key UI Components

### DataTable (Reusable)

```tsx
interface DataTableProps<T> {
  data: T[];
  columns: ColumnDef<T>[];
  isLoading?: boolean;
  pagination?: PaginationState;
  onPaginationChange?: (state: PaginationState) => void;
  sorting?: SortingState;
  onSortingChange?: (state: SortingState) => void;
  onRowClick?: (row: T) => void;
}
```

### StatCard

```tsx
interface StatCardProps {
  title: string;
  value: string | number;
  change?: number;  // % change from previous period
  changeLabel?: string;
  icon?: React.ReactNode;
  trend?: 'up' | 'down' | 'neutral';
}
```

### Commission Summary Cards

```tsx
// Shows 4 cards in a row
<div className="grid grid-cols-1 md:grid-cols-4 gap-4">
  <StatCard title="Direct" value="$12,500" trend="up" change={12} />
  <StatCard title="Splits" value="$3,200" trend="neutral" />
  <StatCard title="Overrides" value="$1,800" trend="up" change={5} />
  <StatCard title="Total" value="$17,500" trend="up" change={8} />
</div>
```

## State Management

### TanStack Query Setup

```tsx
// providers/query-provider.tsx
'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,  // 1 minute
        retry: 1,
      },
    },
  }));

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}
```

### Custom Hooks

```tsx
// hooks/use-commissions.ts
import { useQuery } from '@tanstack/react-query';
import { getCommissions, getCommissionSummary } from '@/lib/api/commissions';

export function useCommissions(filters: CommissionFilters) {
  return useQuery({
    queryKey: ['commissions', filters],
    queryFn: () => getCommissions(filters),
  });
}

export function useCommissionSummary(userId: string, period: string) {
  return useQuery({
    queryKey: ['commission-summary', userId, period],
    queryFn: () => getCommissionSummary(userId, period),
  });
}
```

## Authentication Flow

```
User visits /dashboard
    │
    ▼
Middleware checks for auth token
    │
    ├── No token → Redirect to /login
    │
    └── Has token → Verify with API
            │
            ├── Invalid → Clear, redirect to /login
            │
            └── Valid → Continue, inject user context
```

### Middleware

```tsx
// middleware.ts
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

const publicPaths = ['/login', '/forgot-password'];

export function middleware(request: NextRequest) {
  const token = request.cookies.get('auth-token');
  const isPublicPath = publicPaths.some(p => 
    request.nextUrl.pathname.startsWith(p)
  );

  if (!token && !isPublicPath) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  if (token && isPublicPath) {
    return NextResponse.redirect(new URL('/dashboard', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico).*)'],
};
```

## Type Definitions

```tsx
// types/commission.ts
export interface Commission {
  id: string;
  saleId: string;
  saleExternalId: string;
  saleDate: string;
  saleAmount: number;
  user: {
    id: string;
    name: string;
  };
  commissionType: 'direct' | 'split' | 'override';
  amount: number;
  rateApplied: number;
  status: 'pending' | 'approved' | 'paid' | 'clawed_back';
  ruleName: string;
  calculatedAt: string;
  payrollBatchId?: string;
}

export interface CommissionSummary {
  period: string;
  directTotal: number;
  splitTotal: number;
  overrideTotal: number;
  clawbackTotal: number;
  grandTotal: number;
  salesCount: number;
  commissionCount: number;
  byWeek: {
    week: string;
    amount: number;
    count: number;
  }[];
}
```

## Environment Variables

```env
# .env.example
NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=your-secret-here
```

## Development Workflow

1. Install dependencies: `npm install`
2. Copy `.env.example` to `.env.local`
3. Run dev server: `npm run dev`
4. Access at `http://localhost:3000`

## Testing Strategy

- Unit tests: Vitest for hooks and utilities
- Component tests: React Testing Library
- E2E tests: Playwright for critical flows (login, create batch, etc.)
