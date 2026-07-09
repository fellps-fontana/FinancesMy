// Chaves centralizadas para evitar string magica espalhada nos hooks e
// garantir que invalidacao de cache (apos mutation) e leitura (useQuery)
// sempre apontem para a mesma chave - mesmo padrao de investimentos/query-keys.ts.
export const cartaoKeys = {
  all: ["cartao"] as const,
  saldo: (contaId: string) => [...cartaoKeys.all, "saldo", contaId] as const,
  faturas: (contaId: string) => [...cartaoKeys.all, "faturas", contaId] as const,
  relatorioCategoria: (mes: string, contaId?: string) =>
    [...cartaoKeys.all, "relatorio-categoria", mes, contaId ?? null] as const,
}
