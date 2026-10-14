import { clearAuth, getToken } from "@/shared/api/session"

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

// Falha rapido em dev se o .env nao foi configurado, em vez de deixar toda
// chamada quebrar silenciosamente com uma URL relativa errada.
if (!API_BASE_URL) {
  throw new Error(
    "VITE_API_BASE_URL nao configurada. Copie .env.example para .env e ajuste.",
  )
}

type ApiErrorBody = {
  erro: string
}

export class ApiError extends Error {
  readonly status: number
  // Corpo da resposta ja parseado (null se nao era JSON). Permite a features
  // ler campos extras do erro, ex.: `origemVinculo` no 409 de lancamento.
  readonly body: unknown

  constructor(status: number, erro: string, body: unknown = null) {
    super(erro)
    this.name = "ApiError"
    this.status = status
    this.body = body
  }
}

type RequestOptions = {
  method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE"
  body?: unknown
}

async function request<TResponse>(
  path: string,
  { method, body }: RequestOptions,
): Promise<TResponse> {
  const token = getToken()
  const headers = new Headers({ "Content-Type": "application/json" })
  if (token) {
    headers.set("Authorization", `Bearer ${token}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  const data = await response.json().catch(() => null)

  // Token so existe apos login e o backend nao tem refresh token (JWT de 8h).
  // 401 numa chamada ja autenticada so pode significar sessao morta.
  if (response.status === 401 && token) {
    clearAuth()
    window.location.assign("/login")
  }

  if (!response.ok) {
    const erro = (data as ApiErrorBody | null)?.erro ?? "Erro inesperado. Tente novamente."
    throw new ApiError(response.status, erro, data)
  }

  return data as TResponse
}

export const apiClient = {
  get: <TResponse>(path: string) => request<TResponse>(path, { method: "GET" }),
  post: <TResponse>(path: string, body?: unknown) =>
    request<TResponse>(path, { method: "POST", body }),
  put: <TResponse>(path: string, body?: unknown) =>
    request<TResponse>(path, { method: "PUT", body }),
  patch: <TResponse>(path: string, body?: unknown) =>
    request<TResponse>(path, { method: "PATCH", body }),
  delete: <TResponse>(path: string) => request<TResponse>(path, { method: "DELETE" }),
}
