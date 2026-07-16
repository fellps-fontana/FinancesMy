const formatadorVariacao = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
  signDisplay: "exceptZero",
})

// Formata a evolucao percentual de um ativo (regra-de-negocio.md item 8.1:
// evolucao_percentual = (valor_atual - valor_investido) / valor_investido,
// ja calculada no back e multiplicada por 100) COM sinal explicito
// ("+0,9%"/"-1,8%") - o sinal e o que comunica ganho/perda de relance
// (identidade-visual.md: cor com significado, positivo/negativo).
export function formatarVariacaoPercentual(valor: number): string {
  return formatadorVariacao.format(valor / 100)
}
