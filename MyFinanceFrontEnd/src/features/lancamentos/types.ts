// Espelha LancamentoResponseDto (MyFinances/DTOs/LancamentoResponseDto.cs).
// `tipo` e a fonte confiavel de entrada/saida (regra-de-negocio.md item 2,
// CRITICA) - o sinal de `valor` nunca deve ser usado pra classificar. `oculto`
// e o soft-delete de lancamento Open Finance (item 4, fora de escopo v1, mas
// o campo ja existe no schema); `manual` distingue a origem (item 1).
export type TipoLancamento = "DEBIT" | "CREDIT"

// PENDENTE -> PAGO e o unico caminho manual em v1 (item 5). SUGERIDO so passa
// a existir quando a conciliacao com Open Finance entrar em v2 - o tipo ja
// inclui os 3 valores do backend, mesmo sem nenhum form oferecendo SUGERIDO
// como opcao ainda.
export type StatusLancamento = "PENDENTE" | "SUGERIDO" | "PAGO"

export type LancamentoResponse = {
  id: string
  contaId: string
  categoriaId: string | null
  descricao: string | null
  valor: number
  tipo: TipoLancamento
  data: string
  status: StatusLancamento
  manual: boolean
  oculto: boolean
}

// Espelha CriarLancamentoRequest.cs (Descricao/Valor/Tipo/Data/Status
// obrigatorios; CategoriaId opcional).
export type CriarLancamentoRequest = {
  descricao: string
  valor: number
  categoriaId?: string
  tipo: TipoLancamento
  data: string
  status: StatusLancamento
}

// Espelha EditarLancamentoRequest.cs - unica diferenca do Criar e `status`
// opcional no backend (string? em vez de required string).
export type EditarLancamentoRequest = {
  descricao: string
  valor: number
  categoriaId?: string
  tipo: TipoLancamento
  data: string
  status?: StatusLancamento
}

// Espelha TransferenciaResponse.cs. Transferencia entre contas de mesma
// titularidade (regra-de-negocio.md itens 3 e 12) - nao e gasto nem receita,
// so muda dinheiro de lugar. `contaDestinoId` e opcional pois o emprestimo
// (item 13) usa a mesma estrutura com uma perna so (destino fica null).
export type TransferenciaResponse = {
  id: string
  data: string
  valor: number
  contaOrigemId: string
  contaDestinoId: string | null
  descricao: string | null
}

// Espelha CriarTransferenciaRequest.cs. ContaDestinoId e required no backend
// para o fluxo de transferencia comum (item 3); o caso de perna unica do
// emprestimo (item 13) nao passa por este endpoint.
export type CriarTransferenciaRequest = {
  contaOrigemId: string
  contaDestinoId: string
  valor: number
  data: string
  descricao?: string
}
