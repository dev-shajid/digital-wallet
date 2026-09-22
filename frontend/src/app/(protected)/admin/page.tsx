"use client"

import { Card, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { RequireAuth } from "@/components/guards/require-auth"
import { useAuthStore } from "@/store/auth-store"

function AdminOverview() {
  const user = useAuthStore((state) => state.user)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Admin console</h1>
        <p className="text-muted-foreground">Signed in as {user?.email} (ADMIN).</p>
      </div>
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
  )
}

export default function AdminPage() {
  return (
    <RequireAuth allowedRoles={["ADMIN"]}>
      <AdminOverview />
    </RequireAuth>
  )
}
