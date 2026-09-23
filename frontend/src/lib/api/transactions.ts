import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type { TransactionSummary } from "@/types/transaction"

export async function getTransactions() {
  const { data } = await api.get<ApiResponse<TransactionSummary[]>>(
    "/transactions"
  )
  return data
}
