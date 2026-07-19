const formatadorPercentual = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
})

// percentualDaCarteira (ResumoPorTipo) e valorInvestido/valorAtual em geral ja
// chegam do backend multiplicados por 100 (ex: 64.3 = 64,3% - ver
// Services/AtivoService.cs ObterResumo). Intl.NumberFormat com style
// "percent" espera fracao (0.643 -> "64,3%"), por isso dividimos por 100
// aqui antes de formatar - unico lugar do front que sabe dessa escala. Sem
// sinal - uso composicional ("X% da carteira"), nunca negativo. Para a
// evolucao de um ativo (que PRECISA de sinal +/-), ver
// formatarVariacaoPercentual.
export function formatarPercentual(valor: number): string {
  return formatadorPercentual.format(valor / 100)
}
