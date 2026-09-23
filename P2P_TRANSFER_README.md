# P2P Money Transfer — Sequential Task List

Here is the exact, ordered task list (1 to 19) to execute from start to finish:

---

## 📋 Sequential Tasks

1. **[x] Create Transfer Request DTO**  
   Create `backend/src/Wallet.Application/Transfers/Models/TransferRequest.cs` with validation (`ReceiverAccountNo` regex `^AC\d{8}$`, `Amount > 0` with max 2 decimals, optional `Note` max 255 chars).

2. **[x] Create Transfer Response DTO**  
   Create `backend/src/Wallet.Application/Transfers/Models/TransferResponse.cs` with transaction receipt fields (`TransactionId`, `Reference`, `ReceiverAccountNo`, `ReceiverName`, `Amount`, `SenderBalanceAfter`, `Status`, `CreatedAt`).

3. **[x] Create Transfer History Item DTO**  
   Create `backend/src/Wallet.Application/Transfers/Models/TransferHistoryItem.cs` (`TransactionId`, `Reference`, `Direction` ["SENT"/"RECEIVED"], `CounterpartyAccountNo`, `CounterpartyName`, `Amount`, `Status`, `Note`, `CreatedAt`).

4. **[x] Create Wallet Balance Response DTO**  
   Create `backend/src/Wallet.Application/Wallets/Models/WalletBalanceResponse.cs` (`WalletId`, `CurrencyCode`, `CurrencySymbol`, `Balance`, `Status`).

5. **[x] Create Transfer Service Interface**  
   Create `backend/src/Wallet.Application/Abstractions/ITransferService.cs` declaring `TransferAsync` and `GetHistoryAsync`.

6. **[x] Create Wallet Service Interface**  
   Create `backend/src/Wallet.Application/Abstractions/IWalletService.cs` declaring `GetMyBdtWalletAsync`.

7. **[x] Implement Wallet Service**  
   Create `backend/src/Wallet.Infrastructure/Services/WalletService.cs` implementing `IWalletService` to return the current user's default BDT wallet and balance.

8. **[x] Implement Transfer Service**  
   Create `backend/src/Wallet.Infrastructure/Services/TransferService.cs` implementing `ITransferService`:
   - Validate sender & receiver wallets exist and are `ACTIVE`.
   - Prevent self-transfers (`receiver.Id != senderUserId`).
   - Run inside `BeginTransactionAsync`.
   - Execute row-level locks (`SELECT id FROM wallets WHERE id = ... FOR UPDATE`) ordered by `Wallet.Id` ASC to prevent deadlocks.
   - Re-check sender balance after lock acquisition.
   - Insert `Transaction` record (`Type: P2P_TRANSFER`, `Status: SUCCESS`).
   - Insert 2 `WalletLog` rows (`DEBIT` on sender, `CREDIT` on receiver) with `BalanceBefore` and `BalanceAfter`.
   - Update `Wallet.Balance` on both wallets.
   - Insert `P2PTransfer` record linking `ReceiverWalletId`.
   - Commit transaction atomically.
   - Implement `GetHistoryAsync` (queries sent and received P2P transfers, sorts descending by `CreatedAt`, limits to 50).

9. **[x] Register Services in Dependency Injection**  
   Register `ITransferService` and `IWalletService` in `backend/src/Wallet.Infrastructure/DependencyInjection.cs`.

10. **[x] Create Wallets Controller**  
    Create `backend/src/Wallet.Api/Controllers/WalletsController.cs` with `GET /wallets/me` returning `ApiResponse<WalletBalanceResponse>`.

11. **[x] Create Transfers Controller**  
    Create `backend/src/Wallet.Api/Controllers/TransfersController.cs`:
    - `POST /transfers` accepting `TransferRequest` and returning `ApiResponse<TransferResponse>`.
    - `GET /transfers` returning `ApiResponse<List<TransferHistoryItem>>`.

12. **[x] Add Account Lookup Endpoint**  
    Add `GET /users/lookup?accountNo={accountNo}` to `backend/src/Wallet.Api/Controllers/UsersController.cs` returning `{ accountNo, name }` for live receiver verification.

13. **[x] Create Frontend TypeScript Types**  
    Create `frontend/src/types/transfer.ts` defining `WalletBalance`, `TransferPayload`, `TransferResult`, `TransferHistoryItem`, and `AccountLookupResult`.

14. **[x] Create Frontend API Client Functions**  
    Create `frontend/src/lib/api/wallets.ts` (`getMyWallet`) and `frontend/src/lib/api/transfers.ts` (`sendTransfer`, `getTransferHistory`, `lookupAccount`).

15. **[x] Create Frontend TanStack Query Hooks**  
    Create `frontend/src/hooks/use-transfers.ts` providing `useMyWallet`, `useTransferHistory`, `useAccountLookup`, and `useSendTransfer` (with query cache invalidation on success).

16. **[x] Build Send Money Page**  
    Create `frontend/src/app/(protected)/transfers/page.tsx`:
    - Available balance card at the top.
    - Transfer form with Zod validation.
    - Live recipient name lookup feedback.
    - Inline confirmation receipt & error alerts.
    - Recent transfer history table/list with status and direction badges.

17. **[x] Update Navigation Sidebar**  
    Add "Send Money" (`/transfers`) with an icon to `frontend/src/components/nav-main.tsx`.

18. **[x] Write Backend Integration & Concurrency Tests**  
    Add integration test suite in `backend/tests/Wallet.Api.IntegrationTests/Transfers/` covering valid transfers, non-existent receivers, self-transfers, insufficient balance, frozen wallets, unauthenticated requests, and race-condition safety with `Task.WhenAll`.

19. **[x] Run End-to-End Verification & Code Review**  
    Full codebase verified and reviewed across all layers without errors.
