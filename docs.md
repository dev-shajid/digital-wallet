# Digital Wallet & Expense Management - Project Docs

What's built so far and how to call it. See [database-schema.md](database-schema.md) for the tables.

Base URL: `http://localhost:8000/api/v1`

All responses use the same envelope:
```json
{ "success": true, "status": 200, "message": "...", "data": { }, "errors": null }
```
On validation errors, `errors` is a list of `{ field, message }`.

---

## Built so far

- User registration + login (JWT access token + refresh token)
- Refresh token rotation and logout
- Get current logged-in user's profile
- Health check / diagnostics endpoints

Not built yet: wallets, currencies, transfers, expenses, cash-in, admin endpoints, dashboards.

---

## Auth

### Register
`POST /auth/register`

Headers: `Content-Type: application/json`

Payload:
```json
{
  "name": "Jane Doe",
  "email": "jane@example.com",
  "password": "secret123"
}
```
- name: required, 2-200 chars
- email: required, valid email, unique
- password: required, min 6 chars

Creates the user and their BDT wallet (balance 0) together. Returns `201` with the same shape as login (token + refresh token + user info), so the user is signed in right away.

### Login
`POST /auth/login`

Headers: `Content-Type: application/json`

Payload:
```json
{ "email": "jane@example.com", "password": "secret123" }
```

Response `data`:
```json
{
  "token": "...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "refreshToken": "...",
  "refreshTokenExpiresIn": 604800,
  "user": {
    "userId": "...",
    "name": "Jane Doe",
    "email": "jane@example.com",
    "accountNo": "AC00000001",
    "role": "USER",
    "createdAt": "..."
  }
}
```
`401` on wrong email/password.

### Refresh
`POST /auth/refresh`

Payload: `{ "refreshToken": "..." }`

Returns a new token + refresh token pair, same shape as login. The old refresh token is revoked (rotated) as part of this call — using it again fails.

### Logout
`POST /auth/logout`

Payload: `{ "refreshToken": "..." }`

Revokes the refresh token. `data` is `null`.

### Get current user
`GET /users/me`

Headers: `Authorization: Bearer <token>`

Returns the logged-in user's profile (same shape as `user` above). `401` if the token is missing/expired.

---

## Diagnostics

`GET /diagnostics/health` — no auth, returns `{ "status": "Healthy" }`.

`GET /health`, `/health/live`, `/health/ready` — plain health checks (not under `/api/v1`), used for uptime/DB checks, not part of the app API.

---

## Auth notes

- Access token is a JWT, sent as `Authorization: Bearer <token>` on every protected request.
- Access token expires in ~1 hour; use the refresh token to get a new one without logging in again.
- Passwords are hashed before storage, never stored or returned in plain text.
- Roles: `USER`, `ADMIN`. Admin-only endpoints don't exist yet.
