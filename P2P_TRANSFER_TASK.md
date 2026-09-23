# Task: P2P Money Transfer (Send Money by Account Number)

**Scope for this task:** a user sends money to another registered user by typing that
user's **account number** and an **amount**. **Currency is fixed to BDT for every
transfer right now** — there is no currency picker and no sender-wallet picker,
because every user only has their one default BDT wallet at this stage (extra wallets
are an admin-only feature that doesn't exist yet). Keep the whole feature this simple;
don't build multi-currency or multi-wallet selection now.

This is a full-stack task: backend endpoints + frontend page. Read this whole file
before writing code — it tells you exactly what already exists so you don't rebuild it
or diverge from it.

---

## 1. What already exists (don't rebuild these)

- **Auth is done and working**: register, login, refresh, logout. JWT contains the
  user's id (`sub` claim). `ClaimsPrincipalExtensions.GetUserId()` in
  `backend/src/Wallet.Api/Common/ClaimsPrincipalExtensions.cs` reads it.
- **Every user already has exactly one BDT wallet**, created atomically at registration
  (`AuthService.RegisterAsync`). You never need to create a wallet in this task.
- **Account numbers** are already generated as `"AC" + 8-digit zero-padded sequence`
  (e.g. `AC00000001`) — see `AccountNumberGenerator.cs`. They are **not** the 10-digit
  Luhn format mentioned in the original PRD; go with what's actually implemented.
- **Schema is already migrated** — `Wallet`, `Transaction`, `WalletLog`, `P2PTransfer`
  entities, EF configurations, and migrations all exist already
  (`backend/src/Wallet.Domain/Entities/*`, `backend/src/Wallet.Infrastructure/Persistence/*`).
  **Do not add a migration or change these entities** for this task — everything you
  need is already there.
- **Response envelope**: every endpoint returns `ApiResponse<T>` (`success`, `status`,
  `message`, `data`, `errors`). Use the `controller.ApiOk(...)`, `controller.ApiCreated(...)`,
  `controller.ApiFail(...)` helpers in `Wallet.Api/Common/ApiResponseExtensions.cs` —
  don't hand-build response objects in the controller.
- **Routing**: controllers don't include `api/v1` in their `[Route]` — it's added
  automatically by `ApiPrefixConvention` (see `Program.cs`). So `[Route("wallets")]`
  actually serves `/api/v1/wallets`.
- **Frontend auth/session plumbing** (`src/store/auth-store.ts`, `src/lib/axios.ts`,
  `src/hooks/use-auth.ts`) is done — the axios instance already attaches the bearer
  token and handles refresh-on-401. Just add new API functions/hooks on top of it,
  don't touch the interceptor.

## 2. What does NOT exist yet (you must build it in this task)

- No `IWalletLedger` / generic ledger service yet (that's a separate, larger task
  planned for later and shared across cash-in/P2P/expense). **For this task, implement
  the debit/credit + `wallet_logs` writes directly inside the transfer service** — don't
  block on the generic ledger abstraction. Keep the logic isolated in one service class
  so it's easy to refactor into `IWalletLedger` later without touching the controller.
- No wallet-read endpoint yet (`GET /wallets`) — you need to add a minimal one so the
  frontend can show the user's current BDT balance.
- No transaction/transfer history endpoint yet — you need to add a minimal one scoped
  to P2P transfers only (not the full generic transaction history feature).
- No "Send Money" page on the frontend yet.

---

## 3. Backend

Namespace convention: `WalletSystem.*`. Follow the existing style in `AuthService.cs`
and `UsersController.cs` exactly (constructor injection, `ApiResponse<T>` returns via
the extension helpers, XML doc comments on public endpoints, `[ProducesResponseType]`
on every action).

### 3.1 DTOs — `backend/src/Wallet.Application/Transfers/Models/`

```csharp
// TransferRequest.cs
public class TransferRequest
{
    public string ReceiverAccountNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

// TransferResponse.cs
public class TransferResponse
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ReceiverAccountNo { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SenderBalanceAfter { get; set; }
    public string Status { get; set; } = string.Empty; // "SUCCESS"
    public DateTime CreatedAt { get; set; }
}

// TransferHistoryItem.cs
public class TransferHistoryItem
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "SENT" | "RECEIVED"
    public string CounterpartyAccountNo { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Also add (small, reusable — you'll want this for the balance card on the frontend):

```csharp
// backend/src/Wallet.Application/Wallets/Models/WalletBalanceResponse.cs
public class WalletBalanceResponse
{
    public Guid WalletId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty; // "BDT"
    public string CurrencySymbol { get; set; } = string.Empty; // "৳"
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty; // WalletStatus
}
```

### 3.2 Validation (FluentValidation, or manual — match whatever `RegisterRequest`
already uses)

- `ReceiverAccountNo`: required, must match the generated format (`^AC\d{8}$`).
- `Amount`: `> 0`. Since BDT has 2 decimal places (`CurrencySeed.Bdt.DecimalPlaces`),
  reject more than 2 decimal places.
- `Note`: optional, max length ~255.

### 3.3 `ITransferService` — `backend/src/Wallet.Application/Abstractions/ITransferService.cs`

```csharp
public interface ITransferService
{
    Task<ApiResponse<TransferResponse>> TransferAsync(Guid senderUserId, TransferRequest request, CancellationToken ct = default);
    Task<ApiResponse<List<TransferHistoryItem>>> GetHistoryAsync(Guid userId, CancellationToken ct = default);
}
```

Implement in `backend/src/Wallet.Infrastructure/Services/TransferService.cs`.

**`TransferAsync` logic (this is the part that matters most — read carefully):**

1. Look up the sender's BDT wallet: `Wallets.Include(w => w.User).FirstOrDefault(w => w.UserId == senderUserId && w.CurrencyId == CurrencySeed.BdtId)`.
   - It should always exist (created at registration) — if somehow missing, fail with
     `500` / a clear message rather than throwing unhandled.
2. Validate sender wallet `Status == ACTIVE` → else fail 409/422 `"Your wallet is frozen or closed."`
3. Look up the receiver `User` by `AccountNo == request.ReceiverAccountNo` → not found → fail `404` `"No account found with that account number."`
4. Reject `receiver.Id == senderUserId` → fail `400` `"You can't transfer money to yourself."`
5. Look up receiver's BDT wallet the same way. Validate it exists and is `ACTIVE`
   (it always will unless an admin froze it) → else fail `409` `"The receiver's wallet can't accept transfers right now."`
6. Validate `senderWallet.Balance >= request.Amount` → else fail `422`
   `"Insufficient balance."`
7. **Do everything below inside one `_dbContext.Database.BeginTransactionAsync(ct)`**,
   exactly like the pattern already used in `AuthService.RegisterAsync`:
   - **Lock both wallet rows** to prevent a lost update if the same wallet is hit by two
     transfers concurrently. EF Core has no built-in `SELECT ... FOR UPDATE`, so use raw
     SQL, e.g.:
     ```csharp
     var walletIdsInOrder = new[] { senderWallet.Id, receiverWallet.Id }.OrderBy(id => id).ToList();
     foreach (var id in walletIdsInOrder)
     {
         await _dbContext.Database.ExecuteSqlInterpolatedAsync(
             $"SELECT id FROM wallets WHERE id = {id} FOR UPDATE", ct);
     }
     ```
     **Always lock in a fixed order (sort by wallet id ascending)** — if sender and
     receiver locked in whichever order they happen to be passed in, two simultaneous
     transfers between the same pair of wallets in opposite directions can deadlock.
   - Re-read both wallets' current `Balance` after acquiring the lock (don't trust the
     values fetched in steps 1–6 — another transaction may have changed them) and
     re-check sufficient balance.
   - Generate a unique `Reference` (e.g. `"TXN" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()`).
   - Insert one `Transaction`: `UserId = senderUserId`, `CurrencyId = CurrencySeed.BdtId`,
     `Type = TransactionType.P2P_TRANSFER`, `Amount = request.Amount`,
     `Status = TransactionStatus.SUCCESS`, `Reference`, `Note = request.Note`.
   - Insert two `WalletLog` rows against that transaction:
     - sender: `Direction = DEBIT`, `Amount`, `BalanceBefore = senderWallet.Balance`,
       `BalanceAfter = senderWallet.Balance - Amount`.
     - receiver: `Direction = CREDIT`, `Amount`, `BalanceBefore = receiverWallet.Balance`,
       `BalanceAfter = receiverWallet.Balance + Amount`.
   - Update `senderWallet.Balance` and `receiverWallet.Balance` to those same
     `BalanceAfter` values (this is the invariant from `AGENTS.md` §5 — `Wallet.Balance`
     must always equal the latest applied `BalanceAfter`).
   - Insert one `P2PTransfer` row: `TransactionId`, `ReceiverWalletId = receiverWallet.Id`.
   - `SaveChangesAsync` + `CommitAsync`. On any exception, `RollbackAsync` and rethrow
     (same pattern as `AuthService.RegisterAsync`).
8. Return `ApiResponse<TransferResponse>` with the sender's new balance and receiver's
   name for the confirmation screen.

**Failure transactions:** for this simplified task you don't need a `PENDING`→`FAILED`
row for validation failures (there's no external call like the bank that can fail after
the fact) — a validation failure simply returns an error response and nothing is
written to the database. Only successful transfers get a `Transaction` row. (The
`PENDING`-first flow in `AGENTS.md` is for cash-in, where a real external bank call
happens in between — not needed here.)

**`GetHistoryAsync` logic:**

```csharp
var sent = await _dbContext.Transactions
    .Where(t => t.UserId == userId && t.Type == TransactionType.P2P_TRANSFER)
    .Include(t => t.P2PTransfer!).ThenInclude(p => p.ReceiverWallet).ThenInclude(w => w.User)
    .ToListAsync(ct);

var received = await _dbContext.Transactions
    .Where(t => t.Type == TransactionType.P2P_TRANSFER
        && t.P2PTransfer!.ReceiverWallet.UserId == userId
        && t.UserId != userId)
    .Include(t => t.User)
    .Include(t => t.P2PTransfer!)
    .ToListAsync(ct);
```

Map `sent` to `Direction = "SENT"` (counterparty = receiver), `received` to
`Direction = "RECEIVED"` (counterparty = `t.User`, the sender). Merge, sort by
`CreatedAt` descending. Pagination isn't required for this task — a flat list capped at
the most recent ~50 is fine.

### 3.4 `IWalletService` (minimal) — for the balance endpoint

```csharp
public interface IWalletService
{
    Task<ApiResponse<WalletBalanceResponse>> GetMyBdtWalletAsync(Guid userId, CancellationToken ct = default);
}
```

Just look up the user's BDT wallet + its `Currency` and map to `WalletBalanceResponse`.
Don't build the full multi-wallet listing endpoint from `AGENTS.md` Task 2 — that's a
separate, bigger task (admin-assigned wallets, multiple currencies). This is
intentionally the smallest possible slice: "give me my one wallet's balance."

### 3.5 Controllers

`backend/src/Wallet.Api/Controllers/WalletsController.cs`:
```csharp
[ApiController]
[Authorize]
[Route("wallets")]
public class WalletsController : ControllerBase
{
    // GET /api/v1/wallets/me -> ApiResponse<WalletBalanceResponse>
}
```

`backend/src/Wallet.Api/Controllers/TransfersController.cs`:
```csharp
[ApiController]
[Authorize]
[Route("transfers")]
public class TransfersController : ControllerBase
{
    // POST /api/v1/transfers        body: TransferRequest -> ApiResponse<TransferResponse>
    // GET  /api/v1/transfers        -> ApiResponse<List<TransferHistoryItem>>
}
```

Both use `User.GetUserId()` exactly like `UsersController.GetCurrentUser` does — don't
introduce a different way of reading the current user.

**Optional but recommended (nice UX, small effort):** a lookup endpoint so the frontend
can show "Sending to: Jane Doe" before the user confirms, instead of finding out the
receiver's identity only after submitting:

```
GET /api/v1/users/lookup?accountNo=AC00000002
-> ApiResponse<{ accountNo: string; name: string }>
-> 404 if no user has that account number
```

Add this as `Lookup` on `UsersController`. Don't expose email or anything else about
the receiver — only name + account number, since this is reachable by any
authenticated user typing an arbitrary account number.

### 3.6 Register services in DI

In `backend/src/Wallet.Infrastructure/DependencyInjection.cs`, add:
```csharp
services.AddScoped<ITransferService, TransferService>();
services.AddScoped<IWalletService, WalletService>();
```

### 3.7 Error cases to cover (write integration tests for each)

| Scenario | Expected result |
|---|---|
| Valid transfer, sufficient balance | `201`, sender balance decreases, receiver balance increases, one `Transaction` + two `WalletLog` rows + one `P2PTransfer` row created |
| Receiver account number doesn't exist | `404`, no rows written |
| Receiver account number = sender's own | `400`, no rows written |
| Amount ≤ 0 | `400` (model validation) |
| Amount > sender balance | `422`, no rows written |
| Sender wallet `FROZEN`/`CLOSED` | `409`, no rows written |
| Receiver wallet `FROZEN`/`CLOSED` | `409`, no rows written |
| No auth token | `401` |
| Two concurrent transfers draining the same wallet below zero | second one must fail cleanly, balance never goes negative (this is the one to actually test with `Task.WhenAll`, per `AGENTS.md` §5) |

---

## 4. Frontend

Follow the exact conventions already used in `src/components/auth/signin-form.tsx` and
`src/hooks/use-auth.ts`: React Hook Form + Zod (`@hookform/resolvers/zod`) for the
form, TanStack Query (`useMutation`/`useQuery`) for API calls, the shared `api` axios
instance, and `getApiErrorMessage()` for surfacing backend errors.

### 4.1 Types — `src/types/transfer.ts`

```ts
export interface WalletBalance {
  walletId: string
  currencyCode: string
  currencySymbol: string
  balance: number
  status: "ACTIVE" | "FROZEN" | "CLOSED"
}

export interface TransferPayload {
  receiverAccountNo: string
  amount: number
  note?: string
}

export interface TransferResult {
  transactionId: string
  reference: string
  receiverAccountNo: string
  receiverName: string
  amount: number
  senderBalanceAfter: number
  status: string
  createdAt: string
}

export interface TransferHistoryItem {
  transactionId: string
  reference: string
  direction: "SENT" | "RECEIVED"
  counterpartyAccountNo: string
  counterpartyName: string
  amount: number
  status: string
  note: string | null
  createdAt: string
}

export interface AccountLookupResult {
  accountNo: string
  name: string
}
```

### 4.2 API functions — `src/lib/api/wallets.ts` and `src/lib/api/transfers.ts`

```ts
// src/lib/api/wallets.ts
import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type { WalletBalance } from "@/types/transfer"

export async function getMyWallet() {
  const { data } = await api.get<ApiResponse<WalletBalance>>("/wallets/me")
  return data
}
```

```ts
// src/lib/api/transfers.ts
import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type {
  AccountLookupResult,
  TransferHistoryItem,
  TransferPayload,
  TransferResult,
} from "@/types/transfer"

export async function sendTransfer(payload: TransferPayload) {
  const { data } = await api.post<ApiResponse<TransferResult>>("/transfers", payload)
  return data
}

export async function getTransferHistory() {
  const { data } = await api.get<ApiResponse<TransferHistoryItem[]>>("/transfers")
  return data
}

export async function lookupAccount(accountNo: string) {
  const { data } = await api.get<ApiResponse<AccountLookupResult>>(
    "/users/lookup",
    { params: { accountNo } }
  )
  return data
}
```

### 4.3 Hooks — `src/hooks/use-transfers.ts`

```ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { getMyWallet } from "@/lib/api/wallets"
import { getTransferHistory, lookupAccount, sendTransfer } from "@/lib/api/transfers"

export function useMyWallet() {
  return useQuery({ queryKey: ["wallet", "me"], queryFn: getMyWallet })
}

export function useTransferHistory() {
  return useQuery({ queryKey: ["transfers", "history"], queryFn: getTransferHistory })
}

export function useAccountLookup(accountNo: string) {
  return useQuery({
    queryKey: ["users", "lookup", accountNo],
    queryFn: () => lookupAccount(accountNo),
    enabled: accountNo.length === 10, // "AC" + 8 digits
    retry: false,
  })
}

export function useSendTransfer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: sendTransfer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["wallet", "me"] })
      queryClient.invalidateQueries({ queryKey: ["transfers", "history"] })
    },
  })
}
```

Adjust `enabled`/lookup UX as needed — the point is: don't call `lookupAccount` on
every keystroke, only once the field looks like a complete account number, and don't
fail the whole form if lookup 404s (just don't show a confirmed name yet).

### 4.4 Page — `src/app/(protected)/transfers/page.tsx`

Build one page with:
1. A balance card at the top (reuse the `Card`/`CardHeader`/`CardTitle` pattern from
   `dashboard/page.tsx`) showing `useMyWallet()` data — "Available balance: ৳ 1,250.00".
2. A form (same `Field`/`FieldLabel`/`FieldError`/`Controller` pattern as
   `signin-form.tsx`):
   - `receiverAccountNo` input — as the user types, show the looked-up receiver name
     underneath once `useAccountLookup` resolves ("Sending to: Jane Doe") so they can
     catch typos before submitting.
   - `amount` input (number, 2 decimals).
   - `note` input (optional).
   - Submit button, disabled while `useSendTransfer().isPending`.
   - On success: show a confirmation (reference number, new balance) — a simple
     inline success panel is fine, no need for a toast library since one isn't wired up
     yet.
   - On error: reuse `getApiErrorMessage(mutation.error)` exactly like `signin-form.tsx`
     does.
3. Below the form, a simple table/list rendering `useTransferHistory()` — direction
   (badge: "Sent" red / "Received" green), counterparty name + account no, amount,
   date, status.

Zod schema:
```ts
const transferFormSchema = z.object({
  receiverAccountNo: z
    .string()
    .regex(/^AC\d{8}$/, "Enter a valid account number (e.g. AC00000001)."),
  amount: z.coerce.number().positive("Amount must be greater than 0."),
  note: z.string().max(255).optional(),
})
```

### 4.5 Navigation

Add to `NAV_ITEMS` in `src/components/nav-main.tsx`:
```ts
{ title: "Send Money", url: "/transfers", icon: SendIcon }, // from lucide-react
```

### 4.6 Manual test checklist before opening a PR

- [ ] Register two users (User A, User B) via the sign-up form.
- [ ] Log in as A, go to `/transfers`, confirm balance shows `৳ 0.00`.
- [ ] Since A's balance is 0, sending money should fail with a clear "insufficient
      balance" message in the UI (this exercises the error path end-to-end — there's
      no cash-in yet to fund the wallet, so this is the only way to test failure until
      that task lands).
- [ ] Enter B's account number, confirm the looked-up name shows correctly.
- [ ] Enter your own account number, confirm you get a friendly "can't send to
      yourself" message, not a raw 400.
- [ ] Enter a non-existent account number, confirm "no account found" message.
- [ ] Confirm the history table renders (empty state is fine at this point).

---

## 5. Out of scope for this task (don't build these here)

- Currency selection / multi-wallet selection (depends on admin-managed wallets, not
  built yet).
- Cash-in (needed to actually fund a wallet for full happy-path testing — separate
  task, own dev).
- Admin views of all transfers.
- The generic `IWalletLedger` abstraction and `FailureCodes` shared constants from
  `AGENTS.md` §4/Task 0 — reuse this task's `TransferService` logic when that lands
  later, don't block on it now.
- Pagination/filtering on transfer history.

---

## 6. Definition of Done

- [ ] `dotnet build` succeeds, zero warnings.
- [ ] Integration tests covering every row in the §3.7 table.
- [ ] Manual test checklist in §4.6 passes locally against the real API (not mocked).
- [ ] No direct writes to `Wallet.Balance` outside `TransferService` (matches the
      `AGENTS.md` invariant — balance is only ever moved by log-writing code).
- [ ] PR description includes example `curl`/Swagger request + response for
      `POST /api/v1/transfers`.
