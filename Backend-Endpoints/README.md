# Endpoints

## CRUD Endpoint Usage

There are two dynamic CRUD endpoint sets:

1) Tenant-scoped (schema comes from subdomain via TenantMiddleware)
   Base: `/api/db/{table}`

2) Explicit schema (admin/cross-tenant)
   Base: `/api/db/{schema}/{table}`

### Tenant-scoped examples

```http
GET /api/db/users
GET /api/db/sales/123
POST /api/db/customers
PUT /api/db/customers/42
DELETE /api/db/sale_contracts/9001
```

```json
// POST /api/db/customers
{
  "firstName": "Ada",
  "lastName": "Lovelace",
  "email": "ada@example.com",
  "phone": "555-555-5555"
}
```

```json
// PUT /api/db/customers/42
{
  "customerId": 42,
  "firstName": "Ada",
  "lastName": "Lovelace",
  "email": "ada@newdomain.com",
  "phone": "555-555-5555"
}
```

### Explicit-schema examples

```http
GET /api/db/acme/users
GET /api/db/acme/sales/123
POST /api/db/acme/customers
PUT /api/db/acme/customers/42
DELETE /api/db/acme/sale_contracts/9001
```

```json
// POST /api/db/acme/customers
{
  "firstName": "Grace",
  "lastName": "Hopper",
  "email": "grace@example.com",
  "phone": "555-555-5555"
}
```

Notes:
- `{table}` is the EF Core table name (e.g., `sale_contracts`, `roofing_sales`).
- Composite-key tables return 400 for `{id}` routes.
