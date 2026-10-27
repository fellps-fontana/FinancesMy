export type MesPlano = { ano: number; mes: number }

const MESES_POR_ANO = 12

// Regra item 20: plano so pode ser consultado, gerado ou salvo ate 12 meses a
// frente do mes corrente (inclusive).
export const HORIZONTE_MESES_FUTUROS = 12

export const MESES_DO_ANO: ReadonlyArray<{ valor: number; rotulo: string }> = [
  { valor: 1, rotulo: "Janeiro" },
  { valor: 2, rotulo: "Fevereiro" },
  { valor: 3, rotulo: "Março" },
  { valor: 4, rotulo: "Abril" },
  { valor: 5, rotulo: "Maio" },
  { valor: 6, rotulo: "Junho" },
  { valor: 7, rotulo: "Julho" },
  { valor: 8, rotulo: "Agosto" },
  { valor: 9, rotulo: "Setembro" },
  { valor: 10, rotulo: "Outubro" },
  { valor: 11, rotulo: "Novembro" },
  { valor: 12, rotulo: "Dezembro" },
]

export function mesCorrente(): MesPlano {
  const hoje = new Date()
  return { ano: hoje.getFullYear(), mes: hoje.getMonth() + 1 }
}

export function indiceMes({ ano, mes }: MesPlano): number {
  return ano * MESES_POR_ANO + (mes - 1)
}

export function deslocarMes(periodo: MesPlano, delta: number): MesPlano {
  const indice = indiceMes(periodo) + delta
  return { ano: Math.floor(indice / MESES_POR_ANO), mes: (indice % MESES_POR_ANO) + 1 }
}

export function ehMesmoMes(a: MesPlano, b: MesPlano): boolean {
  return a.ano === b.ano && a.mes === b.mes
}

export function estaDentroDoHorizonte(
  alvo: MesPlano,
  referencia: MesPlano = mesCorrente(),
): boolean {
  return indiceMes(alvo) - indiceMes(referencia) <= HORIZONTE_MESES_FUTUROS
}

export function podeAvancarMes(atual: MesPlano, referencia: MesPlano = mesCorrente()): boolean {
  return indiceMes(atual) - indiceMes(referencia) < HORIZONTE_MESES_FUTUROS
}

export function rotuloMes({ ano, mes }: MesPlano): string {
  const texto = new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" }).format(
    new Date(ano, mes - 1, 1),
  )
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}
