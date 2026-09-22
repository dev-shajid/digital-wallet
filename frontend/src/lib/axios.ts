import axios from "axios"
import { useAuthStore } from "@/store/auth-store"

export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "http://localhost:8000/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      const { token, clearSession } = useAuthStore.getState()
      // Only force a logout if we *thought* we were signed in - an anonymous 401
      // (e.g. hitting a protected endpoint pre-login) shouldn't redirect anyone.
      if (token) {
        clearSession()
        if (typeof window !== "undefined") {
          // A full reload (not router.push) is deliberate here: this runs outside
          // the React tree and we want the TanStack Query cache wiped along with it.
          // eslint-disable-next-line @next/next/no-location-assign-relative-destination
          window.location.href = "/sign-in"
        }
      }
    }
    return Promise.reject(error)
  }
)
