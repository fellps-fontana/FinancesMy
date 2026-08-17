// Tipos espelham os DTOs/Models reais do backend, conferidos em:
// - MyFinances/DTOs/Conta/ContaResponse.cs, CriarContaRequest.cs
// - MyFinances/DTOs/FaturaResponse.cs, PagarFaturaRequest.cs, SaldoCartaoResponse.cs
// - MyFinances/DTOs/CriarCompraRequest.cs
// - MyFinances/Models/TipoConta.cs, OrigemConta.cs, StatusFatura.cs,
//   TipoLancamento.cs, StatusLancamento.cs, Transferencia.cs
//
// Enums de MODEL (Tipo/Origem em ContaResponse) trafegam via JsonStringEnumConverter
// (Program.cs) usando o NOME do membro C# (ex: "Cartao", "Manual") - NAO o
// ToStorageValue() customizado ("CARTAO", "MANUAL"), que so e usado onde o
// backend converte manualmente pra string antes de montar o DTO (StatusFatura
// em FaturaResponse, que sai como "ABERTA"/"FECHADA"/"PAGA").

export type TipoConta = "Banco" | "Cartao" | "Investimento"
export type OrigemConta = "Manual" | "OpenFinance"
export type StatusFatura = "ABERTA" | "FECHADA" | "PAGA"
export type TipoLancamento = "Debit" | "Credit"
export type StatusLancamento = "Pendente" | "Sugerido" | "Pago"

export type ContaResponse = {
  id: string
  nome: string
  tipo: TipoConta
  origem: OrigemConta
  saldoManual: number | null
  ativa: boolean
  diaFechamento: number | null
  diaVencimento: number | null
  pierreAccountId: string | null
}

// POST /api/contas (ContasController.CriarConta). Tipo e string livre no
// DTO - o backend faz ToUpperInvariant() antes de comparar
// (ContaService.ConverterTipoConta), entao "CARTAO"/"Cartao"/"cartao" sao
// equivalentes na requisicao. Mantido maiusculo aqui por consistencia com os
// demais valores de enum trafegados como string no projeto (ex: StatusFatura).
export type CriarContaCartaoRequest = {
  nome: string
  tipo: "CARTAO"
  diaFechamento: number
  diaVencimento: number
}

// GET /api/contas/{id}/saldo (SaldoCartaoService.CalcularSaldoAsync). Saldo
// calculado - compras menos pagamentos menos estornos (regra de negocio item
// 12) - nunca armazenado.
export type SaldoCartaoResponse = {
  contaId: string
  saldo: number
}

// POST /api/contas/{contaId}/compras (DTOs/CriarCompraRequest.cs).
export type CriarCompraRequest = {
  descricao: string
  valor: number
  categoriaId: string | null
  data: string // yyyy-MM-dd (DateOnly do backend)
}

// Resposta crua da entidade Lancamento (Models/Lancamento.cs) - o backend
// devolve a entidade diretamente (Created/Ok), sem DTO dedicado. Tipamos so
// os campos que a UI consome.
// compraParceladaId/parcelaNumero (DTOs/CompraResponse.cs) so vem
// preenchidos quando o lancamento e uma parcela (regra de negocio item 12,
// subsecao "Parcelamento") - null/undefined numa compra a vista.
export type CompraResponse = {
  id: string
  contaId: string
  categoriaId: string | null
  descricao: string | null
  valor: number
  tipo: TipoLancamento
  data: string
  status: StatusLancamento
  faturaId: string | null
  compraParceladaId: string | null
  parcelaNumero: number | null
}

// POST /api/contas/{contaId}/compras-parceladas (DTOs/CriarCompraParceladaRequest.cs).
// Regra de negocio item 12, subsecao "Parcelamento": uma compra parcelada
// gera N Lancamentos (um por parcela), cada um vinculado a fatura do MES DE
// VENCIMENTO da propria parcela - o valor de cada parcela e calculado no
// backend (valorTotal / quantidadeParcelas, resto na ultima parcela); o
// front nunca divide o valor.
export type CriarCompraParceladaRequest = {
  descricao: string
  valorTotal: number
  quantidadeParcelas: number
  categoriaId: string | null
  dataCompra: string // yyyy-MM-dd (DateOnly do backend)
}

// DTOs/CompraParceladaResponse.cs - metadados de agrupamento da compra
// parcelada. `parcelas` e SO para exibicao (ex: "Notebook Dell 3/10") -
// fatura, projecao (item 9) e relatorio por categoria continuam somando
// cada parcela individualmente, sem ler este agrupamento.
export type CompraParceladaResponse = {
  id: string
  contaId: string
  descricao: string
  valorTotal: number
  quantidadeParcelas: number
  dataCompra: string
  parcelas: CompraResponse[]
}

// DTOs/FaturaResponse.cs - valores ja calculados por FaturaSaldoCalculator no
// backend (regra de negocio item 12); o front nao recalcula nada disso.
export type FaturaResponse = {
  id: string
  contaId: string
  dataFechamento: string
  dataVencimento: string
  status: StatusFatura
  valorTotal: number
  valorPago: number
  valorPendente: number
}

// DTOs/PagarFaturaRequest.cs. `valor` pode ser MENOR que o valorPendente da
// fatura (regra de negocio item 12: pagamento parcial e permitido - o
// backend so rejeita valor <= 0 ou valor que exceda o saldo pendente, ver
// PagamentoFaturaService.PagarFaturaAsync).
export type PagarFaturaRequest = {
  valor: number
  data: string
  contaOrigemId: string
}

// Resposta crua da entidade Transferencia (Models/Transferencia.cs),
// devolvida por PagamentoFaturaService.PagarFaturaAsync. NAO traz
// valorPago/valorPendente atualizados da fatura - a tela invalida
// useFaturas/useSaldoCartao apos o sucesso em vez de ler esses campos daqui
// (ver hooks/usePagarFatura.ts).
export type PagamentoFaturaResponse = {
  id: string
  data: string
  valor: number
  contaOrigemId: string
  contaDestinoId: string
  faturaId: string | null
  descricao: string | null
}
