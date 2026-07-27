// Mesmo padrao de contas-fixas/query-keys.ts: chave base "lista" cobre
// qualquer variacao filtrada (tipo, arquivada) via prefixo do React Query —
// uma mutation (criar/editar/arquivar/reativar) so precisa invalidar
// lista() para atingir toda query de lista, independente do filtro usado.
export const categoriasKeys = {
  all: ["categorias"] as const,
  lista: (tipo?: string, arquivada?: boolean) => {
    const filtros: unknown[] = []
    if (tipo !== undefined) filtros.push(tipo)
    if (arquivada !== undefined) filtros.push(arquivada)

    return filtros.length > 0
      ? ([...categoriasKeys.all, "lista", ...filtros] as const)
      : ([...categoriasKeys.all, "lista"] as const)
  },
}
