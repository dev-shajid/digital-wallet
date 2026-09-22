import axios from "axios"
import type { InternalAxiosRequestConfig } from "axios"
import { useAuthStore } from "@/store/auth-store"
import type { ApiResponse, AuthSession } from "@/types/auth"

const baseURL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8000/api/v1"

export const api = axios.create({
  baseURL,
  headers: {
    "Content-Type": "application/json",
  },
})

// A plain client with no interceptors, used only for the refresh call itself so it can
// never trigger the response interceptor's own refresh-retry logic (which would recurse).
const refreshClient = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

type RetryableRequestConfig = InternalAxiosRequestConfig & { _retry?: boolean }

// Shared across concurrent 401s so a burst of requests triggers exactly one refresh
// call instead of one per request; everyone waiting gets the same resulting token.
let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setSession, clearSession } = useAuthStore.getState()

  if (!refreshToken) {
    clearSession()
    return null
  }

  try {
    const { data } = await refreshClient.post<ApiResponse<AuthSession>>(
      "/auth/refresh",
      { refreshToken }
    )
    if (!data.data) {
      clearSession()
      return null
    }
    setSession(data.data)
    return data.data.token
  } catch {
    clearSession()
    return null
  }
}

function redirectToSignIn() {
  if (typeof window !== "undefined") {
    // A full reload (not router.push) is deliberate here: this runs outside the React
    // tree and we want the TanStack Query cache wiped along with it.
    // eslint-disable-next-line @next/next/no-location-assign-relative-destination
    window.location.href = "/sign-in"
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (!axios.isAxiosError(error)) {
      return Promise.reject(error)
    }

    const originalRequest = error.config as RetryableRequestConfig | undefined
    const isRefreshCall = originalRequest?.url?.includes("/auth/refresh")

    if (error.response?.status !== 401 || !originalRequest || originalRequest._retry || isRefreshCall) {
      return Promise.reject(error)
    }

    // Only an authenticated session's request expiring should trigger a refresh + redirect
    // dance - an anonymous 401 (e.g. hitting a protected endpoint pre-login) just propagates.
    if (!useAuthStore.getState().token) {
      return Promise.reject(error)
    }

    originalRequest._retry = true

    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null
    })

    const newAccessToken = await refreshPromise

    if (!newAccessToken) {
      redirectToSignIn()
      return Promise.reject(error)
    }

    originalRequest.headers.Authorization = `Bearer ${newAccessToken}`
    return api(originalRequest)
  }
)
