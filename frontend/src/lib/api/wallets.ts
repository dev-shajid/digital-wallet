import { api } from "@/lib/axios"
import type { ApiResponse } from "@/types/auth"
import type { WalletBalance } from "@/types/transfer"

export async function getMyWallet() {
  const { data } = await api.get<ApiResponse<WalletBalance>>("/wallets/me")
  return data
}
