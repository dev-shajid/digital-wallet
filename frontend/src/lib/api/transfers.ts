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
