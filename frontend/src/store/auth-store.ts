import { create } from "zustand"
import { persist } from "zustand/middleware"
import type { AuthSession, AuthUser } from "@/types/auth"
import { clearAuthCookies, setAuthCookies } from "@/lib/auth-cookies"

interface AuthState {
  user: AuthUser | null
  token: string | null
  refreshToken: string | null
  /** True once the persisted state has been read back from localStorage on the client. */
  hasHydrated: boolean
  setSession: (session: AuthSession) => void
  clearSession: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      refreshToken: null,
      hasHydrated: false,
      setSession: (session) => {
        setAuthCookies(session.token, session.user.role)
        set({
          user: session.user,
          token: session.token,
          refreshToken: session.refreshToken,
        })
      },
      clearSession: () => {
        clearAuthCookies()
        set({ user: null, token: null, refreshToken: null })
      },
    }),
    {
      name: "wallet-auth",
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        refreshToken: state.refreshToken,
      }),
      onRehydrateStorage: () => () => {
        useAuthStore.setState({ hasHydrated: true })
      },
    }
  )
)
