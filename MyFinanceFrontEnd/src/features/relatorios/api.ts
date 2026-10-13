import { apiClient } from "@/shared/api/client"
import type {
  RelatorioCategoriaDetalheResponse,
  RelatorioGastoPorCategoriaResponse,
} from "@/features/relatorios/types"

export function buscarRelatorioGastoPorCategoria(
  ano: number,
  mes: number,
): Promise<RelatorioGastoPorCategoriaResponse> {
  return apiClient.get<RelatorioGastoPorCategoriaResponse>(
    `/api/relatorios/gasto-por-categoria?ano=${ano}&mes=${mes}`,
  )
}

// categoriaId null/omitido => backend devolve o detalhe de "Sem categoria".
export function buscarRelatorioCategoriaDetalhe(
  ano: number,
  mes: number,
  categoriaId: string | null,
): Promise<RelatorioCategoriaDetalheResponse> {
  const categoria = categoriaId ? `&categoriaId=${categoriaId}` : ""
  return apiClient.get<RelatorioCategoriaDetalheResponse>(
    `/api/relatorios/gasto-por-categoria/detalhe?ano=${ano}&mes=${mes}${categoria}`,
  )
}
