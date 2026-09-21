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

### 3.6 Expense
- User records an expense from a wallet with amount, category, description, expense date.
- Category must be `ACTIVE`. Balance must be sufficient. Wallet debited atomically.
- **Categories are admin-managed only** (full CRUD). Users only select. Category referenced by any expense cannot be deleted (`ON DELETE RESTRICT`); set `INACTIVE` instead. Inactive category blocks new expenses but old expenses stay visible.
- Users can list/filter/search own expenses and see summaries.

### 3.7 Transactions
- Every money movement is a `Transaction` row: reference, type, amount, currency, status, note, failure info.
- Users view and filter own transaction history. Admin views all.
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
Role              : USER, ADMIN
CurrencyStatus    : ACTIVE, INACTIVE
WalletStatus      : ACTIVE, FROZEN, CLOSED
TransactionType   : CASH_IN, CASH_OUT, P2P_TRANSFER, EXPENSE
TransactionStatus : PENDING, SUCCESS, FAILED
CategoryStatus    : ACTIVE, INACTIVE
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
code          string PK        (ISO 4217, e.g. BDT, USD)
name          string
symbol        string
iconUrl       string
decimalPlaces int
status        CurrencyStatus
createdAt, updatedAt
```

**Wallet**
```
id            uuid PK
userId        uuid FK -> User.id
currencyCode  string FK -> Currency.code
balance       decimal
status        WalletStatus
createdAt, updatedAt

UNIQUE (userId, currencyCode)
UNIQUE (id, currencyCode)          -- target of composite FK from Transaction
CHECK  balance >= 0
```

**Transaction** (belongs to a **wallet**, not directly to a user; user is derived via Wallet)
```
id            uuid PK
walletId      uuid
currencyCode  string
type          TransactionType
amount        decimal
status        TransactionStatus
reference     string UNIQUE
note          string NULL       (user-provided)
failureCode   string NULL
failureReason string NULL       (admin-only detail)
createdAt, updatedAt

FK (walletId, currencyCode) -> Wallet (id, currencyCode)   -- composite, guarantees currency matches wallet
CHECK amount > 0
CHECK status <> 'FAILED' OR failureReason IS NOT NULL
```

**BankTransfer** (1:1 with Transaction, for `CASH_IN` / `CASH_OUT`)
```
transactionId uuid PK, FK -> Transaction.id
bankCode      string
bankReference string            (id returned by the bank service)
```

**P2PTransfer** (1:1 with Transaction, for `P2P_TRANSFER`)
```
transactionId    uuid PK, FK -> Transaction.id
receiverWalletId uuid FK -> Wallet.id
```
Sender wallet = `Transaction.walletId`. Sender != receiver is enforced in the application layer.

**ExpenseCategory**
```
id          uuid PK
name        string UNIQUE
description string
status      CategoryStatus
createdAt, updatedAt
```

**Expense** (1:1 with Transaction, for `EXPENSE`; amount/description/wallet come from Transaction)
```
transactionId uuid PK, FK -> Transaction.id
categoryId    uuid FK -> ExpenseCategory.id   ON DELETE RESTRICT
expenseDate   date
```

### Relationships

```
User 1 ---- N Wallet
Currency 1 ---- N Wallet
Wallet 1 ---- N Transaction               (composite FK with currencyCode)
Transaction 1 ---- 0..1 BankTransfer
Transaction 1 ---- 0..1 P2PTransfer
Transaction 1 ---- 0..1 Expense
Wallet 1 ---- N P2PTransfer               (as receiver)
ExpenseCategory 1 ---- N Expense
```

### Design notes
- No redundancy on purpose: user derives from `Wallet`, sender wallet derives from `Transaction`, amount/description for expenses live on `Transaction`.
- `Wallet.balance` is a deliberate denormalization for performance and atomic locking. Always update it in the same DB transaction as the `Transaction` row.
- Child tables use `transactionId` as PK to guarantee 1:1.

---

## 5. Non-Functional Requirements

- **Consistency:** every balance change happens inside a DB transaction with row-level locking (`SELECT ... FOR UPDATE`) or optimistic concurrency. No lost updates, no negative balance.
- **Security:** password hashing (ASP.NET Core Identity `PasswordHasher` or BCrypt/Argon2), JWT auth, role-based authorization, no secrets in source control, input validation on every endpoint.
- **Maintainability:** clean layering, small classes, no business logic in controllers, no EF Core leaking into the Domain layer.
- **Observability:** structured logging, correlation id per request, health endpoints.
- **Errors:** global exception handling middleware returning `ProblemDetails`. Never expose stack traces or `failureReason` to normal users.
- **Testing:** unit tests for domain/application rules, integration tests for endpoints. Cover valid ops, invalid input, unauthorized access, insufficient balance, invalid transfers, data-access restrictions.

---

## 6. Tech Stack & Conventions

- **.NET (latest LTS)**, ASP.NET Core Web API, C# with nullable enabled, file-scoped namespaces.
- **Clean Architecture**: `Domain` <- `Application` <- `Infrastructure` <- `Api`. Dependencies point inward. Domain has no package references.
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