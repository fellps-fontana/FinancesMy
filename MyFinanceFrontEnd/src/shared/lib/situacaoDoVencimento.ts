export type SituacaoVencimento = "atrasado" | "pendente"

export type ResultadoSituacao = {
  situacao: SituacaoVencimento
  // Dias inteiros entre hoje e o vencimento (negativo = ja venceu).
  dias: number
  rotulo: string
}

const MS_POR_DIA = 24 * 60 * 60 * 1000

// `data` chega como DateOnly "yyyy-MM-dd"; parse manual em UTC evita o
// deslocamento de fuso de `new Date(iso)` (mesmo cuidado de filtrarPeriodo.ts).
function diasDesdeEpoch(ano: number, mes: number, dia: number): number {
  return Math.floor(Date.UTC(ano, mes - 1, dia) / MS_POR_DIA)
}

function plural(dias: number): string {
  return dias === 1 ? "1 dia" : `${dias} dias`
}

// Situacao de um vencimento ISO ("yyyy-MM-dd") frente a hoje: antes de hoje =
// atrasado; hoje ou depois = ainda pendente.
export function situacaoDoVencimento(vencimento: string, hoje: Date = new Date()): ResultadoSituacao {
  const [ano, mes, dia] = vencimento.split("-").map(Number)
  const dias = diasDesdeEpoch(ano, mes, dia) - diasDesdeEpoch(hoje.getFullYear(), hoje.getMonth() + 1, hoje.getDate())

  if (dias < 0) return { situacao: "atrasado", dias, rotulo: `Venceu ha ${plural(-dias)}` }
  if (dias === 0) return { situacao: "pendente", dias, rotulo: "Vence hoje" }
  return { situacao: "pendente", dias, rotulo: `Vence em ${plural(dias)}` }
}
