// Chaves centralizadas para evitar string magica espalhada nos hooks e
// garantir que invalidacao de cache (apos mutation) e leitura (useQuery)
// sempre apontem para a mesma chave. `lista(status)` sem argumento retorna
// a chave base "lista" - invalidar essa chave base invalida qualquer query
// de lista, filtrada ou nao, ja que o React Query casa por prefixo.
export const contasReceberKeys = {
  all: ["contasReceber"] as const,
  lista: (status?: string) =>
    status
      ? ([...contasReceberKeys.all, "lista", status] as const)
      : ([...contasReceberKeys.all, "lista"] as const),
  porId: (id: string) => [...contasReceberKeys.all, "porId", id] as const,
  totalEsperadoMes: (ano: number, mes: number) =>
    [...contasReceberKeys.all, "totalEsperadoMes", ano, mes] as const,
  // Contas MANUAL (banco + investimento) disponiveis pra selecao de origem
  // (emprestimo) ou destino (recebimento) - mesmo conjunto de dado nos dois
  // formularios, so muda o rotulo do campo. Sem filtro/parametro: uma unica
  // chave cobre os dois usos.
  contasParaSelecao: () => [...contasReceberKeys.all, "contasParaSelecao"] as const,
}
