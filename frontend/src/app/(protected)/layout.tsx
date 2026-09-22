"use client"

import { LogOut, Wallet } from "lucide-react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { RequireAuth } from "@/components/guards/require-auth"
import { useLogout } from "@/hooks/use-auth"
import { useAuthStore } from "@/store/auth-store"

function ProtectedHeader() {
  const user = useAuthStore((state) => state.user)
  const logout = useLogout()

  return (
    <header className="flex items-center justify-between border-b px-6 py-4">
      <Link href={user?.role === "ADMIN" ? "/admin" : "/dashboard"} className="flex items-center gap-2 font-medium">
        <div className="flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground">
          <Wallet className="size-4" />
        </div>
        Digital Wallet
      </Link>
      <div className="flex items-center gap-4">
        {user && (
          <span className="text-sm text-muted-foreground">
            {user.name} · {user.accountNo} · <span className="font-medium">{user.role}</span>
          </span>
        )}
        <Button variant="outline" size="sm" onClick={logout}>
          <LogOut className="size-4" />
          Log out
        </Button>
      </div>
    </header>
  )
}

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <RequireAuth>
      <div className="flex min-h-svh flex-col">
        <ProtectedHeader />
        <main className="flex flex-1 flex-col p-6">{children}</main>
      </div>
    </RequireAuth>
  )
}
