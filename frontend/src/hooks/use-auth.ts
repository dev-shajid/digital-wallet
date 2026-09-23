import { useMutation } from "@tanstack/react-query"
import { useRouter } from "next/navigation"
import {
  initiateRegistration,
  loginUser,
  logoutUser,
  resendOtp,
  verifyEmail,
} from "@/lib/api/auth"
import { useAuthStore } from "@/store/auth-store"

// Every role lands on the same route after auth — / decides what to show based
// on the signed-in user's role, rather than routing roles to different URLs.
const HOME_ROUTE = "/"

// ─── Login ────────────────────────────────────────────────────────────────────
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

// ─── Registration Step 1: initiate (send OTP) ─────────────────────────────────
// On success, calls `onOtpSent` to tell the form to switch to the OTP screen.
// Does NOT touch the session — no user is created yet.
export function useInitiateRegistration(onOtpSent: () => void) {
  return useMutation({
    mutationFn: initiateRegistration,
    onSuccess: () => {
      onOtpSent()
    },
  })
}

// ─── Registration Step 2: verify OTP → create account → log in ───────────────
export function useVerifyEmail() {
  const router = useRouter()
  const setSession = useAuthStore((state) => state.setSession)

  return useMutation({
    mutationFn: verifyEmail,
    onSuccess: (response) => {
      if (!response.data) return
      // OTP verified → user + wallet created → session returned — log in immediately.
      setSession(response.data)
      router.push(HOME_ROUTE)
    },
  })
}

// ─── Resend OTP ───────────────────────────────────────────────────────────────
// `onResent` is called with a success message so the form can show feedback.
export function useResendOtp(onResent: (message: string) => void) {
  return useMutation({
    mutationFn: resendOtp,
    onSuccess: (response) => {
      onResent(response.message ?? "A new code has been sent to your email.")
    },
  })
}

// ─── Logout ───────────────────────────────────────────────────────────────────
export function useLogout() {
  const router = useRouter()
  const clearSession = useAuthStore((state) => state.clearSession)

  return () => {
    const { refreshToken } = useAuthStore.getState()

    // Clear local state and navigate right away — logout should feel instant.
    clearSession()
    router.push("/sign-in")

    if (refreshToken) {
      logoutUser({ refreshToken }).catch(() => {})
    }
  }
}

