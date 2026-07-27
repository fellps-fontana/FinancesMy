// Mesmo padrao de contas-fixas/query-keys.ts: chave centralizada por
// contaId, ja que todo endpoint de LancamentosController vive sob
// /api/contas/{contaId}/lancamentos - invalidar o fluxo de caixa de uma
// conta especifica basta pra refletir criar/editar/pagar/remover lancamento.
export const lancamentosKeys = {
  all: ["lancamentos"] as const,
  fluxoCaixa: (contaId: string) => [...lancamentosKeys.all, "fluxoCaixa", contaId] as const,
}
