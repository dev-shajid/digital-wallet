import axios from "axios"
import type { ApiResponse } from "@/types/auth"

/** Pulls a user-facing message out of a failed request, favoring the backend's own ApiResponse envelope. */
export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiResponse<unknown>>(error)) {
    const body = error.response?.data
    if (body?.errors?.length) {
      return body.errors.map((e) => e.message).join(" ")
    }
    if (body?.message) {
      return body.message
    }
    if (error.code === "ERR_NETWORK") {
      return "Couldn't reach the server. Is the API running?"
    }
  }
  return "Something went wrong. Please try again."
}
