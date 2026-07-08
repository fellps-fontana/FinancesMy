// Chaves centralizadas para evitar string magica espalhada nos hooks e
// garantir que invalidacao de cache (apos mutation) e leitura (useQuery)
// sempre apontem para a mesma chave.
export const investimentosKeys = {
  all: ["investimentos"] as const,
  contas: () => [...investimentosKeys.all, "contas"] as const,
  total: () => [...investimentosKeys.all, "total"] as const,
}
