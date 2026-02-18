# Commission Platform Web -- Implementation Plan

*Created: 2026-02-05*
*Owner: Engineering Lead*

---

## Overview

This plan breaks the frontend implementation into 6 phases, ordered by dependency and priority.
Each phase contains discrete tasks with acceptance criteria. Tasks within a phase can often run
in parallel, but phases should be completed roughly in order since later phases depend on earlier
infrastructure.

**Estimated scope:** ~89 files to create across configuration, pages, components, hooks, API
clients, providers, and middleware.

---

## Phase 0: Project Scaffolding and Configuration

**Goal:** A running Next.js application with all tooling configured.
**Priority:** P0 -- Nothing else can start without this.
**Depends on:** Nothing.

---

### Task 0.1: Initialize Next.js Project

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** None

#### Context
The project directory has documentation and types but no Next.js application. We need to scaffold
the full project with all required dependencies.

#### Requirements
1. Initialize Next.js 14+ with App Router and TypeScript
2. Install all dependencies from the tech stack:
   - `next`, `react`, `react-dom`
   - `tailwindcss`, `postcss`, `autoprefixer`
   - `@tanstack/react-query`
   - `react-hook-form`, `@hookform/resolvers`, `zod`
   - `recharts`
   - `axios`
   - `lucide-react`
   - `clsx`, `tailwind-merge` (for `cn()` utility)
   - `date-fns` (for date formatting)
   - Dev dependencies: `@types/react`, `@types/node`, `typescript`, `eslint`, `eslint-config-next`
3. Configure `tsconfig.json` with `@/` path alias pointing to `src/`
4. Configure `tailwind.config.ts` with content paths for `src/`
5. Configure `next.config.js` (or `next.config.mjs`)
6. Create `postcss.config.js` (or `.mjs`)
7. Create `src/app/globals.css` with Tailwind directives
8. Create `src/app/layout.tsx` (root layout with html/body, font, metadata)
9. Create `src/app/page.tsx` (redirect to `/dashboard` or `/login`)
10. Create `.env.example` with documented variables
11. Verify the existing `src/types/index.ts` is preserved and importable via `@/types`

#### Acceptance Criteria
- [ ] `npm run dev` starts without errors on `http://localhost:3000`
- [ ] `npm run build` completes successfully
- [ ] `npm run lint` passes
- [ ] TypeScript compiles with no errors
- [ ] `@/types` import path resolves correctly
- [ ] Tailwind CSS classes render correctly
- [ ] `.env.example` documents `NEXT_PUBLIC_API_URL`, `NEXTAUTH_URL`, `NEXTAUTH_SECRET`

#### Files to Create/Modify
- `package.json` -- create with all dependencies
- `tsconfig.json` -- create with path aliases
- `tailwind.config.ts` -- create with content paths
- `next.config.mjs` -- create
- `postcss.config.mjs` -- create
- `.env.example` -- create
- `src/app/globals.css` -- create with Tailwind directives and CSS variables
- `src/app/layout.tsx` -- create root layout
- `src/app/page.tsx` -- create landing redirect

---

### Task 0.2: Initialize shadcn/ui

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 0.1

#### Context
shadcn/ui provides the base component library. It must be initialized before any UI components
can be built.

#### Requirements
1. Run `npx shadcn-ui@latest init` (or configure manually)
2. Set up `components.json` for shadcn/ui configuration
3. Install foundational shadcn/ui components:
   - `button`, `card`, `input`, `label`, `select`, `dialog`, `table`
   - `dropdown-menu`, `avatar`, `badge`, `separator`, `sheet`
   - `tabs`, `toast` (or `sonner`), `tooltip`, `popover`, `calendar`
   - `form` (for React Hook Form integration)
   - `skeleton` (for loading states)
4. Create `src/lib/utils.ts` with `cn()` utility function

#### Acceptance Criteria
- [ ] `components.json` exists and is properly configured
- [ ] All listed shadcn/ui components exist in `src/components/ui/`
- [ ] `cn()` utility function works correctly
- [ ] Components render with proper Tailwind styling

#### Files to Create/Modify
- `components.json` -- shadcn/ui config
- `src/lib/utils.ts` -- `cn()` and utility functions
- `src/components/ui/*.tsx` -- shadcn/ui components

---

### Task 0.3: Core Utilities and Constants

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 0.1

#### Context
Utility functions and constants are used across the entire application. These must exist before
building any feature components.

#### Requirements
1. Add to `src/lib/utils.ts`:
   - `formatCurrency(amount: number): string` -- format as USD
   - `formatDate(date: string): string` -- human-readable date
   - `formatDateTime(date: string): string` -- human-readable date+time
   - `formatPercent(value: number): string` -- percentage display
   - `getInitials(name: string): string` -- for avatar fallbacks
2. Create `src/lib/constants.ts`:
   - `SALE_STATUSES` with labels and colors
   - `BATCH_STATUSES` with labels and colors
   - `ROLES` (SalesRep, Manager, Finance, Admin)
   - `MILESTONE_LABELS` (MP1, MP2)
   - Pagination defaults (`DEFAULT_PAGE_SIZE = 20`)
3. Create `src/lib/validators.ts`:
   - Login form schema (email + password)
   - Batch create form schema
   - Rate form schema
   - Common validation patterns (date ranges, positive numbers)

#### Acceptance Criteria
- [ ] All utility functions handle edge cases (null, undefined, zero)
- [ ] Constants are typed and exported
- [ ] Zod schemas validate correctly and provide user-friendly error messages

#### Files to Create/Modify
- `src/lib/utils.ts` -- extend with formatting functions
- `src/lib/constants.ts` -- create
- `src/lib/validators.ts` -- create

---

## Phase 1: Authentication and Layout Shell

**Goal:** Users can log in and see a role-based sidebar navigation shell.
**Priority:** P0 -- All feature pages live inside the authenticated layout.
**Depends on:** Phase 0.

---

### Task 1.1: API Client

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Phase 0

#### Context
All data fetching goes through a centralized API client that handles auth tokens, base URL,
and error formatting. This is the foundation for every API call in the application.

#### Requirements
1. Create `src/lib/api/client.ts`:
   - Axios instance with `NEXT_PUBLIC_API_URL` as base URL
   - Request interceptor: attach `Authorization: Bearer <token>` from cookie or local storage
   - Response interceptor: handle 401 (redirect to login), transform errors to `ApiError` type
   - Support for request cancellation
2. Create `src/lib/api/auth.ts`:
   - `login(credentials: LoginRequest): Promise<LoginResponse>` -- POST to auth endpoint
   - `logout(): void` -- clear token, redirect to login
   - `getCurrentUser(): Promise<User>` -- fetch current user profile
   - Token storage helpers (get/set/clear from cookie)

#### Acceptance Criteria
- [ ] API client sends `Authorization` header when token exists
- [ ] 401 responses trigger redirect to `/login`
- [ ] API errors are transformed to `ApiError` shape
- [ ] Login function stores token and returns user data
- [ ] Logout clears token and redirects

#### Files to Create/Modify
- `src/lib/api/client.ts` -- create
- `src/lib/api/auth.ts` -- create

---

### Task 1.2: Auth Provider and Hooks

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 1.1

#### Context
The auth provider manages the current user session and exposes it to all components. The
`useAuth` and `useCurrentUser` hooks are used throughout the app for role checks and user
display.

#### Requirements
1. Create `src/providers/auth-provider.tsx`:
   - React context storing current user, login/logout functions, loading state
   - On mount: check for existing token, fetch user if valid
   - Expose `AuthContext` with `user`, `isAuthenticated`, `isLoading`, `login`, `logout`, `role`
2. Create `src/hooks/use-auth.ts`:
   - Hook that consumes `AuthContext`
   - Throws if used outside provider
3. Create `src/hooks/use-current-user.ts`:
   - Returns current user with role information
   - Convenience helpers: `isAdmin`, `isManager`, `isFinance`, `isSalesRep`

#### Acceptance Criteria
- [ ] `useAuth()` returns user, login, logout, isAuthenticated, isLoading
- [ ] `useCurrentUser()` provides role-checking helpers
- [ ] Provider handles token expiration gracefully
- [ ] Loading state is shown while verifying token on initial load

#### Files to Create/Modify
- `src/providers/auth-provider.tsx` -- create
- `src/hooks/use-auth.ts` -- create
- `src/hooks/use-current-user.ts` -- create

---

### Task 1.3: Query Provider

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Phase 0

#### Context
TanStack Query must wrap the entire application for data fetching to work.

#### Requirements
1. Create `src/providers/query-provider.tsx`:
   - `'use client'` component
   - Initialize `QueryClient` with sensible defaults:
     - `staleTime: 60 * 1000` (1 minute)
     - `retry: 1`
     - `refetchOnWindowFocus: false` (for development comfort)
   - Wrap children in `QueryClientProvider`
2. Optionally include React Query DevTools in development

#### Acceptance Criteria
- [ ] Provider is a client component
- [ ] QueryClient has sensible default options
- [ ] React Query DevTools available in development builds

#### Files to Create/Modify
- `src/providers/query-provider.tsx` -- create

---

### Task 1.4: Theme Provider

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Phase 0

#### Context
Support for light/dark mode theming.

#### Requirements
1. Create `src/providers/theme-provider.tsx`:
   - Use `next-themes` package (install if needed)
   - Support `light`, `dark`, and `system` themes
   - Default to `system`

#### Acceptance Criteria
- [ ] Theme toggles between light and dark
- [ ] Theme preference persists across page reloads
- [ ] shadcn/ui components respond to theme changes

#### Files to Create/Modify
- `src/providers/theme-provider.tsx` -- create

---

### Task 1.5: Middleware (Auth Redirects)

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 1.1

#### Context
Next.js middleware intercepts requests before they reach pages. It handles redirecting
unauthenticated users to login and authenticated users away from public pages.

#### Requirements
1. Create `src/middleware.ts` (at the project root or `src/`):
   - Check for `auth-token` cookie
   - Public paths: `/login`, `/forgot-password`
   - No token + protected path -> redirect to `/login`
   - Has token + public path -> redirect to `/dashboard`
   - Configure matcher to exclude `api`, `_next/static`, `_next/image`, `favicon.ico`

#### Acceptance Criteria
- [ ] Unauthenticated users are redirected to `/login` from any protected route
- [ ] Authenticated users are redirected to `/dashboard` from `/login`
- [ ] Static assets and API routes are not intercepted

#### Files to Create/Modify
- `src/middleware.ts` -- create

---

### Task 1.6: Login Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 1.1, 1.2, 1.5

#### Context
The login page is the entry point for all users. It must be clean, functional, and handle errors
gracefully.

#### Requirements
1. Create `src/app/(auth)/layout.tsx`:
   - Minimal layout (centered card, no sidebar)
   - Company branding/logo space
2. Create `src/app/(auth)/login/page.tsx`:
   - Email + password form using React Hook Form + Zod
   - Submit calls `login()` from auth context
   - Show validation errors inline
   - Show API errors (invalid credentials) as toast or alert
   - Redirect to `/dashboard` on success
   - Loading state on submit button

#### Acceptance Criteria
- [ ] Form validates email format and required password
- [ ] Successful login redirects to `/dashboard`
- [ ] Invalid credentials show error message
- [ ] Submit button shows loading state
- [ ] Page is responsive

#### Files to Create/Modify
- `src/app/(auth)/layout.tsx` -- create
- `src/app/(auth)/login/page.tsx` -- create

---

### Task 1.7: Dashboard Layout Shell

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 1.2, 1.3, 1.4

#### Context
The dashboard layout wraps all authenticated pages. It includes the sidebar navigation, header
with user menu, and main content area. Navigation items are filtered by user role.

#### Requirements
1. Create `src/app/(dashboard)/layout.tsx`:
   - Wrap children with QueryProvider, AuthProvider, ThemeProvider
   - Render sidebar + header + main content area
   - Responsive: sidebar collapses on mobile (use Sheet or toggle)
2. Create `src/components/layout/sidebar.tsx`:
   - Role-based navigation items per FRONTEND_STRUCTURE.md:
     - Sales Rep: Dashboard, My Sales, My Commissions (with Summary sub-item), My Goals, Profile, Statements
     - Manager: adds Team, Team Sales, Team Commissions, Team Performance
     - Finance: adds Payroll Batches, All Sales, All Commissions, Reports
     - Admin: adds Admin section (Users, Offices, Commission Plans, CRM Connections)
   - Active item highlighting based on current route
   - Collapsible on mobile
3. Create `src/components/layout/header.tsx`:
   - Page title (dynamic based on route)
   - User menu on right side
   - Mobile menu toggle button
4. Create `src/components/layout/nav-item.tsx`:
   - Reusable navigation link component
   - Support for nested items (sub-navigation)
   - Active state styling
5. Create `src/components/layout/user-menu.tsx`:
   - Show user name, avatar/initials
   - Dropdown with: Profile, Settings, Logout

#### Acceptance Criteria
- [ ] Sidebar shows correct items for each role
- [ ] Active route is highlighted in sidebar
- [ ] Mobile sidebar collapses/expands
- [ ] User menu shows user name and has working logout
- [ ] Layout is responsive across breakpoints
- [ ] Main content area scrolls independently from sidebar

#### Files to Create/Modify
- `src/app/(dashboard)/layout.tsx` -- create
- `src/components/layout/sidebar.tsx` -- create
- `src/components/layout/header.tsx` -- create
- `src/components/layout/nav-item.tsx` -- create
- `src/components/layout/user-menu.tsx` -- create

---

### Task 1.8: Shared UI Components

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 0.2

#### Context
These reusable components are used on almost every page. They must be built before any feature
pages.

#### Requirements
1. `src/components/shared/loading-spinner.tsx` -- Full-page and inline loading indicators
2. `src/components/shared/error-boundary.tsx` -- React error boundary with retry action
3. `src/components/shared/empty-state.tsx` -- "No data" display with optional action button
4. `src/components/shared/stat-card.tsx` -- Metric card matching `StatCardProps` from spec
5. `src/components/shared/pagination.tsx` -- Page navigation for tables
6. `src/components/shared/data-table.tsx` -- Reusable sortable/paginated table using `@tanstack/react-table`
   - Must match `DataTableProps<T>` interface from spec
   - Column definitions, sorting, pagination, row click
   - Loading skeleton state
   - Install `@tanstack/react-table` if not already installed
7. `src/components/shared/date-range-picker.tsx` -- Date range selection using calendar popover

#### Acceptance Criteria
- [ ] `DataTable` supports generic column definitions, sorting, and pagination
- [ ] `StatCard` displays title, value, trend indicator, and change percentage
- [ ] `LoadingSpinner` has both full-page and inline variants
- [ ] `ErrorBoundary` catches errors and shows retry option
- [ ] `EmptyState` accepts custom message and optional action
- [ ] `Pagination` controls page navigation with page number display
- [ ] `DateRangePicker` allows selecting start and end dates
- [ ] All components are properly typed with TypeScript

#### Files to Create/Modify
- `src/components/shared/loading-spinner.tsx` -- create
- `src/components/shared/error-boundary.tsx` -- create
- `src/components/shared/empty-state.tsx` -- create
- `src/components/shared/stat-card.tsx` -- create
- `src/components/shared/pagination.tsx` -- create
- `src/components/shared/data-table.tsx` -- create
- `src/components/shared/date-range-picker.tsx` -- create

---

## Phase 2: Sales Module

**Goal:** Sales list and detail pages with filtering, sorting, and pagination.
**Priority:** P0 -- Sales are the core data entity that drives commissions.
**Depends on:** Phase 1.

---

### Task 2.1: Sales API Client and Hooks

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 1.1

#### Context
API functions and TanStack Query hooks for sales data fetching.

#### Requirements
1. Create `src/lib/api/sales.ts`:
   - `getSales(filters: SaleFilters): Promise<PaginatedResponse<Sale>>`
   - `getSaleById(id: number): Promise<ApiResponse<SaleDetail>>`
   - `recalculateSale(id: number): Promise<ApiResponse<CalculationResult>>`
   - `cancelSale(id: number, reason: string): Promise<void>`
2. Create `src/hooks/use-sales.ts`:
   - `useSales(filters: SaleFilters)` -- paginated list query
   - `useSale(id: number)` -- single sale detail query
   - `useRecalculateSale()` -- mutation with cache invalidation
   - `useCancelSale()` -- mutation with cache invalidation

#### Acceptance Criteria
- [ ] All API functions correctly map to endpoints in API_CONTRACT.md
- [ ] Hooks use appropriate query keys for cache management
- [ ] Mutations invalidate relevant queries on success
- [ ] Error handling follows `ApiError` pattern

#### Files to Create/Modify
- `src/lib/api/sales.ts` -- create
- `src/hooks/use-sales.ts` -- create

---

### Task 2.2: Sales List Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 1.7, 1.8, 2.1

#### Context
The main sales view. Uses the shared DataTable with sales-specific filters and columns.
Permissions: Sales Rep sees own sales, Manager sees team, Admin/Finance sees all.

#### Requirements
1. Create `src/components/sales/sales-filters.tsx`:
   - Date range picker (start/end date)
   - Status dropdown (PENDING, APPROVED, COMPLETED, CANCELLED)
   - Project type filter
   - Search input (customer name)
   - Clear filters button
2. Create `src/components/sales/sales-table.tsx`:
   - Columns: Date, Customer, Type, Amount, Status, Participants, Commission Total
   - Sortable by date, amount
   - Row click navigates to `/sales/[id]`
   - Status shown as colored badge
   - Currency amounts formatted
3. Create `src/app/(dashboard)/sales/page.tsx`:
   - Compose SalesFilters + SalesTable + Pagination
   - Filter state managed with URL search params (for shareable URLs)
   - Loading, error, and empty states

#### Acceptance Criteria
- [ ] Table displays sales data with all specified columns
- [ ] Filters update the table data
- [ ] Pagination works correctly
- [ ] Row click navigates to sale detail
- [ ] URL reflects current filter/page state
- [ ] Loading skeleton shown while fetching
- [ ] Empty state shown when no results match filters
- [ ] Currency amounts are formatted

#### Files to Create/Modify
- `src/components/sales/sales-filters.tsx` -- create
- `src/components/sales/sales-table.tsx` -- create
- `src/app/(dashboard)/sales/page.tsx` -- create

---

### Task 2.3: Sale Detail Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 2.1, Task 1.8

#### Context
Shows complete sale information including customer details, participants, commission allocations,
overrides, and clawbacks.

#### Requirements
1. Create `src/components/sales/sale-detail-card.tsx`:
   - Display sale info: date, status, contract amount, project type
   - Customer section: name, email, address
   - Participants list with roles
   - Status badge with color coding
2. Create `src/components/sales/sale-commissions-list.tsx`:
   - Table of allocations for this sale (from `SaleDetail.allocations`)
   - Show: user, type, milestone, amount, approved/paid status
   - Separate section for overrides (from `SaleDetail.overrides`)
   - Separate section for clawbacks (from `SaleDetail.clawbacks`)
3. Create `src/app/(dashboard)/sales/[id]/page.tsx`:
   - Compose SaleDetailCard + SaleCommissionsList
   - Recalculate button (Admin only) -- calls `recalculateSale` mutation
   - Cancel button (Admin only) -- opens confirmation dialog with reason input
   - Back navigation to sales list
   - Loading and error states

#### Acceptance Criteria
- [ ] All sale fields are displayed correctly
- [ ] Allocations, overrides, and clawbacks are listed
- [ ] Admin-only actions (recalculate, cancel) are role-gated
- [ ] Recalculate triggers API call and refreshes data
- [ ] Cancel opens dialog, requires reason, and triggers API call
- [ ] Page handles loading and error states

#### Files to Create/Modify
- `src/components/sales/sale-detail-card.tsx` -- create
- `src/components/sales/sale-commissions-list.tsx` -- create
- `src/app/(dashboard)/sales/[id]/page.tsx` -- create

---

## Phase 3: Commissions Module

**Goal:** Commission (allocation) views with summary, filtering, and charting.
**Priority:** P0 -- Core business logic visibility.
**Depends on:** Phase 1, Phase 2 (shared components).

**NOTE ON TERMINOLOGY:** The FRONTEND_STRUCTURE.md spec uses "commissions" terminology but the
actual API uses "allocations", "overrides", and "clawbacks" as separate endpoints. The UI should
use "Commissions" as the user-facing label but the data layer must target the correct API endpoints.

---

### Task 3.1: Commissions API Client and Hooks

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 1.1

#### Context
API functions for allocations, overrides, clawbacks, and their summaries. Note that the backend
uses separate endpoints for each type rather than a unified `/commissions` endpoint.

#### Requirements
1. Create `src/lib/api/commissions.ts`:
   - `getAllocations(filters: AllocationFilters): Promise<PaginatedResponse<CommissionAllocation>>`
   - `getAllocationSummary(filters: AllocationFilters): Promise<ApiResponse<AllocationSummary>>`
   - `approveAllocation(id: number): Promise<void>`
   - `batchApproveAllocations(ids: number[]): Promise<void>`
   - `getOverrides(filters): Promise<PaginatedResponse<OverrideAllocation>>`
   - `getOverrideSummary(filters): Promise<ApiResponse<...>>`
   - `getClawbacks(filters): Promise<PaginatedResponse<Clawback>>`
   - `processClawback(id: number): Promise<void>`
   - `batchProcessClawbacks(ids: number[]): Promise<void>`
2. Create `src/hooks/use-commissions.ts`:
   - `useAllocations(filters)` -- paginated list
   - `useAllocationSummary(filters)` -- summary data
   - `useApproveAllocation()` -- mutation
   - `useBatchApproveAllocations()` -- mutation
   - `useOverrides(filters)` -- paginated list
   - `useClawbacks(filters)` -- paginated list

#### Acceptance Criteria
- [ ] All API functions correctly target `/allocations`, `/overrides`, `/clawbacks` endpoints
- [ ] Hooks follow TanStack Query patterns with proper query keys
- [ ] Mutations invalidate relevant queries on success

#### Files to Create/Modify
- `src/lib/api/commissions.ts` -- create
- `src/hooks/use-commissions.ts` -- create

---

### Task 3.2: Commissions List Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 1.7, 1.8, 3.1

#### Context
Shows the user's commission allocations with filters and summary cards.

#### Requirements
1. Create `src/components/commissions/commission-filters.tsx`:
   - Date range, milestone filter (MP1/MP2), approval status, paid status
   - Project type filter
   - Clear filters
2. Create `src/components/commissions/commission-summary-cards.tsx`:
   - 4 stat cards in a grid: By Milestone (MP1, MP2), By Status summary, Grand Total
   - Use `AllocationSummary` data
3. Create `src/components/commissions/commissions-table.tsx`:
   - Columns: Sale Date, Customer (from sale), Type, Milestone, Amount, Approved, Paid, Batch
   - Sortable, row click to sale detail
4. Create `src/app/(dashboard)/commissions/page.tsx`:
   - Compose filters + summary cards + table + pagination
   - For Sales Rep: auto-filter to current user
   - For Manager: option to filter by team member
   - For Admin/Finance: no auto-filter

#### Acceptance Criteria
- [ ] Summary cards show correct totals from `AllocationSummary`
- [ ] Table displays allocations with all specified columns
- [ ] Filters work correctly
- [ ] Role-based auto-filtering is applied
- [ ] Loading, error, and empty states handled

#### Files to Create/Modify
- `src/components/commissions/commission-filters.tsx` -- create
- `src/components/commissions/commission-summary-cards.tsx` -- create
- `src/components/commissions/commissions-table.tsx` -- create
- `src/app/(dashboard)/commissions/page.tsx` -- create

---

### Task 3.3: Commission Summary Page

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Tasks 3.1, 1.8

#### Context
Detailed commission analytics with charts and breakdowns.

#### Requirements
1. Create `src/components/commissions/commission-chart.tsx`:
   - Line or bar chart using Recharts
   - Show commission amounts over time (weekly/monthly)
   - Tooltip with formatted currency
2. Create `src/components/commissions/commission-detail-card.tsx`:
   - Pie chart breakdown by allocation type
   - Top earning categories
3. Create `src/app/(dashboard)/commissions/summary/page.tsx`:
   - Period selector (month/quarter/year)
   - Summary cards: MP1 total, MP2 total, Override total, Clawback total, Grand Total
   - Commission trend chart
   - Breakdown by type pie chart

#### Acceptance Criteria
- [ ] Charts render correctly with real data shape
- [ ] Period selector changes all data on page
- [ ] Summary cards show correct totals
- [ ] Charts are responsive

#### Files to Create/Modify
- `src/components/commissions/commission-chart.tsx` -- create
- `src/components/commissions/commission-detail-card.tsx` -- create
- `src/app/(dashboard)/commissions/summary/page.tsx` -- create

---

### Task 3.4: Commission Detail Page

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Task 3.1

#### Context
Detail view for a single commission allocation.

#### Requirements
1. Create `src/app/(dashboard)/commissions/[id]/page.tsx`:
   - Show full allocation detail
   - Link to related sale
   - Approval status and history
   - Payment info (batch link if paid)

#### Acceptance Criteria
- [ ] All allocation fields displayed
- [ ] Navigation to related sale works
- [ ] Approval/payment status clear

#### Files to Create/Modify
- `src/app/(dashboard)/commissions/[id]/page.tsx` -- create

---

## Phase 4: Dashboard and Payroll

**Goal:** Role-based dashboard and finance payroll management.
**Priority:** P0 for Dashboard, P0 for Payroll.
**Depends on:** Phases 2 and 3 (for data hooks and components).

---

### Task 4.1: Dashboard Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 2.1, 3.1, 1.8

#### Context
The home page after login. Shows role-specific metrics and recent activity.

#### Requirements
1. Create `src/app/(dashboard)/dashboard/page.tsx`:
   - **Sales Rep view:**
     - 4 StatCards: Sales this month, Commissions earned, Pending payout, Goal progress
     - Recent sales table (last 5)
     - Commission trend mini-chart
   - **Manager view:**
     - Team metrics (total sales, total commissions)
     - Team member performance summary
     - Recent team sales
   - **Finance view:**
     - Pending payroll amount
     - Open batches count
     - Recent batch activity
   - **Admin view:**
     - System-wide metrics
     - Recent activity across all users
2. Use conditional rendering based on `useCurrentUser()` role

#### Acceptance Criteria
- [ ] Correct dashboard renders for each role
- [ ] StatCards show real data from API
- [ ] Charts render correctly
- [ ] Loading states for each data section
- [ ] Page is responsive

#### Files to Create/Modify
- `src/app/(dashboard)/dashboard/page.tsx` -- create

---

### Task 4.2: Payroll API Client and Hooks

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Task 1.1

#### Context
API functions for payroll batch management.

#### Requirements
1. Create `src/lib/api/payroll.ts`:
   - `getBatches(filters: BatchFilters): Promise<PaginatedResponse<PayrollBatch>>`
   - `getBatchById(id: number): Promise<ApiResponse<PayrollBatchDetail>>`
   - `createBatch(data: CreateBatchRequest): Promise<ApiResponse<PayrollBatch>>`
   - `submitBatch(id: number): Promise<void>`
   - `approveBatch(id: number): Promise<void>`
   - `exportBatch(id: number): Promise<ApiResponse<{ downloadUrl: string; expiresAt: string; format: string }>>`
   - `markBatchPaid(id: number): Promise<void>`
2. Create `src/hooks/use-payroll.ts`:
   - `useBatches(filters)` -- paginated list
   - `useBatch(id)` -- single batch detail
   - `useCreateBatch()` -- mutation
   - `useSubmitBatch()` -- mutation
   - `useApproveBatch()` -- mutation
   - `useExportBatch()` -- mutation
   - `useMarkBatchPaid()` -- mutation

#### Acceptance Criteria
- [ ] All API functions match `/payroll/batches` endpoints in API_CONTRACT.md
- [ ] Mutations invalidate batch queries on success
- [ ] Export returns download URL

#### Files to Create/Modify
- `src/lib/api/payroll.ts` -- create
- `src/hooks/use-payroll.ts` -- create

---

### Task 4.3: Payroll Batch List Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 1.7, 1.8, 4.2

#### Context
Finance users manage payroll batches here.

#### Requirements
1. Create `src/components/payroll/batch-status-badge.tsx`:
   - Color-coded badge for each BatchStatus
2. Create `src/components/payroll/batch-table.tsx`:
   - Columns: Name, Period, Status, Total Amount, Record Count, Created
   - Sortable, row click to detail
   - Actions: View, Approve, Export, Mark Paid (contextual by status)
3. Create `src/app/(dashboard)/payroll/page.tsx`:
   - Batch status filter + date range filter
   - Batch table with pagination
   - "Create Batch" button linking to `/payroll/new`

#### Acceptance Criteria
- [ ] Table shows batches with correct columns
- [ ] Status badges have distinct colors
- [ ] Actions are contextual (only show valid actions per status)
- [ ] Create Batch button navigates correctly
- [ ] Page restricted to Finance and Admin roles

#### Files to Create/Modify
- `src/components/payroll/batch-status-badge.tsx` -- create
- `src/components/payroll/batch-table.tsx` -- create
- `src/app/(dashboard)/payroll/page.tsx` -- create

---

### Task 4.4: Create Payroll Batch Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 4.2, 1.8

#### Context
Form to create a new payroll batch with preview of what will be included.

#### Requirements
1. Create `src/components/payroll/batch-form.tsx`:
   - Form fields: Batch name, pay period start/end (date pickers), pay date
   - Project type multi-select filter
   - Include clawbacks toggle
   - Form validation with Zod
2. Create `src/components/payroll/payout-list.tsx`:
   - Preview table of aggregated payouts by user
   - Show: Employee, Allocations count, Overrides count, Clawbacks count, Gross, Net
3. Create `src/app/(dashboard)/payroll/new/page.tsx`:
   - Two-step flow: configure -> preview -> create
   - BatchForm for configuration
   - PayoutPreview after configuration (optionally fetched from API)
   - Submit creates the batch and navigates to batch detail

#### Acceptance Criteria
- [ ] Form validates all required fields
- [ ] Date pickers work correctly
- [ ] Preview shows aggregated payout data
- [ ] Submit creates batch and redirects to detail page
- [ ] Cancel returns to batch list

#### Files to Create/Modify
- `src/components/payroll/batch-form.tsx` -- create
- `src/components/payroll/payout-list.tsx` -- create
- `src/app/(dashboard)/payroll/new/page.tsx` -- create

---

### Task 4.5: Payroll Batch Detail Page

**Agent:** frontend-agent
**Priority:** P0
**Depends on:** Tasks 4.2, 4.3

#### Context
Shows batch details with individual payouts and status workflow actions.

#### Requirements
1. Create `src/app/(dashboard)/payroll/[id]/page.tsx`:
   - Batch header: name, status, period, totals
   - Payout table by user (from `PayrollBatchDetail.payouts`):
     - Columns: Employee, Allocations, Overrides, Clawbacks, Gross, Clawback Amount, Net
     - Expandable rows showing individual commission line items (stretch goal)
   - Action buttons based on current status:
     - DRAFT -> Submit
     - SUBMITTED -> Approve
     - APPROVED -> Export
     - EXPORTED -> Mark Paid
   - Confirmation dialogs for status transitions
   - Export triggers file download

#### Acceptance Criteria
- [ ] All batch details displayed correctly
- [ ] Payout table shows per-user breakdown
- [ ] Action buttons match current batch status
- [ ] Status transitions work with confirmation
- [ ] Export initiates file download
- [ ] Page handles loading and error states

#### Files to Create/Modify
- `src/app/(dashboard)/payroll/[id]/page.tsx` -- create

---

## Phase 5: Team, Goals, Reports, and Profile

**Goal:** Manager team views, goal tracking, reporting, and user profile.
**Priority:** P1.
**Depends on:** Phases 2 and 3.

---

### Task 5.1: Users/Employees API Client

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Task 1.1

#### Requirements
1. Create `src/lib/api/users.ts`:
   - `getEmployees(filters): Promise<PaginatedResponse<Employee>>`
   - `getEmployeeById(userId: number): Promise<ApiResponse<Employee>>`
   - `getEmployeeRates(userId: number): Promise<ApiResponse<UserCommissionRate[]>>`
   - `getEmployeeAllocations(userId: number, filters): Promise<PaginatedResponse<CommissionAllocation>>`

#### Files to Create/Modify
- `src/lib/api/users.ts` -- create

---

### Task 5.2: Team Pages (Manager)

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Tasks 5.1, 3.1, 1.8

#### Requirements
1. Create `src/app/(dashboard)/team/page.tsx`:
   - Team member list with key metrics per member
   - Link to individual member detail
2. Create `src/app/(dashboard)/team/[userId]/page.tsx`:
   - Employee detail with their sales, commissions, rates
   - Performance summary

#### Acceptance Criteria
- [ ] Team page shows all direct reports
- [ ] Individual member page shows complete performance data
- [ ] Pages restricted to Manager and Admin roles

#### Files to Create/Modify
- `src/app/(dashboard)/team/page.tsx` -- create
- `src/app/(dashboard)/team/[userId]/page.tsx` -- create

---

### Task 5.3: Goals API Client, Hooks, and Pages

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Task 1.1, Task 1.8

#### Requirements
1. Create `src/lib/api/goals.ts` (if not covered by another file)
2. Create `src/hooks/use-goals.ts`:
   - `useGoals(filters)` -- list goals
   - `useGoal(id)` -- single goal detail with milestones
3. Create `src/components/goals/goal-card.tsx` -- goal summary with progress
4. Create `src/components/goals/goal-progress-bar.tsx` -- visual progress indicator
5. Create `src/components/goals/goal-form.tsx` -- form for creating/editing goals (if admin)
6. Create `src/app/(dashboard)/goals/page.tsx` -- goals list
7. Create `src/app/(dashboard)/goals/[id]/page.tsx` -- goal detail with milestones

#### Acceptance Criteria
- [ ] Goals list shows all active goals with progress
- [ ] Goal detail shows milestones and progress history
- [ ] Progress bar is visually clear
- [ ] Pages handle loading/error/empty states

#### Files to Create/Modify
- `src/lib/api/goals.ts` -- create
- `src/hooks/use-goals.ts` -- create
- `src/components/goals/goal-card.tsx` -- create
- `src/components/goals/goal-progress-bar.tsx` -- create
- `src/components/goals/goal-form.tsx` -- create
- `src/app/(dashboard)/goals/page.tsx` -- create
- `src/app/(dashboard)/goals/[id]/page.tsx` -- create

---

### Task 5.4: Reports Pages

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Tasks 3.1, 5.1

#### Requirements
1. Create `src/app/(dashboard)/reports/page.tsx` -- reports index/hub
2. Create `src/app/(dashboard)/reports/statements/page.tsx` -- commission statements
3. Create `src/app/(dashboard)/reports/team-performance/page.tsx` -- team performance analytics

#### Acceptance Criteria
- [ ] Reports index links to sub-reports
- [ ] Statements show per-period commission breakdowns
- [ ] Team performance shows comparative metrics
- [ ] Reports restricted to Finance/Admin (statements available to all)

#### Files to Create/Modify
- `src/app/(dashboard)/reports/page.tsx` -- create
- `src/app/(dashboard)/reports/statements/page.tsx` -- create
- `src/app/(dashboard)/reports/team-performance/page.tsx` -- create

---

### Task 5.5: Profile Page

**Agent:** frontend-agent
**Priority:** P2
**Depends on:** Task 1.2

#### Requirements
1. Create `src/app/(dashboard)/profile/page.tsx`:
   - Display user info (name, email, role, team, hire date)
   - Change password form (if supported by API)
   - Commission rates summary (read-only)

#### Files to Create/Modify
- `src/app/(dashboard)/profile/page.tsx` -- create

---

### Task 5.6: Debounce Hook

**Agent:** frontend-agent
**Priority:** P2
**Depends on:** None

#### Requirements
1. Create `src/hooks/use-debounce.ts`:
   - Generic debounce hook for search inputs
   - Configurable delay (default 300ms)

#### Files to Create/Modify
- `src/hooks/use-debounce.ts` -- create

---

## Phase 6: Admin Module

**Goal:** Admin configuration pages for users, offices, commission plans, rules, and CRM connections.
**Priority:** P1 -- Important for setup but not for day-to-day use.
**Depends on:** Phases 1-3.

---

### Task 6.1: Admin API Client

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Task 1.1

#### Requirements
1. Create `src/lib/api/admin.ts`:
   - `getAllocationTypes(): Promise<ApiResponse<AllocationType[]>>`
   - `getSalesRoles(): Promise<ApiResponse<SalesRole[]>>`
   - `getProjectTypes(): Promise<ApiResponse<ProjectTypeRef[]>>`
   - `getCrmConnections(): Promise<PaginatedResponse<CrmConnection>>`
   - `getCrmConnectionById(id): Promise<ApiResponse<CrmConnection>>`
   - `triggerSync(connectionId: number): Promise<void>`
   - `getSyncLogs(connectionId: number): Promise<PaginatedResponse<SyncRun>>`
   - Rate management: `createRate`, `updateRate`, `getRates`

#### Files to Create/Modify
- `src/lib/api/admin.ts` -- create

---

### Task 6.2: User Management Pages

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Tasks 6.1, 5.1

#### Requirements
1. Create `src/components/admin/user-form.tsx`:
   - Form for creating/editing users
   - Fields: name, email, role, team, office, hire date
   - Validation with Zod
2. Create `src/app/(dashboard)/admin/users/page.tsx` -- user list with search/filter
3. Create `src/app/(dashboard)/admin/users/new/page.tsx` -- create user
4. Create `src/app/(dashboard)/admin/users/[id]/page.tsx` -- edit user + view their rates

#### Acceptance Criteria
- [ ] User list is searchable and filterable
- [ ] Create/edit forms validate correctly
- [ ] Pages restricted to Admin role

#### Files to Create/Modify
- `src/components/admin/user-form.tsx` -- create
- `src/app/(dashboard)/admin/users/page.tsx` -- create
- `src/app/(dashboard)/admin/users/new/page.tsx` -- create
- `src/app/(dashboard)/admin/users/[id]/page.tsx` -- create

---

### Task 6.3: Office Management Page

**Agent:** frontend-agent
**Priority:** P2
**Depends on:** Task 6.1

#### Requirements
1. Create `src/app/(dashboard)/admin/offices/page.tsx`:
   - List offices with region
   - Add/edit office (inline or dialog)

#### Files to Create/Modify
- `src/app/(dashboard)/admin/offices/page.tsx` -- create

---

### Task 6.4: Commission Plan Pages

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Task 6.1

#### Requirements
1. Create `src/components/admin/commission-plan-form.tsx`:
   - Form for plan name, dates, status
2. Create `src/app/(dashboard)/admin/commission-plans/page.tsx` -- plan list
3. Create `src/app/(dashboard)/admin/commission-plans/new/page.tsx` -- create plan
4. Create `src/app/(dashboard)/admin/commission-plans/[id]/page.tsx` -- plan detail with rules table

#### Acceptance Criteria
- [ ] Plan list shows all plans with status
- [ ] Create/edit forms work correctly
- [ ] Plan detail shows associated rules

#### Files to Create/Modify
- `src/components/admin/commission-plan-form.tsx` -- create
- `src/app/(dashboard)/admin/commission-plans/page.tsx` -- create
- `src/app/(dashboard)/admin/commission-plans/new/page.tsx` -- create
- `src/app/(dashboard)/admin/commission-plans/[id]/page.tsx` -- create

---

### Task 6.5: Commission Rule Editor

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** Task 6.4

#### Context
This is the most complex UI component in the application. It includes a dynamic condition builder,
rate type selection, and recipient configuration.

#### Requirements
1. Create `src/components/admin/commission-rule-form.tsx`:
   - Name, description, rule type (Percentage/Flat/Tiered)
   - Dynamic rate configuration based on type
   - Recipient type selector
   - Split config (if applicable)
   - Override config (if applicable)
   - Priority field
2. Create `src/components/admin/rule-condition-builder.tsx`:
   - Visual condition editor per FRONTEND_STRUCTURE.md spec
   - Add/remove conditions
   - Field selector (sale_type, rep.level, amount, etc.)
   - Operator selector (equals, not_equals, greater_than, is_one_of, etc.)
   - Value input (dynamic based on field type)
3. Create `src/app/(dashboard)/admin/commission-plans/[id]/rules/page.tsx`:
   - Rules table for the plan
   - Add/edit rule dialog or page

#### Acceptance Criteria
- [ ] Condition builder supports add/remove conditions
- [ ] Field/operator/value selectors work dynamically
- [ ] Rule form validates all required fields
- [ ] Rules can be created, edited, duplicated, deleted
- [ ] Rate configuration changes based on rule type

#### Files to Create/Modify
- `src/components/admin/commission-rule-form.tsx` -- create
- `src/components/admin/rule-condition-builder.tsx` -- create
- `src/app/(dashboard)/admin/commission-plans/[id]/rules/page.tsx` -- create

---

### Task 6.6: CRM Connections Pages

**Agent:** frontend-agent
**Priority:** P2
**Depends on:** Task 6.1

#### Requirements
1. Create `src/components/admin/crm-connection-form.tsx`:
   - Form for CRM connection config
   - Source system selector, sync settings
2. Create `src/app/(dashboard)/admin/crm-connections/page.tsx` -- connections list
3. Create `src/app/(dashboard)/admin/crm-connections/[id]/page.tsx`:
   - Connection detail with sync logs
   - Manual sync trigger button

#### Acceptance Criteria
- [ ] Connection list shows status and last sync info
- [ ] Manual sync trigger works
- [ ] Sync logs display with status indicators

#### Files to Create/Modify
- `src/components/admin/crm-connection-form.tsx` -- create
- `src/app/(dashboard)/admin/crm-connections/page.tsx` -- create
- `src/app/(dashboard)/admin/crm-connections/[id]/page.tsx` -- create

---

## Phase 7: Polish and Testing

**Goal:** Error handling, accessibility, testing, and production readiness.
**Priority:** P1.
**Depends on:** All previous phases.

---

### Task 7.1: Global Error Handling

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** All feature pages

#### Requirements
1. Create `src/app/not-found.tsx` -- custom 404 page
2. Create `src/app/error.tsx` -- global error boundary page
3. Create `src/app/(dashboard)/error.tsx` -- dashboard-specific error page
4. Add toast notifications for mutation success/failure across all forms
5. Ensure all pages have loading, error, and empty states

#### Files to Create/Modify
- `src/app/not-found.tsx` -- create
- `src/app/error.tsx` -- create
- `src/app/(dashboard)/error.tsx` -- create

---

### Task 7.2: Testing Setup

**Agent:** frontend-agent
**Priority:** P1
**Depends on:** All feature work

#### Requirements
1. Install and configure Vitest
2. Install React Testing Library
3. Write unit tests for:
   - `src/lib/utils.ts` functions
   - `src/lib/validators.ts` schemas
4. Write component tests for:
   - `StatCard` rendering
   - `DataTable` with mock data
   - `BatchStatusBadge` variants
5. Configure Playwright for E2E tests
6. Write E2E tests for critical flows:
   - Login flow
   - View sales list
   - View commission summary
   - Create payroll batch (mock API)

#### Files to Create/Modify
- `vitest.config.ts` -- create
- `playwright.config.ts` -- create
- `src/__tests__/` -- create test files

---

### Task 7.3: Performance and Accessibility

**Agent:** frontend-agent
**Priority:** P2
**Depends on:** All feature work

#### Requirements
1. Add `Suspense` boundaries for route-level code splitting
2. Lazy load charts and heavy components
3. Ensure proper heading hierarchy on all pages
4. Add ARIA labels to interactive elements
5. Keyboard navigation support for DataTable and forms
6. Run Lighthouse audit and address critical issues

---

## Dependency Graph

```
Phase 0 (Scaffolding)
  |
  v
Phase 1 (Auth + Layout)
  |
  +----> Phase 2 (Sales)
  |        |
  |        v
  +----> Phase 3 (Commissions)
  |        |
  |        v
  +----> Phase 4 (Dashboard + Payroll)
  |        |
  |        v
  +----> Phase 5 (Team + Goals + Reports)
  |        |
  |        v
  +----> Phase 6 (Admin)
           |
           v
         Phase 7 (Polish + Testing)
```

**Parallelization opportunities:**
- Within Phase 1: Tasks 1.1, 1.3, 1.4 can run in parallel
- Phase 2 and Phase 3 API clients (Tasks 2.1, 3.1) can run in parallel after Task 1.1
- Phase 4 Payroll work can run in parallel with Phase 3 Commission pages
- Phase 5 and Phase 6 can run in parallel

---

## Implementation Order (Recommended Sequence)

| Order | Task | Agent | Est. Effort | Critical Path |
|-------|------|-------|-------------|---------------|
| 1 | 0.1 Initialize Next.js | frontend-agent | Medium | Yes |
| 2 | 0.2 Initialize shadcn/ui | frontend-agent | Small | Yes |
| 3 | 0.3 Core Utilities | frontend-agent | Small | Yes |
| 4 | 1.1 API Client | frontend-agent | Medium | Yes |
| 5 | 1.3 Query Provider | frontend-agent | Small | Yes |
| 6 | 1.2 Auth Provider + Hooks | frontend-agent | Medium | Yes |
| 7 | 1.4 Theme Provider | frontend-agent | Small | No |
| 8 | 1.5 Middleware | frontend-agent | Small | Yes |
| 9 | 1.8 Shared UI Components | frontend-agent | Large | Yes |
| 10 | 1.7 Dashboard Layout Shell | frontend-agent | Large | Yes |
| 11 | 1.6 Login Page | frontend-agent | Medium | Yes |
| 12 | 2.1 Sales API + Hooks | frontend-agent | Medium | Yes |
| 13 | 2.2 Sales List Page | frontend-agent | Medium | Yes |
| 14 | 2.3 Sale Detail Page | frontend-agent | Medium | Yes |
| 15 | 3.1 Commissions API + Hooks | frontend-agent | Medium | Yes |
| 16 | 3.2 Commissions List Page | frontend-agent | Medium | Yes |
| 17 | 4.1 Dashboard Page | frontend-agent | Large | Yes |
| 18 | 4.2 Payroll API + Hooks | frontend-agent | Medium | Yes |
| 19 | 4.3 Payroll Batch List | frontend-agent | Medium | Yes |
| 20 | 4.4 Create Batch Page | frontend-agent | Large | Yes |
| 21 | 4.5 Batch Detail Page | frontend-agent | Large | Yes |
| 22 | 3.3 Commission Summary | frontend-agent | Medium | No |
| 23 | 3.4 Commission Detail | frontend-agent | Small | No |
| 24 | 5.1 Users API | frontend-agent | Small | No |
| 25 | 5.2 Team Pages | frontend-agent | Medium | No |
| 26 | 5.3 Goals Module | frontend-agent | Medium | No |
| 27 | 5.4 Reports Pages | frontend-agent | Medium | No |
| 28 | 5.5 Profile Page | frontend-agent | Small | No |
| 29 | 5.6 Debounce Hook | frontend-agent | Small | No |
| 30 | 6.1 Admin API | frontend-agent | Medium | No |
| 31 | 6.2 User Management | frontend-agent | Medium | No |
| 32 | 6.4 Commission Plans | frontend-agent | Medium | No |
| 33 | 6.5 Rule Editor | frontend-agent | Large | No |
| 34 | 6.3 Office Management | frontend-agent | Small | No |
| 35 | 6.6 CRM Connections | frontend-agent | Medium | No |
| 36 | 7.1 Error Handling | frontend-agent | Small | No |
| 37 | 7.2 Testing | frontend-agent | Large | No |
| 38 | 7.3 Performance/A11y | frontend-agent | Medium | No |

---

## Key Architectural Decisions

1. **Single types file:** Keep `src/types/index.ts` as-is rather than splitting per domain.
   The file is well-organized and avoids circular import complexity.

2. **API terminology mapping:** The UI uses "Commissions" as user-facing labels, but the API
   layer targets `/allocations`, `/overrides`, `/clawbacks`. The hooks and API client files use
   the API terminology internally; components translate to user-friendly labels.

3. **URL-based filter state:** Table filters are stored in URL search params (`useSearchParams`)
   for shareable, bookmarkable URLs and browser back/forward support.

4. **Role gating:** Implement at two levels:
   - Middleware: redirect unauthorized users away from protected route groups
   - Component-level: `useCurrentUser()` hook for conditional rendering of actions/sections

5. **Mutation feedback:** Use toast notifications (via `sonner` or shadcn toast) for all
   mutation success/failure feedback rather than inline messages.
