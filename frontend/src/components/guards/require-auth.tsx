"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuthStore } from "@/store/auth-store"

/**
 * Client-side guard that mirrors proxy.ts's checks. proxy.ts already verifies a
 * session cookie exists before this route is ever reached, so this renders its
 * children immediately (including on the server) instead of blanking the whole
 * page while the Zustand store rehydrates from localStorage - it only redirects
 * away if, once hydrated, there turns out to be no user after all (e.g. the
 * cookie survived but localStorage was cleared).
 */
export function RequireAuth({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const user = useAuthStore((state) => state.user)
  const hasHydrated = useAuthStore((state) => state.hasHydrated)

  useEffect(() => {
    if (hasHydrated && !user) {
      router.replace("/sign-in")
    }
  }, [hasHydrated, user, router])

  if (hasHydrated && !user) {
    return null
  }

  return <>{children}</>
}
