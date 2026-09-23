import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type { CreateExpensePayload, Expense } from "@/types/expense"

export async function getExpenses() {
  const { data } = await api.get<ApiResponse<Expense[]>>("/expenses")
  return data
}

export async function createExpense(payload: CreateExpensePayload) {
  const { data } = await api.post<ApiResponse<Expense>>("/expenses", payload)
  return data
}
