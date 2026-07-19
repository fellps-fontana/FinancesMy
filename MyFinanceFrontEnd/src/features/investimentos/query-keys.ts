// Chaves centralizadas para evitar string magica espalhada nos hooks e
// garantir que invalidacao de cache (apos mutation) e leitura (useQuery)
// sempre apontem para a mesma chave. Ativo e conta de investimento simples
// sao modulos independentes (regra-de-negocio.md item 8), mas dividem o
// mesmo namespace "investimentos" por conveniencia de cache - nao ha
// invalidacao cruzada entre os dois grupos de chave.
export const investimentosKeys = {
  all: ["investimentos"] as const,
  ativos: () => [...investimentosKeys.all, "ativos"] as const,
  resumoAtivos: () => [...investimentosKeys.all, "resumoAtivos"] as const,
  contas: () => [...investimentosKeys.all, "contas"] as const,
  total: () => [...investimentosKeys.all, "total"] as const,
}
