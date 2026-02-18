# Frontend API Endpoints Implementation Plan

## Context

The frontend needs purpose-built endpoints for sales reps, managers, and admins. The dynamic CRUD endpoints exist but lack filtering, pagination, business logic, and authority-scoped data access. The goal is to build all the backend endpoints so the frontend can be developed in parallel with the commission calculators.

## Conventions

**All new endpoints** use a standardized response envelope:
- List: `{ "data": [...], "meta": { "page": 1, "pageSize": 25, "totalCount": 100 } }`
- Single: `{ "data": { ... } }`
- Errors: RFC 7807 Problem Details via `Results.Problem()`

## Existing Code to Reuse

| What | File | Notes |
|------|------|-------|
| Auth helpers | `Endpoints/EndpointAuthHelpers.cs` | `RequireAuth`, `RequireAuthAndOwnership`, `GetAuthenticatedUserId` |
| Existing DTOs | `Endpoints/EndpointModels.cs` | Commission DTOs, login/profile requests |
| Permission scoping | `Services/PermissionsService.cs` | `GetAccessibleUserIdsAsync()`, `HasPermissionAsync()` |
| Rate lookup | `Services/Commission/IRateLookupService.cs` | 7-level specificity hierarchy, generic `FindBestMatchingRateAsync<TRate>` |
| Commission engine | `Services/Commission/ICommissionEngine.cs` | `ProcessClawbackAsync`, `PreviewAsync`, `CalculateAsync` |
| Result types | `Services/Commission/CommissionResult.cs` | `CalculationResult`, `ClawbackResult`, `RateLookupResult` |
| Auth policies | `Program.cs` | `"Authenticated"`, `"Level5"`, `"Level4Plus"` |

## Implementation Order

### Phase 0: Shared Infrastructure

**Create `Helpers/ApiResults.cs`** - Static helper for envelope responses:
- `ApiResults.Success<T>(data)` - wraps in `{ data }`
- `ApiResults.Paged<T>(items, page, pageSize, totalCount)` - wraps in `{ data, meta }`
- `ApiResults.NotFound(detail)`, `BadRequest(detail)`, `Forbidden(detail)` - RFC 7807

**Create `Helpers/PaginationHelper.cs`** - Extension method on `IQueryable<T>`:
- `query.PaginateAsync(page, pageSize)` returns `(List<T> Items, int TotalCount)`
- Clamps page/pageSize, applies Skip/Take, runs CountAsync

**Create `Helpers/QueryFilterExtensions.cs`**:
- `WhereIf(condition, predicate)` - conditionally applies Where clause

**Create `Services/AllocationQueryService.cs`** - Cross-industry allocation union:
- `GetAllAllocations()` returns `IQueryable<UnifiedAllocationDto>` using `Concat()` across all 4 industry tables
- `GetAllocationsForSale(saleId)` - sale-specific allocations
- The `UnifiedAllocationDto` adds an `Industry` string discriminator

**Create `Services/TeamHierarchyService.cs`**:
- `GetManagedUserIdsAsync(managerId, includeIndirect)` - walks Employee.ManagerId chain (max 5 levels)

**Enhance `Endpoints/EndpointAuthHelpers.cs`**:
- Add `RequireAuthority(http, minimumLevel, out userId)` - combines auth + level check
- Add `GetAuthorityLevel(http)` - returns session authority level

**Add DTOs to `Endpoints/EndpointModels.cs`**:
- `UnifiedAllocationDto`, `CancelSaleRequest`, `BatchApproveRequest`, `CreateBatchRequest`, `AddAllocationsToBatchRequest`
- `CreateGoalRequest`, `AssignGoalRequest`, `CreateAnnouncementRequest`, `AnnouncementTargetDto`
- `CreateTicketRequest`, `AddCommentRequest`, `ChangeTicketStatusRequest`

**Register in `Program.cs`**:
- DI: `AddScoped<AllocationQueryService>()`, `AddScoped<TeamHierarchyService>()`, `AddScoped<PayrollBatchService>()`
- Endpoints: `app.MapXxxEndpoints()` for all 10 new groups

---

### Phase 1: Sales Endpoints — `Endpoints/SalesEndpoints.cs`

Route group: `/api/sales`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/sales` | GET | Authenticated | Paginated list, authority-scoped |
| `/api/sales/{saleId}` | GET | Authenticated | Full detail view |
| `/api/sales/{saleId}/cancel` | POST | Level4Plus | Cancel + trigger clawbacks |
| `/api/sales/{saleId}/allocations` | GET | Authenticated | All allocations for sale |
| `/api/sales/{saleId}/notes` | GET | Authenticated | Customer + project notes |

**GET /api/sales query params**: `?page, pageSize, status, dateFrom, dateTo, projectTypeId, userId, teamId, installerId, customerId, generationTypeId, contractAmountMin, contractAmountMax, sortBy, sortDir`

**Scoping logic**: Authority < 4 uses `PermissionsService.GetAccessibleUserIdsAsync()` to filter by `SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId))`. Level 4+ sees all.

**GET /api/sales/{saleId}** includes: Sale + industry extension + Customer + SaleParticipants (with User names, Role names) + SaleTotal + ProjectPayouts + allocations (via AllocationQueryService) + OverrideAllocations + Clawbacks.

**POST /api/sales/{saleId}/cancel** calls `ICommissionEngine.ProcessClawbackAsync()` (already defined in Services/Commission/ICommissionEngine.cs).

---

### Phase 2: Dashboard Endpoints — `Endpoints/DashboardEndpoints.cs`

Route group: `/api/dashboard`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/dashboard/stats` | GET | Authenticated | KPIs scoped by authority |
| `/api/dashboard/recent-activity` | GET | Authenticated | Last 20 sales/changes |
| `/api/dashboard/leaderboard` | GET | Authenticated | Top earners for period |
| `/api/dashboard/announcements` | GET | Authenticated | Active for current user |

**GET /api/dashboard/stats** params: `?periodStart, periodEnd`

Returns:
```json
{
  "data": {
    "sales": {
      "totalCount": 45,
      "totalValue": 250000.00,
      "byStatus": {
        "PENDING": { "count": 5, "value": 30000 },
        "APPROVED": { "count": 20, "value": 120000 },
        "COMPLETED": { "count": 15, "value": 80000 },
        "CANCELLED": { "count": 5, "value": 20000 }
      }
    },
    "commissions": {
      "pending": 15000.00,
      "approved": 8000.00,
      "paid": 45000.00
    },
    "approvalQueueCount": 12
  }
}
```

Uses `AllocationQueryService.GetAllAllocations()` with authority scoping via `PermissionsService`.

---

### Phase 3: Profile Enhancements — add to `Endpoints/ProfileEndpoints.cs`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/profile/{userId}/commission-summary` | GET | Authenticated + ownership | Commission totals by period |
| `/api/profile/{userId}/recent-sales` | GET | Authenticated + ownership | Last 10 sales |
| `/api/profile/{userId}/goal-progress` | GET | Authenticated + ownership | Goals from VGoalProgressSummary |

---

### Phase 4: Team/Manager Endpoints — `Endpoints/TeamEndpoints.cs`

Route group: `/api/team`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/team/sales` | GET | Level3+ | Sales for managed users |
| `/api/team/members` | GET | Level3+ | Members with summary stats |
| `/api/team/performance` | GET | Level3+ | Aggregate team stats |
| `/api/team/pending-approvals` | GET | Level4Plus | Unapproved allocations |

Uses `TeamHierarchyService.GetManagedUserIdsAsync()` to resolve who the manager can see. Same filter suite as sales endpoints.

**GET /api/team/members** returns per member: name, title, team, office, region, sale count for period, total commissions allocated, profile image URL.

---

### Phase 5: Rate Management — `Endpoints/RateEndpoints.cs`

Route group: `/api/rates`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/rates/{industry}` | GET | Level3+ | List rates with filters |
| `/api/rates/{industry}/user/{userId}` | GET | Level3+ | Rates for specific user |
| `/api/rates/{industry}` | POST | Level4Plus | Create rate |
| `/api/rates/{industry}/{rateId}` | PUT | Level4Plus | Update rate |
| `/api/rates/{industry}/{rateId}` | DELETE | Level4Plus | Soft delete (IsActive=false) |
| `/api/rates/{industry}/lookup` | GET | Level3+ | Preview rate match |

`{industry}` = `solar|roofing|pest|fiber`. Routes to correct DbSet via switch expression. Rate lookup uses `IRateLookupService.FindBestMatchingRateWithDetailsAsync<TRate>()`.

**GET /api/rates/{industry} query params**: `?userId, roleId, installerId, stateCode, isActive, page, pageSize`

---

### Phase 6: Allocation Management — `Endpoints/AllocationEndpoints.cs`

Route group: `/api/allocations`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/allocations` | GET | Authenticated | List, authority-scoped |
| `/api/allocations/{industry}/{id}/approve` | POST | Level4Plus | Approve single |
| `/api/allocations/batch-approve` | POST | Level4Plus | Approve multiple |
| `/api/overrides` | GET | Authenticated | Override allocations |
| `/api/overrides/{id}/approve` | POST | Level4Plus | Approve override |
| `/api/clawbacks` | GET | Authenticated | Clawback list |
| `/api/clawbacks/{id}/process` | POST | Level4Plus | Process clawback |

**GET /api/allocations query params**: `?userId, saleId, isApproved, isPaid, milestoneNumber, industry, dateFrom, dateTo, page, pageSize`

Approve/write-back requires `{industry}` in the route to target the correct table. Reads use `AllocationQueryService`.

**Batch approve** accepts `{ allocationIds: [1,2,3], industry: "roofing" }` and sets `IsApproved=true, ApprovedAt, ApprovedBy` on each.

---

### Phase 7: Admin Collaborator Management — `Endpoints/CollaboratorEndpoints.cs`

Route group: `/api/admin`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/admin/installers` | GET | Level4Plus | List with filters (projectType, state, isPreferred) |
| `/api/admin/installers` | POST | Level4Plus | Add installer (link Installer to CollaboratorCompany) |
| `/api/admin/installers/{id}` | PUT | Level4Plus | Update (isPreferred, notes, isActive) |
| `/api/admin/installers/{id}` | DELETE | Level4Plus | Soft delete |
| `/api/admin/installers/{id}/coverage` | GET | Level4Plus | Get coverage by project type + state |
| `/api/admin/installers/{id}/coverage` | PUT | Level4Plus | Replace coverage entries |
| `/api/admin/dealers` | GET/POST | Level4Plus | Dealer CRUD |
| `/api/admin/dealers/{id}` | PUT/DELETE | Level4Plus | Dealer CRUD |
| `/api/admin/finance-companies` | GET/POST | Level4Plus | Finance company CRUD |
| `/api/admin/finance-companies/{id}` | PUT/DELETE | Level4Plus | Finance company CRUD |
| `/api/admin/partners` | GET/POST | Level4Plus | Partner CRUD |
| `/api/admin/partners/{id}` | PUT/DELETE | Level4Plus | Partner CRUD |

Key pattern: `Installer` is tenant-scoped (in `client_{slug}` schema) wrapping shared `CollaboratorCompany` (in `collab` schema). Coverage uses `InstallerProjectCoverage` / `DealerProjectCoverage` / `FinanceProjectCoverage` / `PartnersProjectCoverage`.

---

### Phase 8: Payroll Batch Management — `Endpoints/PayrollEndpoints.cs`

Route group: `/api/payroll`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/payroll/batches` | GET | Level4Plus | List batches with status filter |
| `/api/payroll/batches/{id}` | GET | Level4Plus | Detail with all allocations |
| `/api/payroll/batches` | POST | Level4Plus | Create draft batch |
| `/api/payroll/batches/{id}` | PUT | Level4Plus | Update draft |
| `/api/payroll/batches/{id}/add-allocations` | POST | Level4Plus | Add approved allocations to batch |
| `/api/payroll/batches/{id}/submit` | POST | Level4Plus | DRAFT -> SUBMITTED |
| `/api/payroll/batches/{id}/approve` | POST | Level5 | SUBMITTED -> APPROVED |
| `/api/payroll/batches/{id}/export` | POST | Level5 | APPROVED -> EXPORTED |
| `/api/payroll/batches/{id}/mark-paid` | POST | Level5 | EXPORTED -> PAID |

**Create `Services/PayrollBatchService.cs`** - State machine enforcement:
```
DRAFT -> SUBMITTED -> APPROVED -> EXPORTED -> PAID
Any non-PAID state -> CANCELLED
```

**mark-paid** sets `IsPaid=true` + `PaidAt` on all allocations in batch across all 4 industry tables + override allocations + salary payouts. Updates `PayrollBatch.TotalAmount` and `RecordCount`.

**add-allocations** accepts `{ allocationIds: [...], industry: "roofing" }`. Validates each allocation is approved and not already in a batch.

---

### Phase 9: Goals & Leaderboards — `Endpoints/GoalEndpoints.cs`

Route group: `/api/goals`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/goals` | GET | Authenticated | User's relevant goals |
| `/api/goals/{id}` | GET | Authenticated | Detail + milestones + assignments |
| `/api/goals/my-progress` | GET | Authenticated | Summary for current user |
| `/api/goals/leaderboard/{leaderboardId}` | GET | Authenticated | Ranked users with progress |
| `/api/goals` | POST | Level4Plus | Create goal |
| `/api/goals/{id}` | PUT | Level4Plus | Update goal |
| `/api/goals/{id}/assign` | POST | Level4Plus | Assign users to goal |

**GET /api/goals** uses `VGoalProgressSummary` view. Filters by user's org hierarchy:
- `UserId == currentUser` (individual goals)
- `TeamId == myTeam` (team goals)
- `OfficeId == myOffice` (office goals)
- `RegionId == myRegion` (region goals)
- `GoalLevel == "COMPANY"` (company-wide goals)

Resolves user's team/office/region via `Employee -> Team -> Office -> Region` includes.

**Leaderboard** loads `GoalLeaderboard` config, resolves scope, ranks users by `CurrentValue` from goals matching the leaderboard's `GoalTypeId` and period. Respects `MaxDisplayCount`, `ShowRanking`, `ShowProgressPercent`, `ShowActualValues` display flags.

---

### Phase 10: Announcements — `Endpoints/AnnouncementEndpoints.cs`

Route group: `/api/announcements`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/announcements` | GET | Authenticated | Active for current user |
| `/api/announcements/{id}` | GET | Authenticated | Single detail |
| `/api/announcements` | POST | Level4Plus | Create with targeting |
| `/api/announcements/{id}` | PUT | Level4Plus | Update |
| `/api/announcements/{id}` | DELETE | Level4Plus | Soft delete (IsActive=false) |
| `/api/announcements/{id}/acknowledge` | POST | Authenticated | Mark read/acknowledged |
| `/api/announcements/unread-count` | GET | Authenticated | Count for badge UI |

**Targeting logic for GET /api/announcements**:
1. Resolve user's teamId, officeId, regionId from Employee -> Team -> Office -> Region
2. Filter announcements where `IsActive && StartDate <= now && (EndDate == null || EndDate >= now) && (ExpiresAt == null || ExpiresAt >= now)`
3. Match targets: no targets (company-wide) OR `TargetType == "TEAM" && TargetEntityId == myTeam` OR `TargetType == "OFFICE" && TargetEntityId == myOffice` OR `TargetType == "REGION" && TargetEntityId == myRegion`
4. Include `AnnouncementViews` for current user to show read/acknowledged status
5. Order by `IsPinned desc, PostDate desc`

**POST /api/announcements** creates the `Announcement` + `AnnouncementTarget` entries in one transaction.

---

### Phase 11: Tickets — `Endpoints/TicketEndpoints.cs`

Route group: `/api/tickets`

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/tickets` | GET | Authenticated | List, authority-scoped |
| `/api/tickets/{id}` | GET | Authenticated | Detail + comments + status history |
| `/api/tickets` | POST | Authenticated | Create ticket |
| `/api/tickets/{id}` | PUT | Authenticated | Update ticket fields |
| `/api/tickets/{id}/comments` | POST | Authenticated | Add comment |
| `/api/tickets/{id}/status` | POST | Level3+ | Change status (creates TicketStatusHistory) |
| `/api/tickets/{id}/assign` | POST | Level3+ | Assign to employee |

**GET /api/tickets query params**: `?status, priority, assignedTo, saleId, customerId, page, pageSize`

**Authority scoping**: Level 1-2 see tickets they created or are assigned to. Level 3 sees team tickets. Level 4+ sees all.

**Status changes** create `TicketStatusHistory` record with old/new status, `ChangedBy`, `ChangedAt`. If new status is RESOLVED: set `ResolvedAt`, `ResolvedBy`. If CLOSED: set `ClosedAt`, `ClosedBy`.

Valid statuses: `OPEN -> IN_PROGRESS -> PENDING -> RESOLVED -> CLOSED`

---

### Enhance Existing CompanySettingsEndpoints.cs

Replace stub GET with real data: project types, sales roles, generation types, allocation types, categories. Replace 501 POST with company settings update.

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `/api/settings/company` | GET | Authenticated | Company config + reference data |
| `/api/settings/company` | PUT | Level5 | Update company settings |

---

## Files Summary

### New files (16):
- `Helpers/ApiResults.cs`
- `Helpers/PaginationHelper.cs`
- `Helpers/QueryFilterExtensions.cs`
- `Services/AllocationQueryService.cs`
- `Services/TeamHierarchyService.cs`
- `Services/PayrollBatchService.cs`
- `Endpoints/SalesEndpoints.cs`
- `Endpoints/DashboardEndpoints.cs`
- `Endpoints/TeamEndpoints.cs`
- `Endpoints/RateEndpoints.cs`
- `Endpoints/AllocationEndpoints.cs`
- `Endpoints/CollaboratorEndpoints.cs`
- `Endpoints/PayrollEndpoints.cs`
- `Endpoints/GoalEndpoints.cs`
- `Endpoints/AnnouncementEndpoints.cs`
- `Endpoints/TicketEndpoints.cs`

### Modified files (5):
- `Endpoints/EndpointAuthHelpers.cs` - add `RequireAuthority`, `GetAuthorityLevel`
- `Endpoints/EndpointModels.cs` - add new DTOs
- `Endpoints/ProfileEndpoints.cs` - add commission-summary, recent-sales, goal-progress
- `Endpoints/CompanySettingsEndpoints.cs` - replace stubs with real implementation
- `Program.cs` - register services + map new endpoint groups

## Verification

After each phase:
1. `dotnet build backend/SparkBackend/SparkBackend.sln` - must compile
2. `dotnet run --project backend/SparkBackend/SparkBackend.csproj` - must start
3. Test endpoints via the MCP postgres tool to verify queries return expected data shapes
4. Verify auth scoping: reps see only their data, managers see team, admin sees all
