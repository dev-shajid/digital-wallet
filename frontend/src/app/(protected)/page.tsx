"use client"

import { Card, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { useAuthStore } from "@/store/auth-store"

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Welcome, {user?.name}</h1>
        <p className="text-muted-foreground">Here&apos;s your account overview.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardDescription>Account Number</CardDescription>
            <CardTitle className="text-lg">{user?.accountNo}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Email</CardDescription>
            <CardTitle className="text-lg">{user?.email}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Role</CardDescription>
            <CardTitle className="text-lg">{user?.role}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Wallets & transactions</CardTitle>
          <CardDescription>
            Coming soon — wallet balances, cash-in, transfers and expenses will show up here
            once those API endpoints are available.
          </CardDescription>
        </CardHeader>
      </Card>

      {user?.role === "ADMIN" && (
        <div className="flex flex-col gap-4">
          <h2 className="text-lg font-semibold">Admin overview</h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Card>
              <CardHeader>
                <CardTitle>Users</CardTitle>
                <CardDescription>Coming soon</CardDescription>
              </CardHeader>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Currencies</CardTitle>
                <CardDescription>Coming soon</CardDescription>
              </CardHeader>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Expense categories</CardTitle>
                <CardDescription>Coming soon</CardDescription>
              </CardHeader>
            </Card>
          </div>
        </div>
      )}
    </div>
  )
}
