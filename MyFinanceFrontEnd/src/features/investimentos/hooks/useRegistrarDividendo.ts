import { useMutation, useQueryClient } from "@tanstack/react-query"
import { registrarDividendo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { RegistrarDividendoRequest } from "@/features/investimentos/types"

type RegistrarDividendoVariables = {
  ativoId: string
  request: RegistrarDividendoRequest
}

export function useRegistrarDividendo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ ativoId, request }: RegistrarDividendoVariables) =>
      registrarDividendo(ativoId, request),
    onSuccess: (_data, { ativoId }) => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.rendimentos(ativoId) })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.rendimentosResumo() })
    },
  })
}
