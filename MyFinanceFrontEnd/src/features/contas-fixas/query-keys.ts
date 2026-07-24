// Mesmo padrao de contas-receber/query-keys.ts: chave base "lista" sem
// argumento cobre qualquer variacao filtrada, ja que o React Query casa por
// prefixo - uma mutation so precisa invalidar `lista()` para atingir toda
// query de lista (ativa, inativa ou sem filtro).
export const contasFixasKeys = {
  all: ["contasFixas"] as const,
  lista: (ativa?: boolean) =>
    ativa !== undefined
      ? ([...contasFixasKeys.all, "lista", ativa] as const)
      : ([...contasFixasKeys.all, "lista"] as const),
  porId: (id: string) => [...contasFixasKeys.all, "porId", id] as const,
}
