# Digital Wallet & Expense Management System

Educational project — a wallet app that simulates cash-in, P2P transfers and expense
tracking. No real money, no real bank.

## Stack

- **Backend:** .NET Web API, EF Core, PostgreSQL — see [backend/README.md](backend/README.md) for setup
- **Frontend:** Next.js — see [frontend/README.md](frontend/README.md) for setup

## Docs

| File | What's in it |
|---|---|
| [PRD.md](PRD.md) | Full product spec — roles, features, schema design, rules |
| [database-schema.md](database-schema.md) | Actual DB tables and columns |
| [docs.md](docs.md) | API endpoints that are built so far, with payloads |
| [TASKS.md](TASKS.md) | Task breakdown and who's building what |

Individual feature task briefs (for whoever's picking them up): `P2P_TRANSFER_TASK.md`, `EXPENSE_TASK.md`.

## Getting started

```bash
cd backend && docker compose up -d && dotnet ef database update --project src/Wallet.Infrastructure --startup-project src/Wallet.Api && dotnet run --project src/Wallet.Api
cd frontend && npm install && npm run dev
```

Backend runs on `http://localhost:8000`, frontend on `http://localhost:3000`. Full steps (env vars, migrations, troubleshooting) are in `backend/README.md`.

## Contributing

- Don't commit directly to `main`. Branch per feature: `<yourname>/<short-description>`.
- Small commits, clear messages.
- Open a PR, get at least one review before merging.
- Read `PRD.md` before building a new feature — it's the source of truth for how things should work, not just this README.
- Added or changed an endpoint or table? Update `docs.md` / `database-schema.md` in the same PR.
- Backend: keep business logic out of controllers, keep EF Core out of `Wallet.Domain`.
- Never change the DB schema without the team knowing — open a migration in its own commit.
