"use client"

import * as React from "react"
import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import {
  AlertCircle,
  ArrowDownLeft,
  ArrowUpRight,
  CheckCircle2,
  Clock,
  Loader2,
  Send,
  UserCheck,
  Wallet,
} from "lucide-react"
import { Controller, useForm } from "react-hook-form"
import * as z from "zod"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  useAccountLookup,
  useMyWallet,
  useSendTransfer,
  useTransferHistory,
} from "@/hooks/use-transfers"
import { getApiErrorMessage } from "@/lib/api/error"
import type { ApiResponse } from "@/types/auth"
import type { TransferHistoryItem, TransferResult } from "@/types/transfer"

const transferFormSchema = z.object({
  receiverAccountNo: z
    .string()
    .trim()
    .regex(/^AC\d{8}$/i, "Enter a valid account number (e.g. AC00000001)."),
  amount: z.coerce.number().positive("Amount must be greater than 0."),
  note: z.string().max(255, "Note cannot exceed 255 characters.").optional(),
})

type TransferFormValues = z.infer<typeof transferFormSchema>

export default function TransfersPage() {
  const [lastReceipt, setLastReceipt] = useState<TransferResult | null>(null)

  const { data: wallet, isLoading: isWalletLoading } = useMyWallet()
  const { data: history, isLoading: isHistoryLoading } = useTransferHistory()
  const sendMutation = useSendTransfer()

  const form = useForm<TransferFormValues>({
    resolver: zodResolver(transferFormSchema),
    defaultValues: {
      receiverAccountNo: "",
      amount: undefined,
      note: "",
    },
  })

  const watchedAccountNo = form.watch("receiverAccountNo") || ""
  const { data: lookedUpUser, isFetching: isLookingUp } = useAccountLookup(watchedAccountNo)

  function onSubmit(values: TransferFormValues) {
    setLastReceipt(null)
    sendMutation.mutate(
      {
        receiverAccountNo: values.receiverAccountNo.trim().toUpperCase(),
        amount: values.amount,
        note: values.note?.trim() || undefined,
      },
      {
        onSuccess: (res: ApiResponse<TransferResult>) => {
          if (res?.data) {
            setLastReceipt(res.data)
            form.reset({
              receiverAccountNo: "",
              amount: undefined,
              note: "",
            })
          }
        },
      }
    )
  }

  return (
    <div className="flex flex-col gap-8 pb-12">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Send Money (P2P Transfer)</h1>
        <p className="text-muted-foreground text-sm">
          Instantly transfer BDT funds to any registered account number.
        </p>
      </div>

      {/* Top Section: Balance Card & Send Form */}
      <div className="grid gap-6 md:grid-cols-3">
        {/* Available Balance Card */}
        <Card className="md:col-span-1 shadow-sm border">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Available BDT Balance
            </CardTitle>
            <Wallet className="h-5 w-5 text-muted-foreground" />
          </CardHeader>
          <CardContent className="pt-2">
            {isWalletLoading ? (
              <div className="flex items-center gap-2 py-3 text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                <span className="text-sm">Loading balance...</span>
              </div>
            ) : (
              <div className="space-y-2">
                <div className="text-3xl font-bold tracking-tight">
                  {wallet?.currencySymbol || "৳"}{" "}
                  {wallet?.balance !== undefined
                    ? Number(wallet.balance).toLocaleString("en-US", {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })
                    : "0.00"}
                </div>
                <div className="flex items-center gap-2">
                  <span className="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-950 dark:text-emerald-400">
                    {wallet?.status || "ACTIVE"}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    {wallet?.currencyCode || "BDT"} Wallet
                  </span>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Transfer Form Card */}
        <Card className="md:col-span-2 shadow-sm border">
          <CardHeader>
            <CardTitle className="text-lg">Transfer Funds</CardTitle>
            <CardDescription>
              Enter the recipient&apos;s 10-digit account number and transfer amount.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4" noValidate>
              {/* API Error Message */}
              {sendMutation.isError && (
                <div className="flex items-start gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
                  <AlertCircle className="h-5 w-5 shrink-0 mt-0.5" />
                  <div>{getApiErrorMessage(sendMutation.error)}</div>
                </div>
              )}

              {/* Success Receipt Banner */}
              {lastReceipt && (
                <div className="rounded-lg border border-emerald-500/30 bg-emerald-50/50 dark:bg-emerald-950/40 p-4 text-sm text-emerald-800 dark:text-emerald-200">
                  <div className="flex items-center gap-2 font-medium">
                    <CheckCircle2 className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                    <span>Transfer Successful!</span>
                  </div>
                  <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-xs sm:grid-cols-4">
                    <div>
                      <span className="text-muted-foreground">Receipt Ref:</span>
                      <p className="font-mono font-medium">{lastReceipt.reference}</p>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Sent To:</span>
                      <p className="font-medium">{lastReceipt.receiverName}</p>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Amount:</span>
                      <p className="font-medium">৳ {lastReceipt.amount.toFixed(2)}</p>
                    </div>
                    <div>
                      <span className="text-muted-foreground">New Balance:</span>
                      <p className="font-medium">৳ {lastReceipt.senderBalanceAfter.toFixed(2)}</p>
                    </div>
                  </div>
                </div>
              )}

              <FieldGroup className="space-y-4">
                {/* Receiver Account Number */}
                <Controller
                  name="receiverAccountNo"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Recipient Account Number</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        placeholder="e.g. AC00000002"
                        className="font-mono uppercase"
                        aria-invalid={fieldState.invalid}
                      />
                      {/* Live Recipient Lookup Indicator */}
                      {isLookingUp && (
                        <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                          <Loader2 className="h-3 w-3 animate-spin" />
                          <span>Looking up account name...</span>
                        </div>
                      )}
                      {lookedUpUser && !isLookingUp && (
                        <div className="flex items-center gap-1.5 text-xs text-emerald-600 dark:text-emerald-400 font-medium mt-1">
                          <UserCheck className="h-3.5 w-3.5" />
                          <span>Recipient: {lookedUpUser.name}</span>
                        </div>
                      )}
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                {/* Amount */}
                <Controller
                  name="amount"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Amount (BDT)</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        type="number"
                        step="0.01"
                        min="0.01"
                        placeholder="0.00"
                        value={field.value === undefined ? "" : field.value}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                          const val = e.target.value
                          field.onChange(val === "" ? undefined : Number(val))
                        }}
                        aria-invalid={fieldState.invalid}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                {/* Optional Note */}
                <Controller
                  name="note"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Note (Optional)</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        placeholder="e.g. Lunch split, Project fee..."
                        aria-invalid={fieldState.invalid}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Button
                  type="submit"
                  className="w-full sm:w-auto"
                  disabled={sendMutation.isPending || isLookingUp}
                >
                  {sendMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Processing Transfer...
                    </>
                  ) : (
                    <>
                      <Send className="mr-2 h-4 w-4" />
                      Send Money
                    </>
                  )}
                </Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      </div>

      {/* Bottom Section: Transfer History Table */}
      <Card className="shadow-sm border">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg">Transfer History</CardTitle>
              <CardDescription>
                Recent P2P transactions sent from or received into your wallet.
              </CardDescription>
            </div>
            <Clock className="h-5 w-5 text-muted-foreground" />
          </div>
        </CardHeader>
        <CardContent>
          {isHistoryLoading ? (
            <div className="flex items-center justify-center py-8 text-muted-foreground gap-2">
              <Loader2 className="h-5 w-5 animate-spin" />
              <span>Loading transfer history...</span>
            </div>
          ) : !history || history.length === 0 ? (
            <div className="py-8 text-center text-sm text-muted-foreground">
              No transfers yet. Money sent or received will appear here.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="border-b text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  <tr>
                    <th className="py-3 px-2">Type</th>
                    <th className="py-3 px-2">Counterparty</th>
                    <th className="py-3 px-2">Amount</th>
                    <th className="py-3 px-2">Reference</th>
                    <th className="py-3 px-2">Date</th>
                    <th className="py-3 px-2">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {history.map((item: TransferHistoryItem) => {
                    const isSent = item.direction === "SENT"
                    return (
                      <tr key={item.transactionId} className="hover:bg-muted/40 transition-colors">
                        <td className="py-3 px-2 whitespace-nowrap">
                          <span
                            className={`inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-xs font-medium ${
                              isSent
                                ? "bg-rose-50 text-rose-700 dark:bg-rose-950 dark:text-rose-400"
                                : "bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400"
                            }`}
                          >
                            {isSent ? (
                              <>
                                <ArrowUpRight className="h-3 w-3" />
                                Sent
                              </>
                            ) : (
                              <>
                                <ArrowDownLeft className="h-3 w-3" />
                                Received
                              </>
                            )}
                          </span>
                        </td>
                        <td className="py-3 px-2 whitespace-nowrap">
                          <div className="font-medium">{item.counterpartyName}</div>
                          <div className="text-xs font-mono text-muted-foreground">
                            {item.counterpartyAccountNo}
                          </div>
                        </td>
                        <td className="py-3 px-2 whitespace-nowrap font-medium">
                          <span className={isSent ? "text-rose-600 dark:text-rose-400" : "text-emerald-600 dark:text-emerald-400"}>
                            {isSent ? "-" : "+"}৳ {Number(item.amount).toFixed(2)}
                          </span>
                        </td>
                        <td className="py-3 px-2 whitespace-nowrap font-mono text-xs text-muted-foreground">
                          {item.reference}
                        </td>
                        <td className="py-3 px-2 whitespace-nowrap text-xs text-muted-foreground">
                          {new Date(item.createdAt).toLocaleString(undefined, {
                            month: "short",
                            day: "numeric",
                            hour: "2-digit",
                            minute: "2-digit",
                          })}
                        </td>
                        <td className="py-3 px-2 whitespace-nowrap">
                          <span className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground font-medium">
                            {item.status}
                          </span>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
