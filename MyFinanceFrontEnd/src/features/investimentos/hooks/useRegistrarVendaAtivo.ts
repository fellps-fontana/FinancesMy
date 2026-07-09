import { useMutation, useQueryClient } from "@tanstack/react-query"
import { registrarVendaAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { RegistrarVendaRequest } from "@/features/investimentos/types"

type RegistrarVendaVariables = {
  contaId: string
  ativoId: string
  request: RegistrarVendaRequest
}

// Venda reduz quantidade (ou desativa o ativo ao zerar, item 8.3) e muda o
// saldo calculado da conta INVESTIMENTO (item 10) - mesma invalidacao da
// compra.
export function useRegistrarVendaAtivo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, ativoId, request }: RegistrarVendaVariables) =>
      registrarVendaAtivo(contaId, ativoId, request),
    onSuccess: (_data, { contaId }) => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos(contaId) })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.contas() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
