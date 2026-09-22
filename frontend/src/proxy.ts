import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

// Next.js 16 renamed "Middleware" to "Proxy" (same file convention, same API) -
// see node_modules/next/dist/docs/01-app/01-getting-started/16-proxy.md.
//
// This performs *optimistic* auth/role checks only, reading the lightweight
// `wallet_token` / `wallet_role` cookies set by the client after login (see
// src/lib/auth-cookies.ts). It is not the source of truth: every protected
// backend endpoint independently verifies the JWT and the user's role. The
// client-side <RequireAuth> guard (src/components/guards/require-auth.tsx)
// double-checks this on the client in case the cookie and the persisted
// Zustand session ever drift (e.g. an expired cookie).

const AUTH_ROUTES = ["/sign-in", "/sign-up"]
const PROTECTED_ROUTE_PREFIXES = ["/dashboard", "/admin"]
const ADMIN_ONLY_PREFIXES = ["/admin"]

function matchesPrefix(pathname: string, prefixes: string[]) {
  return prefixes.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`))
}

function homeRouteForRole(role: string | undefined) {
  return role === "ADMIN" ? "/admin" : "/dashboard"
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl
  const token = request.cookies.get("wallet_token")?.value
  const role = request.cookies.get("wallet_role")?.value

  const isProtectedRoute = matchesPrefix(pathname, PROTECTED_ROUTE_PREFIXES)
  const isAdminOnlyRoute = matchesPrefix(pathname, ADMIN_ONLY_PREFIXES)
  const isAuthRoute = AUTH_ROUTES.includes(pathname)

  // Not logged in and trying to reach a protected route -> bounce to sign-in,
  // remembering where they were headed.
  if (isProtectedRoute && !token) {
    const signInUrl = new URL("/sign-in", request.url)
    signInUrl.searchParams.set("from", pathname)
    return NextResponse.redirect(signInUrl)
  }

  // Logged in but wrong role for an admin-only route -> send to their own home.
  if (isAdminOnlyRoute && token && role !== "ADMIN") {
    return NextResponse.redirect(new URL(homeRouteForRole(role), request.url))
  }

  // Already logged in and visiting /sign-in or /sign-up -> no point, send them in.
  if (isAuthRoute && token) {
    return NextResponse.redirect(new URL(homeRouteForRole(role), request.url))
  }

  return NextResponse.next()
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)"],
}
