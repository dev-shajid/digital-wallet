# Digital Wallet & Expense Management System

Context and PRD for the coding agent. Read this fully before writing any code.

---

## 1. Project Overview

A web-based **Digital Wallet & Expense Management System** built for educational/internship purposes. **No real money.** It simulates core ideas of digital financial apps: wallets, balances, transfers, transaction tracking, expenses, and role-based access.

The system is **fully automated**:

1. A new user registers and automatically gets one wallet in `BDT` with balance `0`.
2. The user adds funds by **cash-in from their bank**. The bank is a **separate Mock Bank Service** (own project, own database, own API). It returns `200 SUCCESS` for every valid request.
3. The user can **spend** from a wallet (**expense**, using admin-managed categories).
4. The user can **transfer** money to another registered user's wallet (**P2P transfer**).
5. Admin manages **currencies**, **wallets** (assign extra wallets to users), and **expense categories**, and monitors users and transactions.

### Architecture (high level)

```
Client (Frontend)  ->  Wallet API (this project)  ->  Wallet DB
                                  |
                                  +--> Bank Client (adapter) --HTTP--> Mock Bank Service (separate) -> Bank DB
```

- Wallet system **does not store bank accounts**. Bank account data lives only in the Mock Bank Service. Wallet DB stores only `bankCode` and `bankReference` per bank operation.
- All bank calls go through one adapter (`IBankClient`) so a different bank can be plugged in later.

### Current phase

**Phase 1 (now):** backend solution setup only — folder structure, schema (entities + EF Core configuration + initial migration), logging, health endpoints.
**Not in Phase 1:** auth, business endpoints, Mock Bank Service, frontend.

---

## 2. Roles

| Role | Can do |
|---|---|
| `USER` | Register, login, manage profile, view own wallets, cash-in, P2P transfer, add/view expenses, view own transactions and summaries |
| `ADMIN` | Everything below: manage currencies, manage expense categories (full CRUD), assign/manage user wallets, view users, wallets, transactions, basic system statistics |

A user can only access their own wallets, transactions and expenses. Regular users cannot access admin features.

---

## 3. Functional Requirements (PRD)

### 3.1 Auth & Users
- Register (name, email, password). Email unique. System generates a unique immutable `accountNo` (10 digits, random, last digit Luhn check digit).
- Login/logout with token-based auth. Protected endpoints reject unauthenticated calls.
- On registration, create the user **and** a BDT wallet in one DB transaction.

### 3.2 Currency (admin)
- Admin CRUD for currencies. Standard fields: ISO 4217 code, name, symbol, icon, decimal places, status.
- Currency in use is never hard-deleted; set `INACTIVE`.
- `BDT` is seeded.

### 3.3 Wallet
- A user can have **multiple wallets**, at most **one per currency**.
- Every user gets a BDT wallet by default. **Only admin** can add another wallet (another currency) for a user, and can freeze/close wallets.
- `FROZEN` wallet blocks all transactions. `CLOSED` requires balance = 0.

### 3.4 Cash-in (bank -> wallet)
- User provides bank details (`bankCode`, account number) with amount and target wallet. Wallet API calls Mock Bank (`debit`). On success, credit wallet; on failure, wallet unchanged and transaction `FAILED`.
- Order: create Transaction `PENDING` -> call bank -> bank success -> credit wallet -> `SUCCESS`.
- Bank account currency must match wallet currency.

### 3.5 P2P Transfer
- Sender picks own wallet and receiver by **`accountNo`** (not email) plus currency. Receiver wallet = receiver's wallet in the same currency.
- Rules: receiver exists, receiver != sender, amount > 0, sufficient balance, same currency, both wallets `ACTIVE`.
- Atomic: debit sender, credit receiver, record transaction, all in one DB transaction with row locks.
- Sender and receiver wallets are recorded as `wallet_logs` rows (`DEBIT` on sender, `CREDIT` on receiver) and a `P2PTransfer` row stores the receiver wallet for incoming-history queries.

### 3.6 Expense
- User records an expense from a wallet with amount, category, description, expense date.
- Category must be `ACTIVE`. Balance must be sufficient. Wallet debited atomically.
- **Categories are admin-managed only** (full CRUD). Users only select. Category referenced by any expense cannot be deleted (`ON DELETE RESTRICT`); set `INACTIVE` instead. Inactive category blocks new expenses but old expenses stay visible.
- Users can list/filter/search own expenses and see summaries.

### 3.7 Transactions
- Every money movement is a `Transaction` row owned by the initiating user (`userId`): reference, type, amount, currency, status, note, failure info. Wallet involvement is recorded in `wallet_logs`.
- **Transaction history (user):** transactions where `Transaction.userId` = the current user, **plus** incoming P2P transfers where `P2PTransfer.receiverWalletId` belongs to one of the user's wallets.
- **Wallet statement:** `wallet_logs` for that wallet ordered by `createdAt`.
- Admin views all transactions.
- **Failed transactions must record why**: `failureCode` (short enum-like code) and `failureReason` (detail, e.g. bank response) so admin can debug. Users see `failureCode` only; `failureReason` is admin-only.

### 3.8 Dashboards
- User: balances per wallet, recent transactions, recent expenses, expense summary.
- Admin: total users, total transactions, wallet statistics, basic expense statistics.

### 3.9 Errors
- Consistent error format (RFC 7807 `ProblemDetails`). Meaningful messages for: invalid input, invalid credentials, insufficient balance, unauthorized, not found, system error.

---

## 4. Database Design (final)

Database: **PostgreSQL**. Naming: PascalCase entities in C#, `snake_case` tables/columns in DB. IDs are `uuid`. Money is `numeric(18,4)`. Timestamps are UTC. Enums are stored as **strings**.

### Enums

```
Role                 : USER, ADMIN
CurrencyStatus       : ACTIVE, INACTIVE
WalletStatus         : ACTIVE, FROZEN, CLOSED
TransactionType      : CASH_IN, CASH_OUT, P2P_TRANSFER, EXPENSE
TransactionStatus    : PENDING, SUCCESS, FAILED
CategoryStatus       : ACTIVE, INACTIVE
WalletLogDirection   : DEBIT, CREDIT
```

`CASH_OUT` exists in the enum for future use; not implemented now.

### Tables

**User**
```
id            uuid PK
name          string
email         string  UNIQUE
accountNo     string  UNIQUE, immutable, system-generated
passwordHash  string
role          Role
createdAt, updatedAt
```

**Currency**
```
id            uuid PK
code          string UNIQUE, immutable   (ISO 4217, e.g. BDT, USD)
name          string
symbol        string
countryCode       string
decimalPlaces int
status        CurrencyStatus
createdAt, updatedAt
```
`BDT` is seeded with a fixed `id` for deterministic migrations and tests.

**Wallet**
```
id            uuid PK
userId        uuid FK -> User.id
currencyId    uuid FK -> Currency.id   ON DELETE RESTRICT
balance       decimal
status        WalletStatus
createdAt, updatedAt

UNIQUE (userId, currencyId)
CHECK  balance >= 0
```

**Transaction** (belongs to the **user** who initiated it; wallet legs are in `wallet_logs`)
```
id            uuid PK
userId        uuid FK -> User.id
currencyId    uuid FK -> Currency.id   ON DELETE RESTRICT
type          TransactionType
amount        decimal
status        TransactionStatus
reference     string UNIQUE
note          string NULL       (user-provided)
failureCode   string NULL
failureReason string NULL       (admin-only detail)
createdAt, updatedAt

INDEX (userId, createdAt)
CHECK amount > 0
CHECK status <> 'FAILED' OR failureReason IS NOT NULL
```

**WalletLog** (`wallet_logs` — one row per wallet involved in a transaction)
```
id             uuid PK
walletId       uuid FK -> Wallet.id        ON DELETE RESTRICT
transactionId  uuid FK -> Transaction.id   ON DELETE RESTRICT
direction      WalletLogDirection
amount         numeric(18,4)
balanceBefore  numeric(18,4) NULL
balanceAfter   numeric(18,4) NULL
createdAt      timestamp UTC

UNIQUE (transactionId, walletId)
INDEX (walletId, createdAt)
CHECK amount > 0
CHECK (balanceBefore IS NULL) = (balanceAfter IS NULL)
CHECK balanceAfter >= 0 when not null
```
Created with the `PENDING` transaction. `balanceBefore` / `balanceAfter` are set only when the leg is applied. Rows are append-only (never deleted; balances updated at most once). Currency comes from `Transaction.currencyId`; wallet currency must match (application rule).

**BankTransfer** (1:1 with Transaction, for `CASH_IN` / `CASH_OUT`)
```
transactionId uuid PK, FK -> Transaction.id
bankCode      string
bankReference string            (id returned by the bank service)
```
The affected wallet is identified via `wallet_logs`.

**P2PTransfer** (1:1 with Transaction, for `P2P_TRANSFER`)
```
transactionId    uuid PK, FK -> Transaction.id
receiverWalletId uuid FK -> Wallet.id   ON DELETE RESTRICT
INDEX (receiverWalletId)
```
Sender user = `Transaction.userId`. Sender wallet = the `DEBIT` row in `wallet_logs`. Receiver user = `Wallet(receiverWalletId).userId`. Application rules (ledger service): sender wallet ≠ `receiverWalletId`; `receiverWalletId` equals the `walletId` of the `CREDIT` row in `wallet_logs`; both wallets share `Transaction.currencyId`.

**ExpenseCategory**
```
id          uuid PK
name        string UNIQUE
description string
status      CategoryStatus
createdAt, updatedAt
```

**Expense** (1:1 with Transaction, for `EXPENSE`; amount/description come from Transaction)
```
transactionId uuid PK, FK -> Transaction.id
categoryId    uuid FK -> ExpenseCategory.id   ON DELETE RESTRICT
expenseDate   date
```
The debited wallet is identified via `wallet_logs`.

### Relationships

```
User 1 ---- N Wallet
User 1 ---- N Transaction
Currency 1 ---- N Wallet
Currency 1 ---- N Transaction
Wallet 1 ---- N WalletLog
Transaction 1 ---- N WalletLog
Transaction 1 ---- 0..1 BankTransfer
Transaction 1 ---- 0..1 P2PTransfer
Transaction 1 ---- 0..1 Expense
Wallet 1 ---- N P2PTransfer               (as receiver)
ExpenseCategory 1 ---- N Expense
```

### Design notes
- `Transaction` holds the initiating user and currency; per-wallet amounts and balance snapshots live in `wallet_logs`.
- Amount and user note for expenses live on `Transaction`.
- P2P sender wallet is the `DEBIT` row in `wallet_logs`; receiver wallet is the `CREDIT` row and is also stored on `P2PTransfer.receiverWalletId` for efficient incoming-transfer history.
- `Wallet.balance` is a deliberate denormalization for performance and atomic locking. It must always equal the `balanceAfter` of the latest **applied** `wallet_logs` row for that wallet. Only `IWalletLedger` writes `wallet_logs` and `Wallet.balance`.
- Child tables use `transactionId` as PK to guarantee 1:1.

---

## 5. Non-Functional Requirements

- **Consistency:** every balance change happens inside a DB transaction with row-level locking (`SELECT ... FOR UPDATE`) or optimistic concurrency. No lost updates, no negative balance. **Invariant:** `Wallet.balance` = latest applied `balanceAfter` on that wallet = sum of applied `CREDIT` amounts minus sum of applied `DEBIT` amounts in `wallet_logs` (reconciliation test in a later phase).
- **Security:** password hashing (ASP.NET Core Identity `PasswordHasher` or BCrypt/Argon2), JWT auth, role-based authorization, no secrets in source control, input validation on every endpoint.
- **Maintainability:** clean layering, small classes, no business logic in controllers, no EF Core leaking into the Domain layer.
- **Observability:** structured logging, correlation id per request, health endpoints.
- **Errors:** global exception handling middleware returning `ProblemDetails`. Never expose stack traces or `failureReason` to normal users.
- **Testing:** unit tests for domain/application rules, integration tests for endpoints. Cover valid ops, invalid input, unauthorized access, insufficient balance, invalid transfers, data-access restrictions.

---

## 6. Tech Stack & Conventions

- **.NET (latest LTS)**, ASP.NET Core Web API, C# with nullable enabled, file-scoped namespaces.
- **Architecture**: Single-project Classic MVC (`src/WalletApp`) with clear folders: `Controllers/`, `Models/` (Entities, Enums, DTOs), `Data/` (DbContext, Configurations, Migrations), and `Services/`.
- **EF Core** with Npgsql, Fluent API configurations in separate `IEntityTypeConfiguration<T>` classes (no data annotations for mapping). `EFCore.NamingConventions` for snake_case.
- **Serilog** for logging (console + rolling file), request logging, correlation id.
- **Health checks**: liveness and readiness (DB).
- **OpenAPI/Swagger** enabled in Development.
- Controllers-based API, versioned route prefix `/api/v1`.
- Central package management (`Directory.Packages.props`), `Directory.Build.props` for shared settings.
- Configuration via `appsettings.json` + environment variables + user-secrets. Never commit secrets.
- Git: feature branches, small commits, conventional commit messages.

---

## 7. Out of Scope

Real payments, real bank integration, cross-currency transfer / exchange rates (same-currency only for now), cash-out implementation, notifications, KYC.