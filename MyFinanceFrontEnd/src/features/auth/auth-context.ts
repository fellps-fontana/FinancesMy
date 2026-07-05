import { createContext } from "react"
import type { LoginRequest } from "@/features/auth/types"
import type { Usuario } from "@/shared/types/user"

export type AuthContextValue = {
  usuario: Usuario | null
  isAuthenticated: boolean
  isLoggingIn: boolean
  loginError: string | null
  login: (credentials: LoginRequest) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
