# Project Tasks & Milestones

Digital Wallet & Expense Management System. Task breakdown for parallel team work.

The database design is final and shared by everyone (see `AGENTS.md`, section 4). Tasks below are split so that people inside the same milestone can work in parallel with no blocking. Dependencies are listed per task.

---

## How to use this document

- One task = one owner = one feature branch = one or more small PRs.
- Every task must meet the **Definition of Done** at the bottom.
- Do not change the database schema inside a task. Raise it with the team first.
- Do not touch another task's module. Use the shared interfaces from Milestone 0.

---



## Milestone 0: Shared Contract

Agree on these together before splitting work. Everything else depends on them.


| Item                                | Decision                                                                                                                                                  |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ICurrentUser` (Application layer)  | Exposes `UserId` and `Role`. Used by all modules. Until Auth is ready, use a development stub.                                                            |
| `IWalletLedger` (Application layer) | The only way to change a balance or a transaction status. Operations never update `Wallet.balance` directly. Defined under Task 3.                        |
| Failure codes                       | `INSUFFICIENT_BALANCE`, `WALLET_FROZEN`, `WALLET_NOT_FOUND`, `RECEIVER_NOT_FOUND`, `CURRENCY_MISMATCH`, `CATEGORY_INACTIVE`, `BANK_ERROR`, `SYSTEM_ERROR` |
| Mock Bank API                       | All endpoints return `200` with success. Contract below.                                                                                                  |
| Error format                        | RFC 7807 `ProblemDetails`                                                                                                                                 |
| Pagination format                   | Request: `page`, `pageSize`. Response: `items`, `page`, `pageSize`, `total`.                                                                              |
| Git workflow                        | Feature branch per task, PR with at least one review, squash merge into `main`.                                                                           |




### Mock Bank API contract

```
POST /api/bank/debit      bank account -> wallet   (cash-in)
POST /api/bank/withdraw   wallet -> bank account   (cash-out, future)
POST /api/bank/refund

Request : { bankCode, accountNumber, amount, currency, reference }
Response: { bankReference, status: "SUCCESS" }
```

---



## Milestone 1: Foundation

All tasks in this milestone are independent of each other.

### Task 1: Auth & User

**Scope**

- Register, login, get current user (`/auth/register`, `/auth/login`, `/auth/me`).
- JWT authentication, role-based authorization (`USER`, `ADMIN`).
- Password hashing.
- `accountNo` generator: 10 digits, random, last digit is a Luhn check digit, unique, immutable, behind an interface.
- Register creates the `User` and one BDT `Wallet` (balance 0) in a single DB transaction.
- Seed an admin user from configuration (user-secrets / environment variables).
- Real implementation of `ICurrentUser`.

**Depends on:** nothing.

### Task 2: Currency & Wallet

**Scope**

- Currency: admin CRUD. A currency in use cannot be deleted (`409`); set `INACTIVE` instead. Authenticated users can read active currencies. API routes may use ISO `code` in URLs; internally wallets and transactions reference `currencyId`.
- User: list own wallets, get own wallet by id. Another user's wallet returns `404`.
- Admin: list a user's wallets, assign a new wallet to a user (currency must be `ACTIVE`, one wallet per user per currency, otherwise `409`).
- Admin: change wallet status `ACTIVE <-> FROZEN`. `CLOSED` only when balance is 0 and is final.

**Depends on:** Task 1 for `[Authorize]` only. Use the `ICurrentUser` stub until then.

### Task 3: Transaction Core

**Scope**

- `IWalletLedger` is the **only** writer of `wallet_logs` and `Wallet.balance`.
- `IWalletLedger` implementation:
  - `CreatePending(userId, currencyId, type, amount, note, legs)` — `legs` is a list of `{ walletId, direction, amount }`; creates the `PENDING` transaction and one `wallet_logs` row per leg (balances not applied yet).
  - `ApplyLeg(walletLogId)` — row-level lock on the wallet, enforce rules, set `balanceBefore` / `balanceAfter`, update `Wallet.balance`.
  - `MarkSuccess(transactionId)` / `MarkFailed(transactionId, failureCode, failureReason)`.
- Apply rules: wallet must be `ACTIVE`, balance must never go below 0, wallet currency must match `Transaction.currencyId`.
- Unique `reference` generator.
- User: `GET /transactions` (filter by type, status, wallet, date range; paginated) and `GET /transactions/{id}`. Returns `failureCode`, never `failureReason`.
- Admin: `GET /admin/transactions` and detail, including `failureReason`.

**Depends on:** nothing (schema only). This is the **critical path**: Milestone 2 depends on it. Assign it first.

### Task 4: Mock Bank Service

**Scope**

- Separate project, separate database, separate API.
- Implement the Mock Bank API contract from Milestone 0.
- Store its own records (accounts, operations) in its own database.
- Idempotent by `reference`.

**Depends on:** nothing.

### Task 5: Frontend (optional, separate owner)

**Scope**

- Start from the Swagger/OpenAPI spec and mock data. Integrate with real endpoints as they become available.

**Depends on:** API contract only.

---



## Milestone 2: Operations

Tasks 6, 7 and 8 are independent of each other. They only use `IWalletLedger`.

### Task 6: Cash-in

**Scope**

- `IBankClient` adapter (the only place that calls the Mock Bank). A different bank can be plugged in later.
- `POST /cash-in`.
- Flow: create `PENDING` -> call bank debit -> on success credit wallet and mark `SUCCESS` -> on failure mark `FAILED` with `BANK_ERROR` and the bank response in `failureReason`. Wallet is unchanged on failure.
- Save `BankTransfer` row (`bankCode`, `bankReference`).
- Bank account currency must match the wallet currency.

**Depends on:** Tasks 1, 2, 3, 4.

### Task 7: P2P Transfer

**Scope**

- `POST /transfers`. Receiver is found by `accountNo` and currency (never by email).
- Rules: receiver exists, receiver is not the sender, amount > 0, same currency, both wallets `ACTIVE`, sufficient balance.
- One DB transaction. Lock both wallets in a fixed order (by id) to avoid deadlocks.
- Through `IWalletLedger`: create the `Transaction`, the `P2PTransfer` row (`receiverWalletId`), and sender `DEBIT` / receiver `CREDIT` legs in `wallet_logs`.
- Failed attempts are recorded with the proper `failureCode`.

**Depends on:** Tasks 1, 2, 3.

### Task 8: Expense & Category

**Scope**

- Category: admin CRUD. A category used by any expense cannot be deleted; set `INACTIVE` instead. Inactive categories block new expenses but old expenses stay visible.
- User: `POST /expenses` (active category, sufficient balance, wallet debited atomically), list with filter and search, expense summary.
- Save `Expense` row.
- Users can only access their own expenses.

**Depends on:** Tasks 1, 2, 3.

---



## Milestone 3: Dashboards & Admin Monitoring



### Task 9: User Dashboard

**Scope**

- Balance per wallet, recent transactions, recent expenses, expense summary by category.

**Depends on:** Tasks 2, 3, 8.

### Task 10: Admin Dashboard & User Management

**Scope**

- Statistics: total users, total transactions, wallet statistics, expense statistics.
- User list with search (name, email, `accountNo`), user detail.

**Depends on:** Tasks 1, 2, 3, 8.

---



## Milestone 4: Hardening & Delivery

Whole team.

- Integration tests: unauthorized access, data access restrictions, insufficient balance, invalid transfers, frozen wallet, bank failure.
- Concurrency test: parallel requests on the same wallet must never produce a negative balance.
- Bug reports and resolutions documented.
- README, API documentation, database design, technical decisions.
- Final presentation.

---



## Dependency Map

```
Milestone 0 (contract)
   |
   +-- Task 1  Auth & User ----------------+
   +-- Task 2  Currency & Wallet ----------+--> Task 6  Cash-in  <-- Task 4 Mock Bank
   +-- Task 3  Transaction Core -----------+--> Task 7  P2P Transfer
   +-- Task 4  Mock Bank Service           +--> Task 8  Expense & Category
   +-- Task 5  Frontend (optional)                 |
                                                   +--> Task 9, 10 Dashboards
```

---



## Definition of Done (every task)

- `dotnet build` passes with no warnings.
- Unit and integration tests written and passing.
- Input validation on every endpoint.
- Access rules enforced: users only see their own data, admin endpoints reject `USER`.
- Every controller action has an XML summary and `[ProducesResponseType]` for each possible status code, so Swagger stays accurate.
- Important events logged with the correlation id. No passwords or tokens in logs.
- README or docs updated where needed.
- PR reviewed and merged into `main`.

