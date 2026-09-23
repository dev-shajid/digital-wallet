import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type {
  CashInPayload,
  CashInResponse,
  Wallet,
} from "@/types/wallet"

export async function getWallets() {
  const { data } = await api.get<ApiResponse<Wallet[]>>("/wallets")
  return data
}

export async function cashInWallet(
  walletId: string,
  payload: CashInPayload
) {
  const { data } = await api.post<ApiResponse<CashInResponse>>(
    `/wallets/${walletId}/cash-in`,
    payload
  )

  return data
}