import { useMutation } from "@tanstack/react-query"
import { useRouter } from "next/navigation"
import { loginUser, logoutUser, registerUser } from "@/lib/api/auth"
import { useAuthStore } from "@/store/auth-store"

// Every role lands on the same route after auth - /dashboard decides what to show
// based on the signed-in user's role, rather than routing roles to different URLs.
const HOME_ROUTE = "/dashboard"

export function useLogin() {
  const router = useRouter()
  const setSession = useAuthStore((state) => state.setSession)

  return useMutation({
    mutationFn: loginUser,
    onSuccess: (response) => {
      if (!response.data) return
      setSession(response.data)
      router.push(HOME_ROUTE)
    },
  })
}

export function useRegister() {
  const router = useRouter()
  const setSession = useAuthStore((state) => state.setSession)

  return useMutation({
    mutationFn: registerUser,
    onSuccess: (response) => {
      if (!response.data) return
      // Registration returns a token too, so the user is logged in immediately -
      // no separate trip through the sign-in form.
      setSession(response.data)
      router.push(HOME_ROUTE)
    },
  })
}

export function useLogout() {
  const router = useRouter()
  const clearSession = useAuthStore((state) => state.clearSession)

  return () => {
    const { refreshToken } = useAuthStore.getState()

    // Clear local state and navigate right away - logout should feel instant. The
    // server-side revoke is best-effort: the local session is gone either way, so a
    // failed request here (e.g. offline) doesn't need to block or be surfaced.
    clearSession()
    router.push("/sign-in")

    if (refreshToken) {
      logoutUser({ refreshToken }).catch(() => {})
    }
  }
}
