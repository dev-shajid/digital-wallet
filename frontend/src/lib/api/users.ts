import { api } from "@/lib/axios"
import type { ApiResponse, AuthUser } from "@/types/auth"

export async function getCurrentUser() {
  const { data } = await api.get<ApiResponse<AuthUser>>("/users/me")
  return data
}
