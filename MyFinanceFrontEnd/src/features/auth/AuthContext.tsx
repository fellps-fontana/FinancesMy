import { useCallback, useMemo, useState, type ReactNode } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { clearAuth, getStoredUser, storeAuth } from "@/shared/api/session"
import { ApiError } from "@/shared/api/client"
import { login as loginRequest } from "@/features/auth/api"
import { AuthContext, type AuthContextValue } from "@/features/auth/auth-context"
import type { LoginRequest } from "@/features/auth/types"
import type { Usuario } from "@/shared/types/user"

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [usuario, setUsuario] = useState<Usuario | null>(() => getStoredUser())
  const [isLoggingIn, setIsLoggingIn] = useState(false)
  const [loginError, setLoginError] = useState<string | null>(null)

  const login = useCallback(async (credentials: LoginRequest) => {
    setIsLoggingIn(true)
    setLoginError(null)

    try {
      const response = await loginRequest(credentials)
      storeAuth(response.token, response.usuario)
      setUsuario(response.usuario)
    } catch (error) {
      const message =
        error instanceof ApiError ? error.message : "Nao foi possivel entrar. Tente novamente."
      setLoginError(message)
      throw error
    } finally {
      setIsLoggingIn(false)
    }
  }, [])

  const logout = useCallback(() => {
    clearAuth()
    setUsuario(null)
    // sem isso o cache de queries do usuario anterior sobrevive ao logout
    // manual (o redirect forcado do 401 em client.ts mata o processo JS e
    // limpa de graca, mas aqui a troca e so de estado)
    queryClient.clear()
  }, [queryClient])

  const value = useMemo<AuthContextValue>(
    () => ({
      usuario,
      isAuthenticated: usuario !== null,
      isLoggingIn,
      loginError,
      login,
      logout,
    }),
    [usuario, isLoggingIn, loginError, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
