"use client"

import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import {
  CircleDollarSignIcon,
  LoaderCircleIcon,
  PlusIcon,
  WalletCardsIcon,
} from "lucide-react"

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
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { useCashIn, useWallets } from "@/hooks/use-wallets"
import { getApiErrorMessage } from "@/lib/api/error"
import type { Wallet } from "@/types/wallet"

const cashInSchema = z.object({
  amount: z.coerce
    .number()
    .min(0.0001, "Amount must be greater than zero.")
    .max(99999999999999.9999, "Amount is too large."),
})

type CashInFormValues = z.infer<typeof cashInSchema>

function formatBalance(balance: number) {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(balance)
}

function statusLabel(status: Wallet["status"]) {
  return status.charAt(0) + status.slice(1).toLowerCase()
}

export default function WalletsPage() {
  const walletsQuery = useWallets()
  const cashInMutation = useCashIn()

  const [selectedWallet, setSelectedWallet] = useState<Wallet | null>(null)
  const [isCashInOpen, setIsCashInOpen] = useState(false)

  const form = useForm<CashInFormValues>({
    resolver: zodResolver(cashInSchema),
    defaultValues: {
      amount: 0,
    },
  })

  const wallets = walletsQuery.data?.data ?? []

  function openCashIn(wallet: Wallet) {
    setSelectedWallet(wallet)
    form.reset({ amount: 0 })
    cashInMutation.reset()
    setIsCashInOpen(true)
  }

  function closeCashIn(open: boolean) {
    setIsCashInOpen(open)

    if (!open) {
      setSelectedWallet(null)
      form.reset({ amount: 0 })
      cashInMutation.reset()
    }
  }

  function onSubmit(values: CashInFormValues) {
    if (!selectedWallet) {
      return
    }

    cashInMutation.mutate(
      {
        walletId: selectedWallet.id,
        amount: values.amount,
      },
      {
        onSuccess: (response) => {
          if (response.success) {
            setIsCashInOpen(false)
            setSelectedWallet(null)
            form.reset({ amount: 0 })
          }
        },
      }
    )
  }

  if (walletsQuery.isLoading) {
    return (
      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-semibold">Wallets</h1>
          <p className="text-muted-foreground">
            View your wallets and add funds.
          </p>
        </div>

        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="h-48 animate-pulse rounded-xl bg-muted"
            />
          ))}
        </div>
      </div>
    )
  }

  if (walletsQuery.isError) {
    return (
      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-semibold">Wallets</h1>
          <p className="text-muted-foreground">
            View your wallets and add funds.
          </p>
        </div>

        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {getApiErrorMessage(walletsQuery.error)}
        </div>

        <Button
          type="button"
          variant="outline"
          onClick={() => walletsQuery.refetch()}
        >
          Try again
        </Button>
      </div>
    )
  }

  return (
    <>
      <div className="flex flex-col gap-6">
        <div>
          <h1 className="text-2xl font-semibold">Wallets</h1>
          <p className="text-muted-foreground">
            View your wallets and add funds.
          </p>
        </div>

        {wallets.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
              <WalletCardsIcon className="size-10 text-muted-foreground" />
              <div>
                <h2 className="font-medium">No wallets found</h2>
                <p className="text-sm text-muted-foreground">
                  You do not have any wallets yet.
                </p>
              </div>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {wallets.map((wallet) => {
              const canCashIn = wallet.status === "ACTIVE"

              return (
                <Card key={wallet.id}>
                  <CardHeader>
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex items-center gap-3">
                        <div className="flex size-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                          <CircleDollarSignIcon className="size-5" />
                        </div>

                        <div>
                          <CardTitle>
                            {wallet.currencyCode} Wallet
                          </CardTitle>
                          <CardDescription>
                            {wallet.currencyName}
                          </CardDescription>
                        </div>
                      </div>

                      <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-medium">
                        {statusLabel(wallet.status)}
                      </span>
                    </div>
                  </CardHeader>

                  <CardContent className="gap-5">
                    <div>
                      <p className="text-sm text-muted-foreground">
                        Available balance
                      </p>
                      <p className="text-3xl font-semibold">
                        {wallet.currencySymbol}
                        {formatBalance(wallet.balance)}
                      </p>
                    </div>

                    <Button
                      type="button"
                      className="w-full"
                      disabled={!canCashIn}
                      onClick={() => openCashIn(wallet)}
                    >
                      <PlusIcon />
                      {canCashIn ? "Cash in" : "Wallet unavailable"}
                    </Button>
                  </CardContent>
                </Card>
              )
            })}
          </div>
        )}
      </div>

      <Sheet open={isCashInOpen} onOpenChange={closeCashIn}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>
              Cash in {selectedWallet?.currencyCode ?? ""} wallet
            </SheetTitle>
            <SheetDescription>
              Add funds directly to your wallet.
            </SheetDescription>
          </SheetHeader>

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-1 flex-col"
            noValidate
          >
            <div className="flex-1 px-4">
              <FieldGroup>
                {cashInMutation.isError && (
                  <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    {getApiErrorMessage(cashInMutation.error)}
                  </div>
                )}

                <Controller
                  name="amount"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>
                        Amount
                      </FieldLabel>

                      <Input
                        {...field}
                        id={field.name}
                        type="number"
                        min="0.0001"
                        max="99999999999999.9999"
                        step="0.01"
                        placeholder="0.00"
                        aria-invalid={fieldState.invalid}
                        disabled={cashInMutation.isPending}
                        onChange={(event) => {
                          field.onChange(event.target.value)
                        }}
                      />

                      <FieldDescription>
                        Enter the amount you want to add.
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
              <Button
                type="submit"
                disabled={cashInMutation.isPending}
              >
                {cashInMutation.isPending && (
                  <LoaderCircleIcon className="animate-spin" />
                )}
                {cashInMutation.isPending ? "Processing..." : "Add funds"}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>
    </>
  )
}