import { useMutation } from "@tanstack/react-query"
import { useRouter } from "next/navigation"
import { loginUser, registerUser } from "@/lib/api/auth"
import { useAuthStore } from "@/store/auth-store"
import type { AuthUser } from "@/types/auth"

function homeRouteForRole(role: AuthUser["role"]) {
  return role === "ADMIN" ? "/admin" : "/dashboard"
}

export function useLogin() {
  const router = useRouter()
  const setSession = useAuthStore((state) => state.setSession)

  return useMutation({
    mutationFn: loginUser,
    onSuccess: (response) => {
      if (!response.data) return
      setSession(response.data)
      router.push(homeRouteForRole(response.data.user.role))
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
      router.push(homeRouteForRole(response.data.user.role))
    },
  })
}

export function useLogout() {
  const router = useRouter()
  const clearSession = useAuthStore((state) => state.clearSession)

  return () => {
    clearSession()
    router.push("/sign-in")
  }
}
