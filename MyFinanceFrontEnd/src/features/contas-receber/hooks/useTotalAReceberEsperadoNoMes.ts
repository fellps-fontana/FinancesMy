import { useQuery } from "@tanstack/react-query"
import { buscarTotalAReceberEsperadoNoMes } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"

export function useTotalAReceberEsperadoNoMes(ano: number, mes: number) {
  return useQuery({
    queryKey: contasReceberKeys.totalEsperadoMes(ano, mes),
    queryFn: () => buscarTotalAReceberEsperadoNoMes(ano, mes),
  })
}
