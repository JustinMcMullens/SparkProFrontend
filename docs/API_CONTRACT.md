# Commission Platform API Contract

## Base URL
```
/api/v1
```

## Authentication
All endpoints require JWT Bearer token:
```
Authorization: Bearer <token>
```

## Response Format

### Success
```json
{
    "data": { ... },
    "meta": { "timestamp": "2025-02-05T12:00:00Z" }
}
```

### Paginated
```json
{
    "data": [ ... ],
    "meta": {
        "page": 1,
        "pageSize": 20,
        "totalCount": 150,
        "totalPages": 8
    }
}
```

### Error (RFC 7807)
```json
{
    "type": "https://api.sparkcommission.com/errors/not-found",
    "title": "Sale Not Found",
    "status": 404,
    "detail": "Sale with ID 123 does not exist",
    "instance": "/api/v1/sales/123"
}
```

---

## Sales

### GET /sales
List sales with filters.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| page | int | Page number |
| pageSize | int | Items per page (max 100) |
| startDate | date | Sale date from |
| endDate | date | Sale date to |
| status | string | PENDING, APPROVED, COMPLETED, CANCELLED |
| projectTypeId | int | Filter by industry |
| customerId | int | Filter by customer |

**Response:**
```json
{
    "data": [
        {
            "saleId": 123,
            "customerId": 456,
            "customerName": "John Smith",
            "projectTypeId": 1,
            "projectTypeName": "Solar",
            "saleDate": "2025-01-15",
            "saleStatus": "APPROVED",
            "contractAmount": 25000.00,
            "isActive": true,
            "participants": [
                { "userId": 10, "userName": "Alice Rep", "roleId": 1, "roleName": "Closer" }
            ],
            "allocationTotal": 1250.00,
            "createdAt": "2025-01-15T10:30:00Z"
        }
    ]
}
```

### GET /sales/{id}
Get sale detail with allocations.

**Response:**
```json
{
    "data": {
        "saleId": 123,
        "customerId": 456,
        "customer": {
            "customerId": 456,
            "name": "John Smith",
            "email": "john@example.com"
        },
        "projectTypeId": 1,
        "projectTypeName": "Solar",
        "saleDate": "2025-01-15",
        "saleStatus": "APPROVED",
        "contractAmount": 25000.00,
        "participants": [ ... ],
        "allocations": [
            {
                "allocationId": 789,
                "userId": 10,
                "userName": "Alice Rep",
                "allocationTypeId": 1,
                "allocationTypeName": "Closer",
                "milestoneNumber": 1,
                "allocatedAmount": 1000.00,
                "isApproved": true,
                "isPaid": false
            }
        ],
        "overrides": [
            {
                "allocationId": 790,
                "userId": 5,
                "userName": "Bob Manager",
                "overrideLevel": 1,
                "allocatedAmount": 250.00,
                "isApproved": true,
                "isPaid": false
            }
        ],
        "clawbacks": []
    }
}
```

### POST /sales/{id}/recalculate
Recalculate commissions (deletes unpaid, recalculates).

**Response:** `200 OK` with CalculationResult

### POST /sales/{id}/cancel
Cancel sale and trigger clawbacks.

**Request:**
```json
{
    "reason": "Customer cancelled contract"
}
```

---

## Allocations

### GET /allocations
List commission allocations.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| userId | int | Filter by recipient |
| saleId | int | Filter by sale |
| projectTypeId | int | Industry filter |
| milestoneNumber | int | 1 or 2 |
| isApproved | bool | Approval status |
| isPaid | bool | Payment status |
| payrollBatchId | int | Filter by batch |
| startDate | date | Allocation date from |
| endDate | date | Allocation date to |

**Response:**
```json
{
    "data": [
        {
            "allocationId": 789,
            "saleId": 123,
            "saleDate": "2025-01-15",
            "contractAmount": 25000.00,
            "userId": 10,
            "userName": "Alice Rep",
            "allocationTypeId": 1,
            "allocationTypeName": "Closer",
            "milestoneNumber": 1,
            "allocatedAmount": 1000.00,
            "isApproved": true,
            "approvedAt": "2025-01-16T09:00:00Z",
            "isPaid": false,
            "payrollBatchId": null,
            "projectTypeName": "Solar"
        }
    ]
}
```

### GET /allocations/summary
Get allocation totals.

**Query Params:** Same filters as list

**Response:**
```json
{
    "data": {
        "period": "2025-01",
        "byMilestone": {
            "mp1": { "count": 45, "total": 12500.00 },
            "mp2": { "count": 30, "total": 8500.00 }
        },
        "byStatus": {
            "pending": 5000.00,
            "approved": 10000.00,
            "paid": 6000.00
        },
        "byAllocationType": [
            { "typeId": 1, "typeName": "Closer", "total": 15000.00 },
            { "typeId": 2, "typeName": "Setter", "total": 6000.00 }
        ],
        "grandTotal": 21000.00
    }
}
```

### POST /allocations/{id}/approve
Approve single allocation.

### POST /allocations/batch-approve
Batch approve allocations.

**Request:**
```json
{
    "allocationIds": [789, 790, 791]
}
```

---

## Overrides

### GET /overrides
List override allocations.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| userId | int | Manager who received override |
| saleId | int | Filter by sale |
| overrideLevel | int | 1, 2, 3... |
| isApproved | bool | Approval status |
| isPaid | bool | Payment status |

### GET /overrides/summary
Override totals by level, user, period.

---

## Clawbacks

### GET /clawbacks
List clawback records.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| userId | int | Who owes clawback |
| saleId | int | Related sale |
| isProcessed | bool | Processing status |
| startDate | date | Clawback date from |
| endDate | date | Clawback date to |

### POST /clawbacks/{id}/process
Mark clawback as processed.

### POST /clawbacks/batch-process
Batch process clawbacks.

---

## Payroll Batches

### GET /payroll/batches
List payroll batches.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| status | string | DRAFT, SUBMITTED, APPROVED, EXPORTED, PAID |
| startDate | date | Pay period start from |
| endDate | date | Pay period end to |

**Response:**
```json
{
    "data": [
        {
            "batchId": 50,
            "batchName": "January 2025 - Week 4",
            "payPeriodStart": "2025-01-20",
            "payPeriodEnd": "2025-01-26",
            "payDate": "2025-01-31",
            "status": "DRAFT",
            "totalAmount": 45000.00,
            "recordCount": 156,
            "createdBy": 5,
            "createdAt": "2025-01-27T08:00:00Z"
        }
    ]
}
```

### GET /payroll/batches/{id}
Get batch with payouts by user.

**Response:**
```json
{
    "data": {
        "batchId": 50,
        "batchName": "January 2025 - Week 4",
        "payPeriodStart": "2025-01-20",
        "payPeriodEnd": "2025-01-26",
        "payDate": "2025-01-31",
        "status": "DRAFT",
        "totalAmount": 45000.00,
        "recordCount": 156,
        "payouts": [
            {
                "userId": 10,
                "userName": "Alice Rep",
                "allocations": 8,
                "overrides": 0,
                "clawbacks": 0,
                "grossAmount": 2500.00,
                "clawbackAmount": 0.00,
                "netAmount": 2500.00
            }
        ]
    }
}
```

### POST /payroll/batches
Create new batch.

**Request:**
```json
{
    "batchName": "January 2025 - Week 4",
    "payPeriodStart": "2025-01-20",
    "payPeriodEnd": "2025-01-26",
    "payDate": "2025-01-31",
    "projectTypeIds": [1, 2],
    "includeClawbacks": true
}
```

### POST /payroll/batches/{id}/submit
Submit batch for approval.

### POST /payroll/batches/{id}/approve
Approve batch.

### POST /payroll/batches/{id}/export
Export batch to CSV/payroll format.

**Response:**
```json
{
    "data": {
        "downloadUrl": "https://...",
        "expiresAt": "2025-01-27T09:00:00Z",
        "format": "csv"
    }
}
```

### POST /payroll/batches/{id}/mark-paid
Mark batch as paid (updates all allocations).

---

## Rates

### GET /rates
List commission rates.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| userId | int | Filter by user |
| projectTypeId | int | Industry |
| roleId | int | Filter by role |
| isActive | bool | Active only |

### GET /rates/lookup
Find applicable rate for a user/sale.

**Query Params:**
| Param | Type | Description |
|-------|------|-------------|
| userId | int | Required |
| saleId | int | Required |
| roleId | int | Optional |

**Response:**
```json
{
    "data": {
        "rateId": 100,
        "userId": 10,
        "roleId": 1,
        "installerId": null,
        "stateCode": "TX",
        "percentMp1": 4.00,
        "flatMp1": 100.00,
        "percentMp2": 2.00,
        "flatMp2": 50.00,
        "effectiveStartDate": "2025-01-01",
        "matchedOn": ["userId", "roleId", "stateCode"]
    }
}
```

### POST /rates
Create new rate.

**Request:**
```json
{
    "userId": 10,
    "roleId": 1,
    "installerId": null,
    "stateCode": "TX",
    "percentMp1": 4.00,
    "flatMp1": 100.00,
    "percentMp2": 2.00,
    "flatMp2": 50.00,
    "effectiveStartDate": "2025-01-01",
    "effectiveEndDate": null
}
```

### PUT /rates/{id}
Update rate.

---

## Employees

### GET /employees
List employees in tenant.

### GET /employees/{userId}
Get employee detail with team/manager.

### GET /employees/{userId}/rates
Get all rates for an employee.

### GET /employees/{userId}/allocations
Get allocations for an employee.

---

## Goals

### GET /goals
List goals.

### GET /goals/{id}
Get goal with progress.

**Response:**
```json
{
    "data": {
        "goalId": 20,
        "userId": 10,
        "userName": "Alice Rep",
        "goalType": "sales_amount",
        "targetValue": 100000.00,
        "currentValue": 45000.00,
        "progressPercent": 45.0,
        "periodStart": "2025-01-01",
        "periodEnd": "2025-03-31",
        "daysRemaining": 55,
        "milestones": [
            { "milestoneId": 1, "targetValue": 25000, "isAchieved": true },
            { "milestoneId": 2, "targetValue": 50000, "isAchieved": false }
        ]
    }
}
```

---

## Admin

### GET /admin/allocation-types
List allocation types (Closer, Setter, etc.)

### GET /admin/sales-roles
List sales roles.

### GET /admin/project-types
List project types (Solar, Pest, etc.)

### GET /admin/crm-connections
List CRM connections.

### POST /admin/crm-connections/{id}/sync
Trigger manual sync.

### GET /admin/crm-connections/{id}/sync-logs
Get sync history.
