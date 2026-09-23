"use client"

import { ArrowLeftRightIcon } from "lucide-react"

import { Card, CardContent } from "@/components/ui/card"

export default function TransfersPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Transfers</h1>
        <p className="text-muted-foreground">
          Send money to another user&apos;s wallet.
        </p>
      </div>

      <Card>
        <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
          <ArrowLeftRightIcon className="size-10 text-muted-foreground" />
          <div>
            <h2 className="font-medium">Coming soon</h2>
            <p className="text-sm text-muted-foreground">
              Wallet-to-wallet transfers aren&apos;t available yet.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
