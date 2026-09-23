export interface WalletBalance {
  walletId: string
  currencyCode: string
  currencySymbol: string
  balance: number
  status: "ACTIVE" | "FROZEN" | "CLOSED"
}

export interface TransferPayload {
  receiverAccountNo: string
  amount: number
  note?: string
}

export interface TransferResult {
  transactionId: string
  reference: string
  receiverAccountNo: string
  receiverName: string
  amount: number
  senderBalanceAfter: number
  status: string
  createdAt: string
}

export interface TransferHistoryItem {
  transactionId: string
  reference: string
  direction: "SENT" | "RECEIVED"
  counterpartyAccountNo: string
  counterpartyName: string
  amount: number
  status: string
  note: string | null
  createdAt: string
}

export interface AccountLookupResult {
  accountNo: string
  name: string
}
