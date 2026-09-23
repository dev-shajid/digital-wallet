"use client"

import { ReceiptIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { useTransactions } from "@/hooks/use-transactions"
import { getApiErrorMessage } from "@/lib/api/error"
import type { TransactionType } from "@/types/transaction"

const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  EXPENSE: "Expense",
  CASH_IN: "Cash in",
  CASH_OUT: "Cash out",
  P2P_TRANSFER: "Transfer",
}

function formatAmount(amount: number) {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(amount)
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  })
}

export default function TransactionsPage() {
  const transactionsQuery = useTransactions()
  const transactions = transactionsQuery.data?.data ?? []

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Transactions</h1>
        <p className="text-muted-foreground">
          Everything you&apos;ve spent or moved between wallets.
        </p>
      </div>

      {transactionsQuery.isLoading ? (
        <div className="flex flex-col gap-3">
          {[1, 2, 3].map((item) => (
            <div key={item} className="h-16 animate-pulse rounded-xl bg-muted" />
          ))}
        </div>
      ) : transactionsQuery.isError ? (
        <div className="flex flex-col gap-4">
          <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {getApiErrorMessage(transactionsQuery.error)}
          </div>
          <Button
            type="button"
            variant="outline"
            className="self-start"
            onClick={() => transactionsQuery.refetch()}
          >
            Try again
          </Button>
        </div>
      ) : transactions.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
            <ReceiptIcon className="size-10 text-muted-foreground" />
            <div>
              <h2 className="font-medium">No transactions yet</h2>
              <p className="text-sm text-muted-foreground">
                Expenses and transfers will show up here.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="divide-y p-0">
            {transactions.map((transaction) => {
              const isCredit = transaction.direction === "CREDIT"
              const title =
                transaction.categoryName ??
                TRANSACTION_TYPE_LABELS[transaction.type]

              return (
                <div
                  key={transaction.transactionId}
                  className="flex items-center justify-between gap-3 px-4 py-2 text-sm"
                >
                  <div className="flex min-w-0 items-center gap-1.5">
                    <span className="shrink-0 rounded-full bg-muted px-1.5 py-px text-[10px] font-medium text-muted-foreground">
                      {TRANSACTION_TYPE_LABELS[transaction.type]}
                    </span>
                    <span className="truncate font-medium">{title}</span>
                    <span className="truncate text-muted-foreground">
                      · {formatDate(transaction.createdAt)}
                      {transaction.note ? ` · ${transaction.note}` : ""}
                    </span>
                  </div>

                  <span
                    className={
                      "shrink-0 font-semibold " +
                      (isCredit ? "text-green-600 dark:text-green-500" : "text-destructive")
                    }
                  >
                    {isCredit ? "+" : "-"}
                    {formatAmount(transaction.amount)} {transaction.currencyCode}
                  </span>
                </div>
              )
            })}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
