import { ApiError, apiClient } from "@/shared/api/client"
import type {
  AtualizarSaldoRequest,
  ContaResponse,
  CriarContaRequest,
  EditarContaRequest,
} from "@/features/contas/types"

// Erros de regra conhecidos de PATCH /desativar (409: conta tem ativo ativo) e
// PATCH /saldo (422: saldo invalido). Estendem ApiError para continuar
// compativeis com quem trata `instanceof ApiError`; `tipo` permite a UI
// distinguir o caso e `message` e o texto `erro` vindo do backend.
export type TipoErroContaOperacao = "CONTA_COM_ATIVO_ATIVO" | "SALDO_INVALIDO"

export class ContaOperacaoError extends ApiError {
  readonly tipo: TipoErroContaOperacao

  constructor(tipo: TipoErroContaOperacao, original: ApiError) {
    super(original.status, original.message, original.body)
    this.name = "ContaOperacaoError"
    this.tipo = tipo
  }
}

function lerMensagemDoCorpo(erro: ApiError): string {
  const corpo = erro.body
  if (typeof corpo === "object" && corpo !== null && "erro" in corpo) {
    const mensagem = (corpo as { erro: unknown }).erro
    if (typeof mensagem === "string" && mensagem.length > 0) return mensagem
  }
  return erro.message
}

// Converte 409/422 em erro tipado com a mensagem de body.erro; outros erros
// seguem como vieram.
export function tipificarErroConta(erro: unknown, tipo: TipoErroContaOperacao, status: number): unknown {
  if (erro instanceof ApiError && erro.status === status) {
    return new ContaOperacaoError(tipo, new ApiError(erro.status, lerMensagemDoCorpo(erro), erro.body))
  }
  return erro
}

// GET /api/contas so aceita UM `?tipo=` por chamada (Controllers/ContasController.cs,
// ListarContas) - nao ha endpoint que combine tipos num unico request. Cada
// funcao aqui e uma chamada crua por tipo; quem combina banco + investimento
// (ver ESCOPO desta tela: Cartao tem pagina propria em /cartao, fica de fora)
// e o hook, nao esta camada - ver stack.md "api.ts: nao decide cache, retry,
// invalidacao ou quando chamar".
export function listarContasBanco(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=banco")
}

export function listarContasCartao(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=cartao")
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

// PUT /api/contas/{id} - edita nome, subtipo (Banco), icone e cor. 400: nome
// vazio/subtipo invalido; 404: conta inexistente; 409: conta inativa.
export function editarConta(id: string, request: EditarContaRequest): Promise<ContaResponse> {
  return apiClient.put<ContaResponse>(`/api/contas/${id}`, request)
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
