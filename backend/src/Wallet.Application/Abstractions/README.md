# Application layer

This layer will hold **use cases** and **interfaces** in later phases - things like
`IWalletLedger` (the only component allowed to write balances/wallet_logs) and
`IBankClient` (the adapter that calls the Mock Bank Service).

Phase 1 is folder structure + database schema only, so this layer is intentionally
empty right now. It references `Wallet.Domain` so those future interfaces can use
domain entities (e.g. `Task<Wallet> DebitAsync(...)`).
