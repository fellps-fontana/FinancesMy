// Chave centralizada por prefixo (`all`). Toda mutacao de lancamento/
// transferencia invalida `lancamentosKeys.all` inteiro (React Query casa por
// prefixo quando `exact` nao e passado) - cobre num so lugar tanto a visao
// agregada quanto a chave legada por conta, sem precisar listar cada uma.
export const lancamentosKeys = {
  all: ["lancamentos"] as const,
  // Legada: ainda usada por hooks/useFluxoCaixa.ts, que por sua vez so
  // sobrevive porque features/dashboard/hooks/useUltimosLancamentos.ts
  // depende dele (fora do escopo desta tarefa - ver api.ts).
  fluxoCaixa: (contaId: string) => [...lancamentosKeys.all, "fluxoCaixa", contaId] as const,
  // Visao agregada (todas as contas) que LancamentosPage.tsx consome por
  // padrao a partir desta tarefa.
  fluxoCaixaTodasContas: () => [...lancamentosKeys.all, "fluxoCaixaTodasContas"] as const,
  // Contas (banco + investimento + cartao) usadas so pra resolver nome de
  // conta na exibicao de transferencia (components/TransferenciaFluxoCaixaItem.tsx).
  // Nao entra em calculo de lancamento, mas fica nesta chave por conveniencia -
  // invalidar `all` tambem revalida essa lista, custo desprezivel.
  contasParaExibicaoTransferencia: () =>
    [...lancamentosKeys.all, "contasParaExibicaoTransferencia"] as const,
}
