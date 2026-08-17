import { apiClient } from "@/shared/api/client"
import type { AtualizarSaldoRequest, ContaResponse, CriarContaRequest } from "@/features/contas/types"

// GET /api/contas so aceita UM `?tipo=` por chamada (Controllers/ContasController.cs,
// ListarContas) - nao ha endpoint que combine tipos num unico request. Cada
// funcao aqui e uma chamada crua por tipo; quem combina banco + investimento
// (ver ESCOPO desta tela: Cartao tem pagina propria em /cartao, fica de fora)
// e o hook, nao esta camada - ver stack.md "api.ts: nao decide cache, retry,
// invalidacao ou quando chamar".
export function listarContasBanco(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=banco")
}

export function listarContasInvestimento(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=investimento")
}

// POST /api/contas (DTOs/Conta/CriarContaRequest.cs, TASK-127) - cria conta
// MANUAL Banco ou Investimento (form desta tela nao oferece Cartao, ver
// types.ts CriarContaRequest).
export function criarConta(request: CriarContaRequest): Promise<ContaResponse> {
  return apiClient.post<ContaResponse>("/api/contas", request)
}

// PATCH /api/contas/{id}/saldo (Controllers/ContasController.cs,
// AtualizarSaldo) - edita saldo_manual de conta MANUAL (banco ou
// investimento). Regra-de-negocio.md item 10: o usuario e a fonte da
// verdade do saldo, controle continuo, nao so no cadastro inicial.
export function atualizarSaldoConta(id: string, request: AtualizarSaldoRequest): Promise<void> {
  return apiClient.patch<void>(`/api/contas/${id}/saldo`, request)
}

// PATCH /api/contas/{id}/desativar (Controllers/ContasController.cs,
// DesativarConta) - soft-delete da conta (mesmo principio de exclusao
// logica ja usado em Ativo, regra-de-negocio.md item 8.3).
export function desativarConta(id: string): Promise<void> {
  return apiClient.patch<void>(`/api/contas/${id}/desativar`)
}
