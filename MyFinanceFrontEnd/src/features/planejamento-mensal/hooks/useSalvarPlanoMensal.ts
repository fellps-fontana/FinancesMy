import { useMutation, useQueryClient } from "@tanstack/react-query"
import { salvarPlanoMensal } from "@/features/planejamento-mensal/api"
import { planejamentoMensalKeys } from "@/features/planejamento-mensal/query-keys"
import type { SalvarPlanoMensalRequest } from "@/features/planejamento-mensal/types"

export function useSalvarPlanoMensal(ano: number, mes: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: SalvarPlanoMensalRequest) => salvarPlanoMensal(ano, mes, request),
    onSuccess: (planoSalvo) => {
      // Grava a resposta do PUT antes de a tela sair do modo edicao (sem flash do plano antigo);
      // a invalidacao confirma com o back e o mutate so conclui quando ela termina.
      const chave = planejamentoMensalKeys.plano(ano, mes)
      queryClient.setQueryData(chave, planoSalvo)
      return queryClient.invalidateQueries({ queryKey: chave })
    },
  })
}
