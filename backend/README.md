# Wallet System API — Phase 1

Backend for the **Digital Wallet & Expense Management System** (educational project; no real money).

**Phase 1 scope (this repo state):**

- Clean Architecture solution layout (.NET 8)
- PostgreSQL database **schema** (domain entities, EF Core Fluent API, initial migration, BDT currency seed)
- **One** operational HTTP endpoint: `GET /health` (includes DB connectivity check)
- Serilog logging, correlation ID, global `ProblemDetails` error handling
- **Swagger UI** in Development only (`/` redirects to `/swagger`)

**Not included yet:** authentication, business APIs (auth, wallets, transfers, expenses), Mock Bank Service, frontend.

---



## Prerequisites


| Tool                                                           | Purpose                                   |
| -------------------------------------------------------------- | ----------------------------------------- |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Build and run (see `global.json`)         |
| [Docker](https://www.docker.com/)                              | Local PostgreSQL via `docker-compose.yml` |


If the SDK is installed under `~/.dotnet`, add it to your path:

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
```

For EF CLI tools:

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet tool install --global dotnet-ef --version 8.0.11
```

---



## Repository layout

```
backend/
├── WalletSystem.sln
├── global.json
├── Directory.Build.props          # shared build settings (nullable, warnings as errors)
├── Directory.Packages.props       # central NuGet versions
├── docker-compose.yml             # PostgreSQL only
└── src/
    └── WalletApp/                 # Single project (Classic MVC)
        ├── Controllers/           # [C] API Controllers (HealthController, etc.)
        ├── Models/                # [M] Database entities, enums, DTOs
        │   ├── Entities/          # Database models (User, Wallet, Transaction, etc.)
        │   ├── Enums/             # Enums (Role, WalletStatus, etc.)
        │   └── DTOs/              # Request/Response contracts
        ├── Data/                  # AppDbContext, Configurations, Migrations, Seed
        ├── Services/              # Business logic services
        ├── Middleware/            # Exception handling, CorrelationId
        └── Program.cs             # Application entry point
└── tests/
    ├── Wallet.UnitTests/          # Unit tests referencing WalletApp
    └── Wallet.IntegrationTests/   # Integration tests referencing WalletApp
```

---



## Quick start

All commands run from the `backend/` directory.

### 1. Start PostgreSQL

```bash
docker compose up -d
```

The container maps **host port** `5433` → container `5432` so it does not clash with a local PostgreSQL on `5432`.

Default credentials (development only):


| Setting  | Value         |
| -------- | ------------- |
| Host     | `localhost`   |
| Port     | `5433`        |
| Database | `wallet_db`   |
| User     | `wallet_user` |
| Password | `wallet_pass` |




### 2. Configure connection string

Default in `src/Wallet.Api/appsettings.json`:

`Host=localhost;Port=5433;Database=wallet_db;Username=wallet_user;Password=wallet_pass`

Override without editing files:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=wallet_db;Username=wallet_user;Password=wallet_pass"
```

Use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for passwords you do not want in config files. Do not commit secrets.

### 3. Build and apply migrations

```bash
dotnet restore
dotnet build

dotnet ef database update --project src/WalletApp/WalletApp.csproj
```

This creates tables, constraints, and seeds the **BDT** currency row.

### 4. Run the API

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/WalletApp/WalletApp.csproj
```

The app listens on **[http://localhost:8000](http://localhost:8000)** (see `Properties/launchSettings.json`).


| URL                                                                                            | Description                             |
| ---------------------------------------------------------------------------------------------- | --------------------------------------- |
| [http://localhost:8000/](http://localhost:8000/)                                               | Redirects to Swagger (Development only) |
| [http://localhost:8000/swagger](http://localhost:8000/swagger)                                 | Interactive API docs                    |
| [http://localhost:8000/swagger/v1/swagger.json](http://localhost:8000/swagger/v1/swagger.json) | OpenAPI document                        |
| [http://localhost:8000/health](http://localhost:8000/health)                                   | Liveness check (no database)            |


Swagger and the `/` redirect are **disabled outside Development**.

---



## HTTP surface (Phase 1)


| Method | Path                                   | Description                                                                |
| ------ | -------------------------------------- | -------------------------------------------------------------------------- |
| `GET`  | `/health`                              | Liveness probe; confirms the API process is up (does not query PostgreSQL) |
| `GET`  | `/`                                    | Redirect to `/swagger` (Development only)                                  |
| `GET`  | `/swagger`, `/swagger/v1/swagger.json` | API documentation (Development only)                                       |


No `/api/v1/...` business routes exist until later phases.

---



## API response envelope

All JSON API responses use the same shape (`Wallet.Application.Common.Models.ApiResponse<T>`):


| Property  | Type          | Description                                                      |
| --------- | ------------- | ---------------------------------------------------------------- |
| `status`  | number        | HTTP status code (also set on the response line)                 |
| `success` | boolean       | `true` for successful operations                                 |
| `message` | string        | Human-readable summary                                           |
| `data`    | object | null | Payload on success; optional on some failures (e.g. health)      |
| `errors`  | array | null  | Validation or domain issues; each item has `field` and `message` |


**Success example:**

```json
{
  "status": 200,
  "success": true,
  "message": "Request completed successfully.",
  "data": { },
  "errors": null
}
```

**Error example:**

```json
{
  "status": 404,
  "success": false,
  "message": "User not found.",
  "data": null,
  "errors": null
}
```

**Validation example:**

```json
{
  "status": 400,
  "success": false,
  "message": "One or more validation errors occurred.",
  "data": null,
  "errors": [
    { "field": "email", "message": "Email is required." }
  ]
}
```

Unhandled exceptions are converted to this format by `GlobalExceptionHandlerMiddleware` (no stack traces in Production).

Controllers (later phases) should return envelopes via `ApiResponseExtensions` on `ControllerBase` (`ApiOk`, `ApiCreated`, `ApiFail`).

---



## Database

- **Engine:** PostgreSQL
- **ORM:** EF Core 8 + Npgsql
- **Naming:** `snake_case` columns/tables (`EFCore.NamingConventions`)
- **Money:** `numeric(18,4)`; enums stored as strings; UTC timestamps
- **Migrations:** `src/Wallet.Infrastructure/Persistence/Migrations/`
- **Seed:** `BDT` currency in `Persistence/Seed/CurrencySeed.cs`

---



## Logging and errors

- **Serilog:** console + rolling files under `src/Wallet.Api/logs/` (when running the API project)
- **Correlation ID:** request header `X-Correlation-Id` (generated if missing); echoed on the response and added to log context
- **Errors:** `GlobalExceptionHandlerMiddleware` returns RFC 7807 `ProblemDetails` (no stack traces in Production)

---



## Adding features (later phases)

1. Define use cases and DTOs in `Wallet.Application`
2. Implement data access and integrations in `Wallet.Infrastructure`
3. Add controllers under `Wallet.Api/Controllers` with route prefix `/api/v1`
4. Add migrations only when the schema changes: `dotnet ef migrations add <Name> ...`

Keep business logic out of controllers; keep EF types out of `Wallet.Domain`.

---



## Troubleshooting


| Issue                               | What to try                                                                                                                                |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `role "wallet_user" does not exist` | Another Postgres is bound to port 5433, or compose is not running. Run `docker compose ps` and use port **5433** in the connection string. |
| `dotnet ef` not found               | Install global tool (see Prerequisites) and set `DOTNET_ROOT` / `PATH`.                                                                    |
| Swagger 404                         | Set `ASPNETCORE_ENVIRONMENT=Development`.                                                                                                  |
| Build warnings fail                 | `Directory.Build.props` treats warnings as errors; fix all warnings before commit.                                                         |


