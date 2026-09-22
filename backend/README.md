# Wallet System API — Phase 1

Backend for the **Digital Wallet & Expense Management System** (educational project; no real money).

This README also has short notes for anyone coming from Node.js, since .NET has
different names for the same ideas.

**Phase 1 scope (this repo state):**

- Clean Architecture solution layout (.NET 10)
- PostgreSQL database **schema** (domain entities, EF Core Fluent API config, initial migration, BDT currency seed)
- Two health endpoints: `GET /health/live` (process is up) and `GET /health/ready` (process is up AND can reach Postgres)
- Serilog logging (console + rolling file), correlation id per request, global exception handling (`ProblemDetails`)
- **Swagger UI** in Development only, generated automatically from the code

**Not included yet:** authentication, business APIs (wallets, transfers, expenses), Mock Bank Service, frontend. Those come in later phases.

---

## Solution layout, and what it maps to in Node

| Folder | What's in it | Closest Node.js equivalent |
|---|---|---|
| `src/Wallet.Domain` | Entities (`User`, `Wallet`, `Transaction`, ...) and enums. No database code, no framework references - plain C# classes. | Your Mongoose/Prisma **model shapes**, but without the ORM-specific bits. |
| `src/Wallet.Application` | Interfaces and use-case contracts for later phases. Empty for now (Phase 1 has no business logic yet). | Your `services/` or `use-cases/` folder's **type definitions**, before you write the implementations. |
| `src/Wallet.Infrastructure` | `WalletDbContext` (EF Core), one `IEntityTypeConfiguration<T>` per entity, migrations, BDT seed data. | Your Prisma/TypeORM/Sequelize **schema + client + migrations**. |
| `src/Wallet.Api` | `Program.cs` (the entry point - like `index.js`/`app.js`), logging, health checks, Swagger, future Controllers. | Your **Express app**: entry file, middleware, routes. |
| `tests/Wallet.UnitTests` | Tests that don't touch a real database. | Jest/Mocha unit tests. |
| `tests/Wallet.IntegrationTests` | Tests that boot the real API. | Supertest-style integration tests. |

Each folder above is its own **project** (`.csproj` file) - think of each one as a
separate `package.json` in a monorepo. `WalletSystem.sln` is the "workspace" file that
groups them, and dependencies only flow one way: `Api → Infrastructure/Application → Domain`.
`Domain` never depends on anything else - that's what keeps business rules independent
of the database/framework (so, e.g., the `Wallet` entity has no idea Postgres exists).

**Naming note:** the C# namespace is `WalletSystem.*` (e.g. `WalletSystem.Domain`), not
`Wallet.*` like the folder/project names. That's deliberate - one of the domain
entities is itself called `Wallet`, and having a namespace segment with the exact same
name as a class can create confusing compiler errors, so the namespace root was kept
distinct.

---

## Prerequisites

| Tool | Purpose |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Build and run (see `global.json`) |
| [Docker](https://www.docker.com/) | Local PostgreSQL via `docker-compose.yml` |

Quick vocabulary if you're new to .NET:

| .NET term | Node.js equivalent |
|---|---|
| NuGet package | npm package |
| `Directory.Packages.props` | your root `package.json` `dependencies` + a lockfile, shared by every project |
| `.csproj` file | a workspace package's `package.json` |
| `dotnet restore` | `npm install` |
| `dotnet build` | `tsc` (type-check/compile) |
| `dotnet run` | `npm run dev` |
| `dotnet test` | `npm test` |
| EF Core | Prisma / TypeORM / Sequelize |
| `dotnet ef migrations add X` | `prisma migrate dev --name X` |
| `dotnet ef database update` | applying pending migrations to your dev DB |

---

## Quick start

All commands run from the `backend/` directory.

### 1. Start PostgreSQL

```bash
docker compose up -d
```

| Setting | Value |
|---|---|
| Host | `localhost` |
| Port | `5433` (mapped so it won't clash with a local Postgres on 5432) |
| Database | `wallet_db` |
| User | `wallet_user` |
| Password | `wallet_pass` |

### 2. Connection string

Already set in `src/Wallet.Api/appsettings.json` (`ConnectionStrings:Default`) to match
the docker-compose values above. Override without editing files:

```bash
export ConnectionStrings__Default="Host=localhost;Port=5433;Database=wallet_db;Username=wallet_user;Password=wallet_pass"
```

(Double underscore `__` is how .NET reads nested config keys from an environment
variable - the same idea as `DATABASE_URL` in a `.env` file, just a different naming
convention.) Use [user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
for anything you don't want in a config file. Never commit real secrets.

### 3. Install the EF Core CLI tool (one-time)

```bash
dotnet tool install --global dotnet-ef --version 10.0.12
```

### 4. Build and apply the migration

```bash
dotnet restore
dotnet build

dotnet ef database update --project src/Wallet.Infrastructure --startup-project src/Wallet.Api
```

This creates all 9 tables, their constraints/indexes, and seeds the **BDT** currency row.

### 5. Run the API

```bash
dotnet run --project src/Wallet.Api
```

The app listens on **http://localhost:8000** (see `src/Wallet.Api/Properties/launchSettings.json`).

| URL | Description |
|---|---|
| http://localhost:8000/ | Redirects to Swagger (Development only) |
| http://localhost:8000/swagger | Interactive API docs |
| http://localhost:8000/health/live | Liveness - is the process running |
| http://localhost:8000/health/ready | Readiness - process running AND Postgres reachable |

### 6. Run the tests

```bash
dotnet test
```

---

## Database

- **Engine:** PostgreSQL
- **ORM:** EF Core 10 + Npgsql
- **Naming:** `snake_case` tables/columns (`EFCore.NamingConventions`), even though the C# classes are PascalCase
- **Money:** `numeric(18,4)` - never a floating-point type, so amounts never lose precision
- **Enums:** stored as text (e.g. `"ACTIVE"`), not numbers, so the raw table data stays readable
- **Migrations:** `src/Wallet.Infrastructure/Persistence/Migrations/`
- **Seed:** BDT currency, `src/Wallet.Infrastructure/Persistence/Seed/CurrencySeed.cs`
- **Entity configuration:** one file per table under `src/Wallet.Infrastructure/Persistence/Configurations/` (Fluent API, not data-annotation attributes on the entities themselves - keeps `Wallet.Domain` free of any EF Core reference)

Foreign keys are all `ON DELETE RESTRICT`: nothing referenced by another row (a
`Currency` in use, a `Wallet` with transactions, an `ExpenseCategory` with expenses,
...) can be deleted out from under it. This is deliberate for a financial ledger - see
`AGENTS.md` section 4 for the full schema.

---

## Logging and errors

- **Serilog:** console + rolling daily files under `src/Wallet.Api/logs/`
- **Correlation id:** every request gets an `X-Correlation-Id` response header (reused if the caller already sent one) and it's attached to every log line for that request
- **Errors:** any unhandled exception becomes an RFC 7807 `ProblemDetails` JSON response (`src/Wallet.Api/Middleware/GlobalExceptionHandler.cs`) - no stack traces are ever sent to the client

---

## Adding features (later phases)

1. Define interfaces/use cases in `Wallet.Application`
2. Implement data access/integrations in `Wallet.Infrastructure`
3. Add controllers under `Wallet.Api/Controllers` with route prefix `/api/v1`
4. Add a migration whenever the schema changes:
   ```bash
   dotnet ef migrations add <Name> --project src/Wallet.Infrastructure --startup-project src/Wallet.Api
   ```

Keep business logic out of controllers; keep EF Core types out of `Wallet.Domain`.

---

## Troubleshooting

| Issue | What to try |
|---|---|
| `relation "..." already exists` when running `database update` | The Postgres volume already has old data from a previous attempt. Reset it: `docker compose down -v && docker compose up -d`, then re-run the migration. |
| `dotnet ef` not found | Run the install command in step 3 above, and make sure `~/.dotnet/tools` is on your `PATH`. |
| `dotnet ef` version mismatch errors | The global tool version must match the EF Core version in `Directory.Packages.props` (currently 10.0.12): `dotnet tool update --global dotnet-ef --version 10.0.12`. |
| Swagger 404 | Set `ASPNETCORE_ENVIRONMENT=Development` (already the default via `launchSettings.json` when using `dotnet run`). |
| Port 8000 already in use | Something else is already listening on it - stop that process, or pass `--urls http://localhost:<port>` to `dotnet run`. |
