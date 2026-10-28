import { useMemo } from "react"
import { usePlanoMensal } from "@/features/planejamento-mensal/hooks/usePlanoMensal"
import { sublimitesParaGastoVsLimite } from "@/features/limite-gasto/lib/sublimiteParaGastoVsLimite"

const hoje = new Date()

// Sublimite de UMA categoria no plano do mes; `null` se a categoria nao tem
// sublimite (ou o mes nao tem plano); `undefined` enquanto carrega.
// Default = mes corrente.
export function useGastoVsLimite(
  categoriaId: string,
  ano: number = hoje.getFullYear(),
  mes: number = hoje.getMonth() + 1,
) {
  const { data: plano, ...consulta } = usePlanoMensal(ano, mes)
  const data = useMemo(
    () =>
      plano === undefined
        ? undefined
        : (sublimitesParaGastoVsLimite(plano).find((item) => item.categoriaId === categoriaId) ??
          null),
    [plano, categoriaId],
  )
  return { ...consulta, data }
}
