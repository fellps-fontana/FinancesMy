import { skipToken, useQuery } from "@tanstack/react-query"
import { listarFaturas } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"

// O backend NAO ordena o resultado (FaturaRepository.ListarPorConta so
// filtra por contaId, sem OrderBy) - reordenamos aqui no front por
// dataVencimento desc (fatura mais recente primeiro), a ordem que a tela
// precisa.
export function useFaturas(contaId: string | null) {
  return useQuery({
    queryKey: cartaoKeys.faturas(contaId ?? ""),
    queryFn:
      contaId === null
        ? skipToken
        : async () => {
            const faturas = await listarFaturas(contaId)
            return [...faturas].sort((a, b) => b.dataVencimento.localeCompare(a.dataVencimento))
          },
  })
}
