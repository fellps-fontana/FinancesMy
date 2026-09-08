import { useQuery } from "@tanstack/react-query"
import { listarAssinaturasCartao } from "@/features/assinaturas-cartao/api"
import { assinaturasCartaoKeys } from "@/features/assinaturas-cartao/query-keys"

export function useAssinaturasCartao(contaId?: string, ativa?: boolean) {
  return useQuery({
    queryKey: assinaturasCartaoKeys.lista(contaId, ativa),
    queryFn: () => listarAssinaturasCartao(contaId, ativa),
    enabled: contaId !== undefined,
  })
}
