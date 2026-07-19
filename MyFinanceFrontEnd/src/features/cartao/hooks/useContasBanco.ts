import { useQuery } from "@tanstack/react-query"
import { listarContasBanco } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"

// Contas BANCO disponiveis como origem do pagamento de fatura (regra de
// negocio item 3: transferencia de mesma titularidade conta corrente ->
// cartao). GET /api/contas?tipo=banco.
export function useContasBanco() {
  return useQuery({
    queryKey: cartaoKeys.contasBanco(),
    queryFn: listarContasBanco,
  })
}
