// Chaves centralizadas do React Query (mesmo padrao de contas-fixas e
// contas-receber): `lista` sem argumento retorna a chave base, e invalidar
// essa base atinge qualquer variacao filtrada (ativa/inativa/sem filtro),
// ja que o React Query casa por prefixo.
export const recebiveisRecorrentesKeys = {
  all: ["recebiveisRecorrentes"] as const,
  lista: (ativa?: boolean) =>
    ativa !== undefined
      ? ([...recebiveisRecorrentesKeys.all, "lista", ativa] as const)
      : ([...recebiveisRecorrentesKeys.all, "lista"] as const),
  porId: (id: string) => [...recebiveisRecorrentesKeys.all, "porId", id] as const,
}
