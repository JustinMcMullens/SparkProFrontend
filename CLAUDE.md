# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SparkPro is a Next.js 14 application for role-based commission tracking, payroll management, and sales analytics. It serves multi-industry sales organizations (Solar, Pest, Roofing, Fiber) with authority-scoped dashboards and workflows.

**Tech Stack:**
- Next.js 14+ with App Router
- TypeScript 5
- Tailwind CSS + shadcn/ui components
- TanStack Query (React Query) for data fetching
- React Hook Form + Zod for form validation
- Recharts for data visualization

## Development Commands

```bash
npm install              # Install dependencies
npm run dev              # Start development server (http://localhost:3000)
npm run build            # Build for production
npm run start            # Start production server
npm run lint             # Run ESLint
npm run test             # Run tests
npm run test:e2e         # Run Playwright E2E tests
```

## Architecture & Structure

### API Integration
- **Backend Base URL:** `NEXT_PUBLIC_API_URL` (default `http://localhost:5000`)
- **Authentication:** Cookie-based sessions (ASP.NET `.AspNetCore.Session` + auth cookie). Pass `credentials: 'include'` on every fetch — no Bearer token needed.
- **Response envelope:** `{ data: T }` (single) / `{ data: T[], meta: { page, pageSize, totalCount } }` (list)
- **Errors:** RFC 7807 Problem Details `{ title, status, detail }`
- **Type Definitions:** `src/types/index.ts` — all TypeScript interfaces matching the backend schema
- **Backend endpoint docs:** `Backend-Endpoints/` folder — actual C# source files and markdown summaries

### Authentication Flow (actual)

1. `POST /login` with `{ username, password }` — sets session cookie + auth cookie
2. `GET /auth/session` — validates existing session, returns user + permissions
3. `POST /auth/logout` — clears session
4. Frontend checks session on mount via `AuthProvider` (`src/lib/providers/AuthProvider.tsx`)
5. Redirect unauthenticated users to `/login`

**There is no JWT Bearer token.** Auth is purely cookie-based. The `AuthProvider` calls `/auth/session` on mount and exposes `useAuth()` / `useCurrentUser()` hooks.

### Authority Levels (numeric, not named roles)

| Level | Role | Access |
|-------|------|--------|
| 1–2 | Rep / Setter / Closer | Own sales & commissions only |
| 3 | Team Lead | Team sales & commissions |
| 4 | Management | All sales, create batches, approve allocations |
| 5 | Admin / Executive | Full access + payroll approval/export |

Use `useCurrentUser()` for client-side checks:
```typescript
const { authorityLevel, isManagement, isAdmin } = useCurrentUser();
```

### Application Structure

```
src/app/
├── (auth)/              # Unauthenticated routes (login, etc.)
├── (dashboard)/         # Authenticated routes with sidebar layout
│   ├── dashboard/       # Authority-scoped KPI dashboard
│   ├── sales/           # Sales list and detail pages
│   ├── commissions/     # Commission tracking and summary
│   ├── team/            # L3+ team views
│   ├── payroll/         # L4+ payroll batch management
│   ├── goals/           # Goal tracking & leaderboards
│   ├── reports/         # Reporting views
│   ├── tickets/         # Support ticket system
│   ├── announcements/   # Company announcements
│   ├── admin/           # L5 configuration (rates, collaborators)
│   └── profile/         # User profile & paystubs
```

```
src/components/
├── ui/                  # shadcn/ui base components
├── layout/              # Sidebar, header, navigation
├── sales/               # Sales-specific components
├── commissions/         # Commission & allocation components
├── payroll/             # Payroll batch components
├── admin/               # Admin forms (rates, installers, etc.)
├── goals/               # Goal tracking components
├── tickets/             # Ticket system components
└── shared/              # DataTable, StatCard, etc.
```

### API Client Structure (actual)

```
src/lib/api/
├── client.ts            # fetch wrapper — credentials: 'include', JSON envelope
├── index.ts             # barrel re-exports all modules as namespaces
├── auth.ts              # /login, /auth/session, /auth/logout
├── sales.ts             # /api/sales
├── dashboard.ts         # /api/dashboard
├── profile.ts           # /api/profile, /managedUsers, /payroll, /org/users
├── team.ts              # /api/team
├── rates.ts             # /api/rates/{industry}
├── allocations.ts       # /api/allocations, /api/overrides, /api/clawbacks
├── payroll.ts           # /api/payroll/batches
├── paystubs.ts          # /api/paystubs
├── goals.ts             # /api/goals
├── announcements.ts     # /api/announcements
├── tickets.ts           # /api/tickets
└── admin.ts             # /api/admin/installers|dealers|finance-companies|partners, /api/settings/company
```

### Providers

```
src/lib/providers/
├── QueryProvider.tsx    # TanStack Query client
└── AuthProvider.tsx     # Session management + useAuth() + useCurrentUser()
```

Wrap root layout: `<QueryProvider><AuthProvider>{children}</AuthProvider></QueryProvider>`

### Custom Hooks

```
src/hooks/
├── use-sales.ts         # useSales, useSale, useCancelSale
├── use-dashboard.ts     # useDashboardStats, useLeaderboard, etc.
├── use-allocations.ts   # useAllocations, useBatchApproveAllocations
├── use-payroll.ts       # usePayrollBatches, useSubmitBatch, etc.
├── use-goals.ts         # useGoals, useMyGoalProgress, etc.
├── use-announcements.ts # useAnnouncements, useUnreadAnnouncementCount
└── use-tickets.ts       # useTickets, useCreateTicket, etc.
```

### Industries

Backend supports 4 industries as separate DB tables. Use lowercase slugs in URLs: `solar | roofing | pest | fiber`

### Key Data Models

All types in `src/types/index.ts`. Primary entities:

- **SaleListItem / SaleDetail:** Contract with customer, participants, industry-specific extension
- **UnifiedAllocation:** Cross-industry commission allocation (allocationId + industry discriminator)
- **OverrideAllocation:** Manager override commissions by level
- **Clawback:** Negative adjustments when sales are cancelled
- **PayrollBatch:** State machine: `DRAFT → SUBMITTED → APPROVED → EXPORTED → PAID`
- **CommissionRate:** Per-industry rate with 7-level specificity hierarchy
- **GoalProgress:** From `VGoalProgressSummary` DB view, scoped to user's org level
- **Announcement:** Targeted by team/office/region/company with acknowledge tracking
- **Ticket:** Support ticket with status history and comments

### Payroll Batch State Machine

```
DRAFT → SUBMITTED → APPROVED → EXPORTED → PAID
Any non-PAID state → CANCELLED
```

Actions: create → add-allocations → submit (L4) → approve (L5) → export (L5) → mark-paid (L5)

### Important URL Patterns

Some routes do NOT have the `/api/` prefix:
- `POST /login`
- `GET /auth/session`
- `POST /auth/logout`
- `GET /managedUsers/{userId}`
- `GET /payroll/{userId}` (payroll deals, not batches)
- `GET /org/users/{requesterUserId}`
- `POST /profile/{userId}/images`
- `GET /referrals/{userId}`

Everything else uses `/api/...`

### Environment Variables

Required in `.env.local`:
```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Utilities (`src/lib/utils.ts`)

- `cn(...classes)` — tailwind-merge + clsx
- `formatCurrency(amount)` — USD formatting
- `formatDate(dateStr)` — "Jan 15, 2025" format
- `formatDateShort(dateStr)` — "1/15/25" format
- `getInitials(firstName, lastName)` — "JD" for avatars
- `buildImageUrl(path, baseUrl?)` — resolves relative image paths to full URLs

## Important Patterns

**Incomplete / stub routes (return 501):**
- `GET /api/paystubs/summary`
- `GET /api/paystubs/pending`
- `PUT /api/settings/company`

**Image URLs from the backend are relative paths** like `/user_images/{userId}/profile_picture.avif`. Use `buildImageUrl()` from `src/lib/utils.ts` to make them absolute.

**Currency Formatting:** Always use `formatCurrency()` from `lib/utils.ts`.

**Date Handling:** Backend returns ISO 8601 strings and `DateOnly` strings (`"2025-01-15"`). Use `formatDate()` for display.

**Error Handling:** `ApiRequestError` from `src/lib/api/client.ts` carries `.status` and `.problem` (RFC 7807). Check `status === 401` to redirect to login.

## Key Documentation

- **Backend Endpoints:** `Backend-Endpoints/ENDPOINT_SUMMARY.md` — full route table
- **Implementation Plan:** `Backend-Endpoints/ENDPOINT_IMPLEMENTATION_PLAN.md` — backend design context
- **Type Definitions:** `src/types/index.ts` — all TypeScript interfaces
