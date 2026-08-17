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

// Espelha ClassificacaoLancamento (Domain/ClassificacaoLancamento.cs) via
// LancamentoResponseDto.Classificacao. TRANSFERENCIA cobre tanto
// transferencia entre contas quanto pagamento de fatura de cartao (item 12:
// pagamento de fatura e transferencia conta corrente -> cartao). Fonte da
// verdade para o que soma no resumo do mes / filtro de chip
// (regra-de-negocio.md itens 2 CRITICA, 3 e 12) - nunca usar `tipo` isolado
// nem o sinal de `valor` pra essa decisao.
export type ClassificacaoLancamento = "ENTRADA" | "SAIDA" | "TRANSFERENCIA" | "COMPETENCIA_CARTAO"

export type LancamentoResponse = {
  id: string
  contaId: string
  categoriaId: string | null
  descricao: string | null
  valor: number
  tipo: TipoLancamento
  classificacao: ClassificacaoLancamento
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

// Espelha o item de transferencia devolvido por GET /api/lancamentos/fluxo-caixa
// (endpoint agregado, ver FluxoCaixaItem abaixo) - forma resumida da
// Transferencia (Models/Transferencia.cs), sem os campos administrativos que
// TransferenciaResponse ja cobre para o fluxo de criacao. `contaDestinoId`
// null = emprestimo de perna unica (regra-de-negocio.md item 13: "destino e
// uma pessoa fora do sistema"). `ehPagamentoFatura` distingue a transferencia
// comum (item 3) do pagamento de fatura de cartao (item 12) - ambos chegam
// pela mesma estrutura de duas pernas, a flag e o que direciona a UI a
// mostrar "Pagamento de fatura" em vez de "Conta A -> Conta B".
export type TransferenciaFluxoCaixa = {
  id: string
  data: string
  valor: number
  contaOrigemId: string
  contaDestinoId: string | null
  ehPagamentoFatura: boolean
  descricao: string | null
}

// Espelha o item de uniao devolvido por GET /api/lancamentos/fluxo-caixa
// (LancamentosController, endpoint agregado que substitui o antigo
// fluxo-caixa por conta) - cada linha do fluxo de caixa e OU um Lancamento
// avulso OU uma Transferencia (uma unica linha logica por transferencia,
// regra-de-negocio.md item 3: "no fluxo de caixa a transferencia aparece
// como uma unica linha logica"). `data` fica duplicado no nivel superior
// (igual ao campo interno) so pra permitir filtrar/agrupar por periodo
// (lib/filtrarPeriodo.ts) sem precisar checar `tipoItem` antes.
export type FluxoCaixaItem =
  | { tipoItem: "LANCAMENTO"; data: string; lancamento: LancamentoResponse; transferencia: null }
  | { tipoItem: "TRANSFERENCIA"; data: string; lancamento: null; transferencia: TransferenciaFluxoCaixa }

// Forma minima de Conta usada so para resolver id -> nome na exibicao de
// transferencia (components/TransferenciaFluxoCaixaItem.tsx). Deliberadamente
// mais enxuto que ContaResponse (features/contas/types.ts ou
// features/cartao/types.ts) - este arquivo nao precisa de tipo/origem/saldo,
// so do necessario pro lookup visual, evitando acoplar a um contrato de Conta
// de outra feature.
export type ContaParaExibicao = {
  id: string
  nome: string
}
