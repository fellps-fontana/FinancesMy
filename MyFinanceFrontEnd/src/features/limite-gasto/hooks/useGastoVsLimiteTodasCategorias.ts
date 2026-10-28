import { useMemo } from "react"
import { usePlanoMensal } from "@/features/planejamento-mensal/hooks/usePlanoMensal"
import { sublimitesParaGastoVsLimite } from "@/features/limite-gasto/lib/sublimiteParaGastoVsLimite"

const hoje = new Date()

// Le do plano do mes (item 20) via usePlanoMensal; sem plano resolve `[]`
// (data continua `undefined` enquanto carrega). Default = mes corrente.
export function useGastoVsLimiteTodasCategorias(
  ano: number = hoje.getFullYear(),
  mes: number = hoje.getMonth() + 1,
) {
  const { data: plano, ...consulta } = usePlanoMensal(ano, mes)
  const data = useMemo(
    () => (plano === undefined ? undefined : sublimitesParaGastoVsLimite(plano)),
    [plano],
  )
  return { ...consulta, data }
}
