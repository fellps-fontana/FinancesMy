import { useQuery } from "@tanstack/react-query"
import { obterSaldoCartao } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"

export function useSaldoCartao(contaId: string | null) {
  return useQuery({
    queryKey: cartaoKeys.saldo(contaId ?? ""),
    queryFn: () => obterSaldoCartao(contaId as string),
    enabled: contaId !== null,
  })
}
