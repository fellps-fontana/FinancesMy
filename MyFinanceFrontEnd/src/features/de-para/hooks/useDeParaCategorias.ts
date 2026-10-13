import { useQuery } from "@tanstack/react-query"
import { listarDePara } from "@/features/de-para/api"
import { deParaKeys } from "@/features/de-para/query-keys"

export function useDeParaCategorias(categoriaPierre?: string) {
  return useQuery({
    queryKey: deParaKeys.lista(categoriaPierre),
    queryFn: () => listarDePara(categoriaPierre),
  })
}
