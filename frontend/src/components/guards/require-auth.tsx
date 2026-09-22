"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuthStore } from "@/store/auth-store"
import type { Role } from "@/types/auth"

interface RequireAuthProps {
  children: React.ReactNode
  /** When set, only these roles may view the children; everyone else is redirected. */
  allowedRoles?: Role[]
}

/**
 * Client-side guard that mirrors proxy.ts's checks. proxy.ts only reads cookies
 * optimistically before the page renders; this catches the case where the cookie
 * and the Zustand/localStorage session have drifted (e.g. cookie expired) and
 * covers client-side navigations proxy already let through.
 */
export function RequireAuth({ children, allowedRoles }: RequireAuthProps) {
  const router = useRouter()
  const user = useAuthStore((state) => state.user)
  const hasHydrated = useAuthStore((state) => state.hasHydrated)

  useEffect(() => {
    if (!hasHydrated) return

    if (!user) {
      router.replace("/sign-in")
      return
    }

    if (allowedRoles && !allowedRoles.includes(user.role)) {
      router.replace("/dashboard")
    }
  }, [hasHydrated, user, allowedRoles, router])

  if (!hasHydrated || !user) {
    return null
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return null
  }

  return <>{children}</>
}
