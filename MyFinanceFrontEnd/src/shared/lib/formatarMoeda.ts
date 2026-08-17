const formatadorReal = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
})

// Formatacao de moeda pt-BR - funcao pura de proposito geral, sem regra de
// negocio de nenhum modulo (stack.md "Estrutura de pastas (src/)" - criterio
// shared/lib). Fonte unica consumida por 2+ features (contas, investimentos,
// cartao, contas-receber, contas-fixas, categorias, dashboard, limite-gasto)
// - antes duplicada/reexportada de dentro de features/investimentos, o que
// violava "nenhum import de uma feature apontando pra dentro de outra"
// (correcao apontada pelo style no Bloco G/Contas).
export function formatarMoeda(valor: number): string {
  return formatadorReal.format(valor)
}
