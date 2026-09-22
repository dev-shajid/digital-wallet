import Cookies from "js-cookie"
import type { Role } from "@/types/auth"

/**
 * These two cookies are read by `proxy.ts` (Next's server-side middleware) to make
 * fast, optimistic redirect decisions before a page renders. They are NOT the source
 * of truth for authorization - every protected API call is still verified by the
 * backend using the JWT in the Authorization header. See the Proxy docs' note that
 * middleware checks "should not be your only line of defense".
 */
const TOKEN_COOKIE = "wallet_token"
const ROLE_COOKIE = "wallet_role"

const COOKIE_OPTIONS: Cookies.CookieAttributes = {
  path: "/",
  sameSite: "lax",
  secure: process.env.NODE_ENV === "production",
}

export function setAuthCookies(token: string, role: Role) {
  Cookies.set(TOKEN_COOKIE, token, COOKIE_OPTIONS)
  Cookies.set(ROLE_COOKIE, role, COOKIE_OPTIONS)
}

export function clearAuthCookies() {
  Cookies.remove(TOKEN_COOKIE, { path: "/" })
  Cookies.remove(ROLE_COOKIE, { path: "/" })
}

export function getTokenCookie() {
  return Cookies.get(TOKEN_COOKIE)
}
