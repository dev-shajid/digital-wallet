export type Role = "USER" | "ADMIN"

export interface AuthUser {
  userId: string
  name: string
  email: string
  accountNo: string
  role: Role
  createdAt: string
}

export interface AuthSession {
  token: string
  tokenType: string
  expiresIn: number
  refreshToken: string
  refreshTokenExpiresIn: number
  user: AuthUser
}

export interface RefreshTokenPayload {
  refreshToken: string
}

export interface RegisterPayload {
  name: string
  email: string
  password: string
}

export interface LoginPayload {
  email: string
  password: string
}

export interface ApiError {
  field: string | null
  message: string
}

export interface ApiResponse<T> {
  success: boolean
  status: number
  message: string
  data: T | null
  errors: ApiError[] | null
}
