export type AnoMes = {
  ano: number
  mes: number // 1-12
}

function somarMeses({ ano, mes }: AnoMes, delta: number): AnoMes {
  const indice = ano * 12 + (mes - 1) + delta
  return { ano: Math.floor(indice / 12), mes: (indice % 12) + 1 }
}

export function mesAnterior(referencia: AnoMes): AnoMes {
  return somarMeses(referencia, -1)
}

export function proximoMes(referencia: AnoMes): AnoMes {
  return somarMeses(referencia, 1)
}

// Ex.: "Outubro 2026".
export function rotuloMes({ ano, mes }: AnoMes): string {
  const nome = new Date(ano, mes - 1, 1).toLocaleDateString("pt-BR", { month: "long" })
  return `${nome.charAt(0).toUpperCase()}${nome.slice(1)} ${ano}`
}
