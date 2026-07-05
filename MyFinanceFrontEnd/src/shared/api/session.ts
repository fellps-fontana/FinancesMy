import type { Usuario } from "@/shared/types/user"

// Persistencia local da sessao. O backend nao tem refresh token (JWT expira
// em 8h) - quando o token para de valer, a unica saida e logar de novo.
const TOKEN_KEY = "myfinances_token"
const USER_KEY = "myfinances_user"

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function getStoredUser(): Usuario | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as Usuario
  } catch {
    return null
  }
}

export function storeAuth(token: string, usuario: Usuario): void {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(USER_KEY, JSON.stringify(usuario))
}

export function clearAuth(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}
