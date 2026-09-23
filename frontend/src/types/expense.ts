export type ExpenseCategoryStatus = "ACTIVE" | "INACTIVE"

export interface ExpenseCategory {
  id: string
  name: string
  description: string
  status: ExpenseCategoryStatus
  createdAt: string
  updatedAt: string
}

// Admin only - POST /expense-categories
export interface CreateExpenseCategoryPayload {
  name: string
  description?: string
}

// Admin only - PUT /expense-categories/{id}. Every field is optional on the wire,
// but the backend rejects a body with all of them omitted.
export interface UpdateExpenseCategoryPayload {
  name?: string
  description?: string
  status?: ExpenseCategoryStatus
}

export interface Expense {
  transactionId: string
  reference: string
  categoryName: string
  amount: number
  note: string | null
  expenseDate: string
  walletBalanceAfter: number
  createdAt: string
}

export interface CreateExpensePayload {
  categoryId: string
  amount: number
  note?: string
  expenseDate?: string
}
