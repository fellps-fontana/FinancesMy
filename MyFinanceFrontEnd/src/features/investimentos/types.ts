// Nomes de campo iguais aos DTOs do backend (Controllers/AtivosController.cs,
// DTOs/Ativo/*.cs). TipoAtivo tem DUAS serializacoes diferentes no back,
// dependendo do campo:
// - AtivoResponse.tipo passa pelo JsonStringEnumConverter global (Program.cs)
//   e chega como o NOME do enum em PascalCase: "RendaFixa" | "RendaVariavel".
// - AtivosResumoResponse.porTipo[].tipo e uma STRING simples (nao enum) que o
//   Service ja preenche com TipoAtivoExtensions.ToStorageValue() -
//   "RENDA_FIXA" | "RENDA_VARIAVEL" - por isso NAO passa pelo conversor.
// Confirmado em Services/AtivoService.cs (ObterResumo) e nos testes de
// AtivosControllerTests.cs ("tipo":"RendaFixa" no create/list vs
// Tipo == "RENDA_FIXA" no resumo). Cada tipo abaixo existe para nao misturar
// as duas serializacoes no front.
export type TipoAtivo = "RendaFixa" | "RendaVariavel"
export type TipoAtivoStorage = "RENDA_FIXA" | "RENDA_VARIAVEL"

// Ativo (regra-de-negocio.md item 8): registro STANDALONE, sem vinculo com
// Conta. valorAtual e evolucaoPercentual sao 100% manuais (item 8.1) - sem
// nenhuma API de cotacao, em nenhuma fase da v1. quantidade e precoMedio
// vem calculados pelo backend a partir do historico de aportes (item 8.1,
// media ponderada recalculada a cada aporte - ver AtivoResponse.FromAtivo
// em DTOs/Ativo/AtivoResponse.cs).
export type AtivoResponse = {
  id: string
  nome: string
  tipo: TipoAtivo
  instituicao: string
  quantidade: number
  valorInvestido: number
  valorAtual: number
  precoMedio: number
  evolucaoPercentual: number
  dataCompra: string
  ativa: boolean
}

// Cadastrar um Ativo E, na pratica, registrar o primeiro aporte dele
// (regra-de-negocio.md item 8.1) - por isso o request pede quantidade +
// precoUnitario, nunca valorInvestido direto (campo removido do contrato).
export type CriarAtivoRequest = {
  nome: string
  tipo: TipoAtivo
  instituicao: string
  quantidade: number
  precoUnitario: number
  dataCompra: string
}

export type AtualizarValorAtualRequest = {
  novoValorAtual: number
}

// Aporte individual e registro historico IMUTAVEL (regra-de-negocio.md item
// 8.1) - sem edicao nem exclusao apos criado. Nomes de campo iguais a
// DTOs/Ativo/RegistrarAporteRequest.cs e DTOs/Ativo/AtivoAporteResponse.cs.
export type RegistrarAporteRequest = {
  quantidade: number
  precoUnitario: number
  data: string
}

export type AtivoAporteResponse = {
  id: string
  ativoId: string
  data: string
  quantidade: number
  precoUnitario: number
  valorTotal: number
  criadoEm: string
}

// evolucaoPercentual e percentualDaCarteira ja chegam multiplicados por 100
// (ex: 9.09 = 9,09%), calculados no Service - ver
// Services/AtivoService.cs (CalcularEvolucaoPercentual, ObterResumo).
export type ResumoPorTipo = {
  tipo: TipoAtivoStorage
  valorAtual: number
  percentualDaCarteira: number
}

export type AtivosResumoResponse = {
  totalInvestido: number
  totalAtual: number
  porTipo: ResumoPorTipo[]
}

// --- Conta de investimento simples (cofrinho, XP sem detalhe de ativo) -
// regra-de-negocio.md item 8 ("Conta de investimento - saldo simples") e
// item 10. Modulo separado de Ativo, sem nenhuma relacao entre os dois.
// `saldo` vem sempre populado pelo backend (ContaResponse.FromConta);
// `saldoManual` e o campo editavel pelo usuario.
export type ContaResponse = {
  id: string
  nome: string
  tipo: string
  origem: string
  saldo: number
  saldoManual: number | null
  ativa: boolean
}

export type TotalInvestidoResponse = {
  totalInvestido: number
}

// Nomes de campo iguais a CriarContaRequest (DTOs/Conta/CriarContaRequest.cs):
// Tipo e obrigatorio no back (model binding falha com 400 sem ele - nao ha
// endpoint dedicado so para conta de investimento). "INVESTIMENTO" e o unico
// tipo que este formulario cria (ver TipoConta.ToStorageValue/FromStorageValue,
// case-insensitive no back).
export type CriarContaInvestimentoRequest = {
  nome: string
  tipo: "INVESTIMENTO"
  saldoManual: number
}

export type AtualizarSaldoRequest = {
  novoSaldo: number
}
