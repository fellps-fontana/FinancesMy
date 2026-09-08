// Mesmo padrao de contasFixasKeys (features/contas-fixas/query-keys.ts):
// chave base "lista" sem argumento cobre qualquer variacao filtrada
// (contaId/ativa), ja que React Query casa por prefixo.
export const assinaturasCartaoKeys = {
  all: ["assinaturasCartao"] as const,
  lista: (contaId?: string, ativa?: boolean) => {
    const partes: unknown[] = [...assinaturasCartaoKeys.all, "lista"]
    if (contaId !== undefined) partes.push(contaId)
    if (ativa !== undefined) partes.push(ativa)
    return partes as readonly unknown[]
  },
  porId: (id: string) => [...assinaturasCartaoKeys.all, "porId", id] as const,
}
