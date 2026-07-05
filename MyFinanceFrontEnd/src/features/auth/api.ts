import { apiClient } from "@/shared/api/client"
import type { LoginRequest, LoginResponse } from "@/features/auth/types"

export function login(credentials: LoginRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>("/api/auth/login", credentials)
}
