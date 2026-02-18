# Frontend API Endpoint Summary

> **Note:** This is a summary of what is currently implemented. For the full design specification and internal logic details, see [`ENDPOINT_IMPLEMENTATION_PLAN.md`](./ENDPOINT_IMPLEMENTATION_PLAN.md).

## Authority Levels

| Level | Role |
|-------|------|
| 1–2 | Individual Contributors (Reps / Setters / Closers) |
| 3 | Team Lead |
| 4 | Management |
| 5 | Admin / Executive |

## Response Envelope

All endpoints use `ApiResults` helpers:
- **List:** `{ "data": [...], "meta": { "page": 1, "pageSize": 25, "totalCount": 100 } }`
- **Single:** `{ "data": { ... } }`
- **Errors:** RFC 7807 Problem Details

All list endpoints support `?page` and `?pageSize` (default 25).

---

## Sales — `/api/sales`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/sales` | Authenticated | Paginated sales list with filters |
| GET | `/api/sales/{saleId}` | Authenticated | Full sale detail |
| POST | `/api/sales/{saleId}/cancel` | L4+ | Cancel a sale |
| GET | `/api/sales/{saleId}/allocations` | Authenticated | Allocations & overrides for a sale |
| GET | `/api/sales/{saleId}/notes` | Authenticated | Customer & project notes |

**GET `/api/sales` query params:** `page`, `pageSize`, `status`, `dateFrom`, `dateTo`, `projectTypeId`, `userId`, `installerId`, `customerId`, `generationTypeId`, `contractAmountMin`, `contractAmountMax`, `sortBy`, `sortDir`

**Scoping:** L1–2 see only their own sales; L4+ see all.

**Example response (`GET /api/sales`):**
```json
{
  "data": [
    {
      "saleId": 1,
      "saleDate": "2025-01-15",
      "status": "APPROVED",
      "contractAmount": 28000.00,
      "projectType": "Solar",
      "participants": [{ "name": "Jane Doe", "role": "Closer" }]
    }
  ],
  "meta": { "page": 1, "pageSize": 25, "totalCount": 134 }
}
```

**Cancel body:** `{ "reason": "Customer cancelled contract" }`

---

## Dashboard — `/api/dashboard`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/dashboard/stats` | Authenticated | KPI stats scoped by authority |
| GET | `/api/dashboard/recent-activity` | Authenticated | Recent sales feed |
| GET | `/api/dashboard/leaderboard` | Authenticated | Top earners for period |
| GET | `/api/dashboard/announcements` | Authenticated | Active announcements (max 20) |

**Query params:** `stats`: `?periodStart`, `?periodEnd` | `recent-activity`: `?count` | `leaderboard`: `?periodStart`, `?periodEnd`, `?limit`

**Example response (`GET /api/dashboard/stats`):**
```json
{
  "data": {
    "sales": {
      "totalCount": 45,
      "totalValue": 250000.00,
      "byStatus": {
        "PENDING":   { "count": 5,  "value": 30000 },
        "APPROVED":  { "count": 20, "value": 120000 },
        "COMPLETED": { "count": 15, "value": 80000 },
        "CANCELLED": { "count": 5,  "value": 20000 }
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

---

## Profile — various routes

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/user/{userId}/companies` | Owner / Admin | User's company memberships |
| PUT | `/api/user/{userId}/last-company` | Owner / Admin | Update last accessed company |
| GET | `/managedUsers/{userId}` | Self / L4+ | Users managed by this person |
| GET | `/api/profile/{userId}/commission-summary` | Owner | Commission totals by industry & period |
| GET | `/api/profile/{userId}/recent-sales` | Owner | Recent sales (`?count=`) |
| GET | `/api/profile/{userId}/goal-progress` | Owner | Active goal progress |
| GET | `/payroll/{userId}` | Self / L4+ | Payroll deals (setter / closer split) |
| GET | `/org/users/{requesterUserId}` | L4+ | Full org chart |
| POST | `/profile/{userId}/images` | Owner / Admin | Upload profile / banner images |

**Image upload form fields:** `file` (multipart), `kind` = `profile` \| `profilebanner` \| `dashboardbanner`

---

## Team — `/api/team`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/team/sales` | L3+ | Managed team's sales (paginated) |
| GET | `/api/team/members` | L3+ | Direct reports with monthly stats |
| GET | `/api/team/performance` | L3+ | Aggregate team performance metrics |
| GET | `/api/team/pending-approvals` | L4+ | Allocations awaiting approval |

**Query params:** `sales`: same filter suite as `/api/sales` | `performance`: `?periodStart`, `?periodEnd`

**Example response (`GET /api/team/performance`):**
```json
{
  "data": {
    "period": { "start": "2025-01-01", "end": "2025-01-31" },
    "teamSize": 6,
    "totalSales": 28,
    "totalValue": 182000.00,
    "totalCommissions": 14400.00,
    "byMember": [
      { "userId": "abc", "name": "John D.", "sales": 5, "commissions": 2400.00 }
    ]
  }
}
```

---

## Rates — `/api/rates`

`{industry}` = `solar` | `pest` | `roofing` | `fiber`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/rates/{industry}` | L3+ | List commission rates (paginated) |
| GET | `/api/rates/{industry}/user/{userId}` | L3+ | All rates for a specific user |
| POST | `/api/rates/{industry}` | L4+ | Create rate |
| PUT | `/api/rates/{industry}/{rateId}` | L4+ | Update rate |
| DELETE | `/api/rates/{industry}/{rateId}` | L4+ | Soft-delete rate (`IsActive = false`) |

**GET query params:** `?userId`, `?roleId`, `?stateCode`, `?isActive`, `?page`, `?pageSize`

---

## Allocations — `/api/allocations`, `/api/overrides`, `/api/clawbacks`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/allocations` | Authenticated | List allocations (authority-scoped) |
| POST | `/api/allocations/{industry}/{id}/approve` | L4+ | Approve single allocation |
| POST | `/api/allocations/batch-approve` | L4+ | Batch approve allocations |
| GET | `/api/overrides` | Authenticated | List override allocations |
| POST | `/api/overrides/{id}/approve` | L4+ | Approve override |
| GET | `/api/clawbacks` | Authenticated | List clawbacks |

**GET query params:** `?userId`, `?saleId`, `?isApproved`, `?isPaid`, `?industry`, `?dateFrom`, `?dateTo`, `?page`, `?pageSize`

**Batch approve body:** `[{ "industry": "roofing", "allocationId": 42 }, ...]`

**Example response (`POST /api/allocations/batch-approve`):**
```json
{
  "data": { "approved": 5, "total": 5, "errors": [] }
}
```

---

## Collaborators — `/api/admin`

All routes require **L4+**.

| Method | Route | Purpose |
|--------|-------|---------|
| GET / POST | `/api/admin/installers` | List / create installers |
| PUT / DELETE | `/api/admin/installers/{id}` | Update / soft-delete installer |
| GET | `/api/admin/installers/{id}/coverage` | Get installer coverage areas |
| GET / POST | `/api/admin/dealers` | List / create dealers |
| PUT / DELETE | `/api/admin/dealers/{id}` | Update / soft-delete dealer |
| GET / POST | `/api/admin/finance-companies` | List / create finance companies |
| PUT / DELETE | `/api/admin/finance-companies/{id}` | Update / soft-delete finance company |
| GET / POST | `/api/admin/partners` | List / create partners |
| PUT / DELETE | `/api/admin/partners/{id}` | Update / soft-delete partner |

All list endpoints support `?page`, `?pageSize`, `?isActive`.

---

## Payroll Batches — `/api/payroll`

All routes require **L4+** (approve / export / mark-paid require **L5**).

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/payroll/batches` | L4+ | List batches (`?status=`) |
| GET | `/api/payroll/batches/{id}` | L4+ | Batch detail with all allocations |
| POST | `/api/payroll/batches` | L4+ | Create draft batch |
| PUT | `/api/payroll/batches/{id}` | L4+ | Update draft batch |
| POST | `/api/payroll/batches/{id}/add-allocations` | L4+ | Add approved allocations to batch |
| POST | `/api/payroll/batches/{id}/submit` | L4+ | DRAFT → SUBMITTED |
| POST | `/api/payroll/batches/{id}/approve` | L5 | SUBMITTED → APPROVED |
| POST | `/api/payroll/batches/{id}/export` | L5 | APPROVED → EXPORTED |
| POST | `/api/payroll/batches/{id}/mark-paid` | L5 | Mark batch + allocations PAID |

**State machine:** `DRAFT → SUBMITTED → APPROVED → EXPORTED → PAID` (any non-PAID state can be CANCELLED)

**add-allocations body:** `[{ "industry": "solar", "allocationId": 7 }, ...]`

---

## Goals — `/api/goals`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/goals` | Authenticated | Goals scoped to user's org level |
| GET | `/api/goals/{id}` | Authenticated | Goal detail with progress % |
| GET | `/api/goals/my-progress` | Authenticated | User's active goal progress summary |
| GET | `/api/goals/leaderboard/{leaderboardId}` | Authenticated | Ranked users for a goal |
| POST | `/api/goals` | L4+ | Create goal |
| PUT | `/api/goals/{id}` | L4+ | Update goal |

**Scoping:** individual → team → office → region → company goals all surfaced based on the user's org membership.

---

## Announcements — `/api/announcements`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/announcements` | Authenticated | Active announcements (with `IsAcknowledged`) |
| GET | `/api/announcements/{id}` | Authenticated | Full detail (records a view) |
| GET | `/api/announcements/unread-count` | Authenticated | Badge count |
| POST | `/api/announcements` | L4+ | Create announcement with targeting |
| PUT | `/api/announcements/{id}` | L4+ | Update announcement |
| DELETE | `/api/announcements/{id}` | L4+ | Soft-delete |
| POST | `/api/announcements/{id}/acknowledge` | Authenticated | Mark as acknowledged |

**Example response (`GET /api/announcements/unread-count`):**
```json
{ "data": { "unreadCount": 3 } }
```

---

## Tickets — `/api/tickets`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/tickets` | Authenticated | List (L1–2 = own; L3 = team; L4+ = all) |
| GET | `/api/tickets/{id}` | Authenticated | Full detail with comments & status history |
| POST | `/api/tickets` | Authenticated | Create ticket |
| PUT | `/api/tickets/{id}` | Authenticated | Update ticket fields |
| POST | `/api/tickets/{id}/comments` | Authenticated | Add comment |
| POST | `/api/tickets/{id}/status` | L3+ | Change status (creates history record) |
| POST | `/api/tickets/{id}/assign` | L3+ | Assign ticket to employee |

**GET query params:** `?status`, `?priority`, `?assignedTo`, `?page`, `?pageSize`

**Status flow:** `OPEN → IN_PROGRESS → PENDING → RESOLVED → CLOSED`

---

## Company Settings — `/api/settings/company`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/settings/company` | Authenticated | Company config & reference data |
| PUT | `/api/settings/company` | L5 | Update company settings *(not yet implemented)* |

**Example response (`GET /api/settings/company`):**
```json
{
  "data": {
    "subdomain": "acme",
    "projectTypes": [{ "id": 1, "name": "Solar" }],
    "salesRoles": [...],
    "generationTypes": [...],
    "allocationTypes": [...]
  }
}
```

---

## Paystubs — `/api/paystubs`

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/paystubs` | Authenticated | Last 12 salary payouts |
| GET | `/api/paystubs/commissions` | Authenticated | Commission history by industry (last 10 each) |
| GET | `/api/paystubs/upcoming` | L4+ | Unpaid allocations ready for payroll |
| GET | `/api/paystubs/summary` | — | **Not implemented (501)** |
| GET | `/api/paystubs/pending` | — | **Not implemented (501)** |

> **Note:** `/api/paystubs/upcoming` currently returns placeholder/test data and needs a real implementation.

---

## Incomplete / Stub Routes

The following routes are defined in the implementation plan but **not yet present in code:**

| Route | Notes |
|-------|-------|
| `GET /api/rates/{industry}/lookup` | Rate preview / match lookup |
| `POST /api/clawbacks/{id}/process` | Process a clawback |
| `POST /api/goals/{id}/assign` | Assign users to a goal |
| `PUT /api/settings/company` | Company settings update |
| `GET /api/paystubs/summary` | Returns 501 |
| `GET /api/paystubs/pending` | Returns 501 |
