import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

// Next.js 16 renamed "Middleware" to "Proxy" (same file convention, same API) -
// see node_modules/next/dist/docs/01-app/01-getting-started/16-proxy.md.
//
// This performs *optimistic* auth checks only, reading the lightweight
// `wallet_token` cookie set by the client after login (see src/lib/auth-cookies.ts).
// It is not the source of truth: every protected backend endpoint independently
// verifies the JWT. The client-side <RequireAuth> guard
// (src/components/guards/require-auth.tsx) double-checks this on the client in
// case the cookie and the persisted Zustand session ever drift.
//
// There's no role-based routing here on purpose: every user, regardless of role,
// lands on the same routes (/dashboard, /profile) - pages decide what to render
// based on the signed-in user's role, not which URL they're on.

const HOME_ROUTE = "/dashboard"
const AUTH_ROUTES = ["/sign-in", "/sign-up"]
const PROTECTED_ROUTE_PREFIXES = [
  "/dashboard",
  "/wallets",
  "/profile",
]

function matchesPrefix(pathname: string, prefixes: string[]) {
  return prefixes.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`))
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl
  const token = request.cookies.get("wallet_token")?.value

  const isProtectedRoute = matchesPrefix(pathname, PROTECTED_ROUTE_PREFIXES)
  const isAuthRoute = AUTH_ROUTES.includes(pathname)

  // Not logged in and trying to reach a protected route -> bounce to sign-in,
  // remembering where they were headed.
  if (isProtectedRoute && !token) {
    const signInUrl = new URL("/sign-in", request.url)
    signInUrl.searchParams.set("from", pathname)
    return NextResponse.redirect(signInUrl)
  }

  // Already logged in and visiting /sign-in or /sign-up -> no point, send them in.
  if (isAuthRoute && token) {
    return NextResponse.redirect(new URL(HOME_ROUTE, request.url))
  }

  return NextResponse.next()
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)"],
}
