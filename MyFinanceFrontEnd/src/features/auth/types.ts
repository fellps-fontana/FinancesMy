import type { Usuario } from "@/shared/types/user"

// Nomes de campo iguais ao LoginRequest do backend - nao renomear para
// camelCase "correto" em ingles, o contrato e esse.
export type LoginRequest = {
  usernameOrEmail: string
  senha: string
}

export type LoginResponse = {
  token: string
  usuario: Usuario
}
