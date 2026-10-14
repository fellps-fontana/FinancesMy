import { ApiError } from "@/shared/api/client"
import { LancamentoNaoAvulsoError } from "@/features/lancamentos/types"

// Erros conhecidos da camada de dados trazem mensagem pronta para o usuario
// (400/409 do backend); qualquer outro vira o texto de contingencia.
export function mensagemDeErro(error: unknown, fallback: string): string {
  if (error instanceof ApiError || error instanceof LancamentoNaoAvulsoError) {
    return error.message
  }
  return fallback
}
