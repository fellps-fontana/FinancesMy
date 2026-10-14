import type { AnoMes } from "@/features/relatorios/lib/navegarMes"

// Mes/ano vivem na query string (?ano=2026&mes=7); ausente/invalido = mes atual.
export function lerMesDaQuery(params: URLSearchParams): AnoMes {
  const hoje = new Date()
  const ano = Number(params.get("ano"))
  const mes = Number(params.get("mes"))
  const valido = Number.isInteger(ano) && ano > 1900 && Number.isInteger(mes) && mes >= 1 && mes <= 12
  return valido ? { ano, mes } : { ano: hoje.getFullYear(), mes: hoje.getMonth() + 1 }
}
