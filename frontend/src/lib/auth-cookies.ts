import Cookies from "js-cookie"

/**
 * Read by `proxy.ts` (Next's server-side middleware) to make a fast, optimistic
 * "is this request authenticated" redirect decision before a page renders. It is
 * NOT the source of truth for authorization - every protected API call is still
 * verified by the backend using the JWT in the Authorization header. See the
 * Proxy docs' note that middleware checks "should not be your only line of defense".
 */
const TOKEN_COOKIE = "wallet_token"

const COOKIE_OPTIONS: Cookies.CookieAttributes = {
  path: "/",
  sameSite: "lax",
  secure: process.env.NODE_ENV === "production",
}

export function setAuthCookies(token: string) {
  Cookies.set(TOKEN_COOKIE, token, COOKIE_OPTIONS)
}

export function clearAuthCookies() {
  Cookies.remove(TOKEN_COOKIE, { path: "/" })
}

export function getTokenCookie() {
  return Cookies.get(TOKEN_COOKIE)
}
