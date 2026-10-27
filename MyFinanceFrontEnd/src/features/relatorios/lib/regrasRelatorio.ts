const formatadorPercentual = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })

// "12,5" (percentuais do backend ja vem na escala 0-100); o sufixo % fica na tela.
export function formatarPercentualRelatorio(percentual: number): string {
  return formatadorPercentual.format(percentual)
}
