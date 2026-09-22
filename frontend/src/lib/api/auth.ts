import { api } from "@/lib/axios"
import type {
  ApiResponse,
  AuthSession,
  LoginPayload,
  RefreshTokenPayload,
  RegisterPayload,
} from "@/types/auth"

export async function registerUser(payload: RegisterPayload) {
  const { data } = await api.post<ApiResponse<AuthSession>>(
    "/auth/register",
    payload
  )
  return data
}

export async function loginUser(payload: LoginPayload) {
  const { data } = await api.post<ApiResponse<AuthSession>>(
    "/auth/login",
    payload
  )
  return data
}

export async function logoutUser(payload: RefreshTokenPayload) {
  const { data } = await api.post<ApiResponse<null>>("/auth/logout", payload)
  return data
}
