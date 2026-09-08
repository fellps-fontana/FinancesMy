import { apiClient } from "@/shared/api/client"
import type { LoginRequest, LoginResponse, RegistrarUsuarioRequest } from "@/features/auth/types"
import type { Usuario } from "@/shared/types/user"

export function login(credentials: LoginRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>("/api/auth/login", credentials)
}

export function registrar(data: RegistrarUsuarioRequest): Promise<Usuario> {
  return apiClient.post<Usuario>("/api/auth/registrar", data)
}
