import { useQuery } from "@tanstack/react-query"
import { buscarProjecaoMes } from "@/features/dashboard/api"
import { dashboardKeys } from "@/features/dashboard/query-keys"

export function useProjecaoMes(ano: number, mes: number) {
  return useQuery({
    queryKey: dashboardKeys.projecaoMes(ano, mes),
    queryFn: () => buscarProjecaoMes(ano, mes),
  })
}
