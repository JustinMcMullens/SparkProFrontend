# Claude Code Subagents

## Your Team

| Agent | Color | Role |
|-------|-------|------|
| 🟣 **engineering-lead** | Purple | Architecture, code review, coordination |
| 🟢 **backend-agent** | Green | All .NET/C# backend work |
| 🟠 **frontend-agent** | Orange | All Next.js frontend work |
| 🟡 **project-manager** | Yellow | Task tracking, status updates |

## Quick Start

```bash
# View available agents
/agents

# Explicitly use an agent
> Use backend-agent to implement the Commission Engine

# Or just describe your task — Claude picks the right agent
> Build the sales list page with filters and pagination
```

## Recommended Workflow

### 1. Plan the work
```
Use engineering-lead to break down the payroll feature into tasks
```

### 2. Implement backend
```
Have backend-agent implement the payroll batch endpoints
```

### 3. Implement frontend
```
Have frontend-agent build the payroll batch UI
```

### 4. Review
```
Use engineering-lead to review the implementation
```

### 5. Track progress
```
Have project-manager update PROJECT_STATUS.md
```

## Running in Parallel

Open multiple terminals:

**Terminal 1 — Backend:**
```bash
cd commission-platform-api
claude
> Use backend-agent to implement Commission Engine
```

**Terminal 2 — Frontend:**
```bash
cd commission-platform-web
claude
> Use frontend-agent to build the dashboard
```

The background colors help you tell which agent is running in each terminal.

## Key Docs Agents Reference

- `/docs/ARCHITECTURE.md` — System design
- `/docs/COMMISSION_ENGINE.md` — Calculation logic
- `/docs/API_CONTRACT.md` — API specification
- `/docs/DATABASE_SCHEMA.sql` — Data model
- `/docs/FRONTEND_STRUCTURE.md` — UI structure (frontend repo)
