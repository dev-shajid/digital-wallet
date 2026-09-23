"use client"

import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import {
  ArrowLeftRightIcon,
  LoaderCircleIcon,
  PlusIcon,
  ReceiptIcon,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { useExpenseCategories } from "@/hooks/use-expense-categories"
import { useCreateExpense } from "@/hooks/use-expenses"
import { useTransactions } from "@/hooks/use-transactions"
import { useWallets } from "@/hooks/use-wallets"
import { getApiErrorMessage } from "@/lib/api/error"
import type { TransactionType } from "@/types/transaction"

const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  EXPENSE: "Expense",
  CASH_IN: "Cash in",
  CASH_OUT: "Cash out",
  P2P_TRANSFER: "Transfer",
}

function today() {
  return new Date().toISOString().slice(0, 10)
}

const expenseFormSchema = z.object({
  currencyId: z.string().min(1, "Select a wallet."),
  categoryId: z.string().min(1, "Select a category."),
  amount: z.coerce
    .number()
    .min(0.01, "Amount must be greater than zero.")
    .max(99999999999999.9999, "Amount is too large."),
  expenseDate: z
    .string()
    .min(1, "Pick a date.")
    .refine((value) => value <= today(), "Expense date can't be in the future."),
  note: z.string().max(255, "Note cannot exceed 255 characters.").optional(),
})

type ExpenseFormValues = z.infer<typeof expenseFormSchema>

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
  const walletsQuery = useWallets()
  const categoriesQuery = useExpenseCategories()
  const transactionsQuery = useTransactions()
  const createExpenseMutation = useCreateExpense()

  const [isExpenseFormOpen, setIsExpenseFormOpen] = useState(false)
  const [isTransferOpen, setIsTransferOpen] = useState(false)

  const form = useForm<ExpenseFormValues>({
    resolver: zodResolver(expenseFormSchema),
    defaultValues: {
      currencyId: "",
      categoryId: "",
      amount: 0,
      expenseDate: today(),
      note: "",
    },
  })

  const wallets = (walletsQuery.data?.data ?? []).filter(
    (wallet) => wallet.status === "ACTIVE"
  )
  const categories = (categoriesQuery.data?.data ?? []).filter(
    (category) => category.status === "ACTIVE"
  )
  const transactions = transactionsQuery.data?.data ?? []

  function defaultFormValues() {
    return {
      currencyId: wallets[0]?.currencyId ?? "",
      categoryId: "",
      amount: 0,
      expenseDate: today(),
      note: "",
    }
  }

  function openExpenseForm() {
    form.reset(defaultFormValues())
    createExpenseMutation.reset()
    setIsExpenseFormOpen(true)
  }

  function closeExpenseForm(open: boolean) {
    setIsExpenseFormOpen(open)

    if (!open) {
      form.reset(defaultFormValues())
      createExpenseMutation.reset()
    }
  }

  function onSubmit(values: ExpenseFormValues) {
    createExpenseMutation.mutate(
      {
        currencyId: values.currencyId,
        categoryId: values.categoryId,
        amount: values.amount,
        expenseDate: values.expenseDate,
        note: values.note || undefined,
      },
      {
        onSuccess: (response) => {
          if (response.success) {
            setIsExpenseFormOpen(false)
          }
        },
      }
    )
  }

  return (
    <>
      <div className="flex flex-col gap-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold">Transactions</h1>
            <p className="text-muted-foreground">
              Everything you&apos;ve spent or moved between wallets.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Button type="button" variant="outline" onClick={() => setIsTransferOpen(true)}>
              <ArrowLeftRightIcon />
              Transfer money
            </Button>
            <Button
              type="button"
              onClick={openExpenseForm}
              disabled={
                categoriesQuery.isLoading ||
                walletsQuery.isLoading ||
                categories.length === 0 ||
                wallets.length === 0
              }
            >
              <PlusIcon />
              Add expense
            </Button>
          </div>
        </div>

        {categoriesQuery.data && categories.length === 0 && (
          <div className="rounded-md border border-input bg-muted/50 px-4 py-3 text-sm text-muted-foreground">
            There are no active expense categories yet. Ask an admin to add one
            before you can record an expense.
          </div>
        )}

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

      <Sheet open={isExpenseFormOpen} onOpenChange={closeExpenseForm}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>Add expense</SheetTitle>
            <SheetDescription>
              Pick a category and amount to debit from your wallet.
            </SheetDescription>
          </SheetHeader>

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-1 flex-col"
            noValidate
          >
            <div className="flex-1 px-4">
              <FieldGroup>
                {createExpenseMutation.isError && (
                  <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    {getApiErrorMessage(createExpenseMutation.error)}
                  </div>
                )}

                <Controller
                  name="currencyId"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Wallet</FieldLabel>
                      <Select
                        value={field.value}
                        onValueChange={field.onChange}
                        disabled={createExpenseMutation.isPending || wallets.length <= 1}
                      >
                        <SelectTrigger
                          id={field.name}
                          aria-invalid={fieldState.invalid}
                        >
                          <SelectValue placeholder="Select a wallet" />
                        </SelectTrigger>
                        <SelectContent>
                          {wallets.map((wallet) => (
                            <SelectItem
                              key={wallet.currencyId}
                              value={wallet.currencyId}
                            >
                              {wallet.currencyCode} · {wallet.currencySymbol}
                              {wallet.balance.toFixed(2)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {wallets.length <= 1 && (
                        <FieldDescription>
                          You only have one wallet, so this is fixed.
                        </FieldDescription>
                      )}
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="categoryId"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Category</FieldLabel>
                      <Select
                        value={field.value}
                        onValueChange={field.onChange}
                        disabled={createExpenseMutation.isPending}
                      >
                        <SelectTrigger
                          id={field.name}
                          aria-invalid={fieldState.invalid}
                        >
                          <SelectValue placeholder="Select a category" />
                        </SelectTrigger>
                        <SelectContent>
                          {categories.map((category) => (
                            <SelectItem key={category.id} value={category.id}>
                              {category.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="amount"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Amount</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        type="number"
                        min="0.01"
                        step="0.01"
                        placeholder="0.00"
                        aria-invalid={fieldState.invalid}
                        disabled={createExpenseMutation.isPending}
                        onChange={(event) => field.onChange(event.target.value)}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="expenseDate"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Date</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        type="date"
                        max={today()}
                        aria-invalid={fieldState.invalid}
                        disabled={createExpenseMutation.isPending}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="note"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>
                        Note (optional)
                      </FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        placeholder="e.g. Lunch with team"
                        aria-invalid={fieldState.invalid}
                        disabled={createExpenseMutation.isPending}
                      />
                      <FieldDescription>
                        A short description to help you remember this expense.
                      </FieldDescription>
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />
              </FieldGroup>
            </div>

            <SheetFooter>
              <Button type="submit" disabled={createExpenseMutation.isPending}>
                {createExpenseMutation.isPending && (
                  <LoaderCircleIcon className="animate-spin" />
                )}
                {createExpenseMutation.isPending ? "Saving..." : "Add expense"}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>

      {/* Placeholder only - P2P transfers aren't implemented yet. This just
          reserves the entry point in the UI so it doesn't need to be added
          again once that feature is built. */}
      <Sheet open={isTransferOpen} onOpenChange={setIsTransferOpen}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>Transfer money</SheetTitle>
            <SheetDescription>
              Send money to another user&apos;s wallet.
            </SheetDescription>
          </SheetHeader>

          <div className="flex flex-1 flex-col items-center justify-center gap-3 px-4 text-center">
            <ArrowLeftRightIcon className="size-10 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Wallet-to-wallet transfers are coming soon.
            </p>
          </div>
        </SheetContent>
      </Sheet>
    </>
  )
}
