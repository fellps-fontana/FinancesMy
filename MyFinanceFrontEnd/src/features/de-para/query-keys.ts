// Chave base "lista" cobre qualquer variacao filtrada por categoriaPierre via
// prefixo do React Query - mutations invalidam lista() e atingem todas.
export const deParaKeys = {
  all: ["de-para"] as const,
  lista: (categoriaPierre?: string) =>
    categoriaPierre !== undefined
      ? ([...deParaKeys.all, "lista", categoriaPierre] as const)
      : ([...deParaKeys.all, "lista"] as const),
}
