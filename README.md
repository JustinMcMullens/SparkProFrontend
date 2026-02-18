# Commission Platform Web

A Next.js 14 application providing role-based dashboards for commission tracking, payroll management, and sales analytics.

## Tech Stack

- **Next.js 14+** - React framework with App Router
- **TypeScript 5** - Type safety
- **Tailwind CSS** - Styling
- **shadcn/ui** - Component library
- **TanStack Query** - Data fetching & caching
- **React Hook Form + Zod** - Form handling & validation
- **Recharts** - Charts and visualizations

## Getting Started

### Prerequisites

- Node.js 20+
- npm or pnpm

### Local Development

1. Clone the repository
2. Install dependencies:
   ```bash
   npm install
   ```
3. Copy environment file:
   ```bash
   cp .env.example .env.local
   ```
4. Update `NEXT_PUBLIC_API_URL` to point to your backend
5. Start development server:
   ```bash
   npm run dev
   ```
6. Open [http://localhost:3000](http://localhost:3000)

## Documentation

See the `/docs` folder for detailed specifications:

| Document | Description |
|----------|-------------|
| [FRONTEND_STRUCTURE.md](docs/FRONTEND_STRUCTURE.md) | Pages, components, and routing |
| [API_CONTRACT.md](docs/API_CONTRACT.md) | Backend API specification |

## Project Structure

```
src/
├── app/                    # Next.js App Router
│   ├── (auth)/            # Login, auth pages
│   ├── (dashboard)/       # Authenticated pages
│   │   ├── dashboard/     # Home dashboard
│   │   ├── sales/         # Sales views
│   │   ├── commissions/   # Commission views
│   │   ├── payroll/       # Finance/payroll
│   │   ├── admin/         # Admin settings
│   │   └── ...
│   └── api/               # API routes (if needed)
│
├── components/
│   ├── ui/                # shadcn/ui components
│   ├── layout/            # Sidebar, header, nav
│   ├── sales/             # Sales-specific components
│   ├── commissions/       # Commission components
│   ├── payroll/           # Payroll components
│   └── shared/            # Reusable components
│
├── hooks/                 # Custom React hooks
├── lib/                   # Utilities, API client
├── types/                 # TypeScript definitions
└── providers/             # Context providers
```

## Role-Based Views

| Role | Access |
|------|--------|
| **Sales Rep** | Own sales, commissions, goals, profile |
| **Manager** | Team views + everything above |
| **Finance** | Payroll batches, all commissions (read) |
| **Admin** | Full access including configuration |

## Key Features

- **Dashboard** - Role-specific metrics and charts
- **Sales List** - Filterable, sortable sales table
- **Commission Tracking** - View earnings by period, type
- **Goals** - Track progress toward targets
- **Payroll Management** - Create and export batches (Finance)
- **Admin Console** - Manage users, plans, rules (Admin)

## Available Scripts

```bash
npm run dev       # Start development server
npm run build     # Build for production
npm run start     # Start production server
npm run lint      # Run ESLint
npm run test      # Run tests
npm run test:e2e  # Run Playwright E2E tests
```

## Environment Variables

```env
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=your-secret-here
```

## Type Definitions

All API types are pre-defined in `src/types/index.ts` matching the backend API contract. Use these types throughout the application for type safety.

```tsx
import { Sale, Commission, PayrollBatch } from '@/types';
```

## License

Proprietary - All rights reserved
