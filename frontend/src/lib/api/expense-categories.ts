import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type {
  CreateExpenseCategoryPayload,
  ExpenseCategory,
  UpdateExpenseCategoryPayload,
} from "@/types/expense"

// Role-aware on the backend: a regular user only gets ACTIVE categories back,
// an admin gets every status.
export async function getExpenseCategories() {
  const { data } = await api.get<ApiResponse<ExpenseCategory[]>>(
    "/expense-categories"
  )
  return data
}

export async function createExpenseCategory(
  payload: CreateExpenseCategoryPayload
) {
  const { data } = await api.post<ApiResponse<ExpenseCategory>>(
    "/expense-categories",
    payload
  )
  return data
}

export async function updateExpenseCategory(
  id: string,
  payload: UpdateExpenseCategoryPayload
) {
  const { data } = await api.put<ApiResponse<ExpenseCategory>>(
    `/expense-categories/${id}`,
    payload
  )
  return data
}

export async function deleteExpenseCategory(id: string) {
  const { data } = await api.delete<ApiResponse<{ id: string; name: string }>>(
    `/expense-categories/${id}`
  )
  return data
}
