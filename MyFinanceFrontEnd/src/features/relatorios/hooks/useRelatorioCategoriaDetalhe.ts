import { useQuery } from "@tanstack/react-query"
import { buscarRelatorioCategoriaDetalhe } from "@/features/relatorios/api"
import { relatoriosKeys } from "@/features/relatorios/query-keys"

// categoriaId null = "Sem categoria".
export function useRelatorioCategoriaDetalhe(
  ano: number,
  mes: number,
  categoriaId: string | null = null,
) {
  return useQuery({
    queryKey: relatoriosKeys.categoriaDetalhe(ano, mes, categoriaId),
    queryFn: () => buscarRelatorioCategoriaDetalhe(ano, mes, categoriaId),
  })
}
