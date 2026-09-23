export type WalletStatus = "ACTIVE" | "FROZEN" | "CLOSED"

export interface Wallet {
  id: string
  currencyCode: string
  currencyName: string
  currencySymbol: string
  balance: number
  status: WalletStatus
  createdAt: string
  updatedAt: string
}

export interface CashInPayload {
  amount: number
}

export interface CashInResponse {
  transactionId: string
  transactionReference: string
  walletId: string
  amount: number
  currencyCode: string
  balanceBefore: number
  balanceAfter: number
  status: string
  createdAt: string
}