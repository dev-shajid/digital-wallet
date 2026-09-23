import { api } from "@/lib/axios"
import type {
  ApiResponse,
  AuthSession,
  InitiateRegistrationPayload,
  LoginPayload,
  RefreshTokenPayload,
  ResendOtpPayload,
  VerifyEmailPayload,
} from "@/types/auth"

// ─── Step 1 of registration ───────────────────────────────────────────────────
// Sends the form data, triggers OTP email. Returns 202 with no session data.
export async function initiateRegistration(payload: InitiateRegistrationPayload) {
  const { data } = await api.post<ApiResponse<null>>(
    "/auth/registration",
    payload
  )
  return data
}

// ─── Step 2 of registration ───────────────────────────────────────────────────
// Submits the OTP. On success returns a full AuthSession (JWT + user).
export async function verifyEmail(payload: VerifyEmailPayload) {
  const { data } = await api.post<ApiResponse<AuthSession>>(
    "/auth/verify-email",
    payload
  )
  return data
}

// ─── Resend OTP ───────────────────────────────────────────────────────────────
// Regenerates and re-sends the OTP while the cache entry is still alive.
export async function resendOtp(payload: ResendOtpPayload) {
  const { data } = await api.post<ApiResponse<null>>("/auth/resend-otp", payload)
  return data
}

// ─── Login / Logout ───────────────────────────────────────────────────────────
export async function loginUser(payload: LoginPayload) {
  const { data } = await api.post<ApiResponse<AuthSession>>("/auth/login", payload)
  return data
}

export async function logoutUser(payload: RefreshTokenPayload) {
  const { data } = await api.post<ApiResponse<null>>("/auth/logout", payload)
  return data
}

