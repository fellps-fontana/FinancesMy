import { skipToken, useQuery } from "@tanstack/react-query"
import { listarComprasDaFatura } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"

// Compras (lancamentos) da fatura selecionada; troca de aba = nova chave.
export function useComprasDaFatura(contaId: string | null, faturaId: string | null) {
  return useQuery({
    queryKey: cartaoKeys.comprasDaFatura(contaId ?? "", faturaId ?? ""),
    queryFn:
      contaId !== null && faturaId !== null
        ? () => listarComprasDaFatura(contaId, faturaId)
        : skipToken,
  })
}
