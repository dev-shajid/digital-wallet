export type TransactionType = "CASH_IN" | "CASH_OUT" | "P2P_TRANSFER" | "EXPENSE"
export type TransactionDirection = "DEBIT" | "CREDIT"

// One row in the unified history from GET /transactions - covers every
// transaction type. Fields that only apply to one type (categoryName, so
// far only set for EXPENSE) are null for every other type.
export interface TransactionSummary {
  transactionId: string
  reference: string
  type: TransactionType
  status: string
  direction: TransactionDirection
  amount: number
  currencyCode: string
  note: string | null
  categoryName: string | null
  createdAt: string
}
