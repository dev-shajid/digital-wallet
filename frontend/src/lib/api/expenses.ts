import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type { CreateExpensePayload, Expense } from "@/types/expense"

// Listing is done through GET /transactions now (a unified history across
// every transaction type) - this only creates.
export async function createExpense(payload: CreateExpensePayload) {
  const { data } = await api.post<ApiResponse<Expense>>("/expenses", payload)
  return data
}
