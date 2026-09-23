"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuthStore } from "@/store/auth-store"
import type { Role } from "@/types/auth"

/**
 * Used inside a page that's already behind <RequireAuth> (see the (protected)
 * layout) to further restrict it to one role - e.g. admin-only management
 * pages. Renders nothing and bounces home if the signed-in user's role
 * doesn't match; the backend enforces the real authorization independently.
 */
export function RequireRole({
  role,
  children,
}: {
  role: Role
  children: React.ReactNode
}) {
  const router = useRouter()
  const currentRole = useAuthStore((state) => state.user?.role)
  const hasHydrated = useAuthStore((state) => state.hasHydrated)

  const isAllowed = currentRole === role

  useEffect(() => {
    if (hasHydrated && !isAllowed) {
      router.replace("/")
    }
  }, [hasHydrated, isAllowed, router])

  if (hasHydrated && !isAllowed) {
    return null
  }

  return <>{children}</>
}
