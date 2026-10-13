import { useQuery } from "@tanstack/react-query"
import { buscarRelatorioGastoPorCategoria } from "@/features/relatorios/api"
import { relatoriosKeys } from "@/features/relatorios/query-keys"

export function useRelatorioGastoPorCategoria(ano: number, mes: number) {
  return useQuery({
    queryKey: relatoriosKeys.gastoPorCategoria(ano, mes),
    queryFn: () => buscarRelatorioGastoPorCategoria(ano, mes),
  })
}
