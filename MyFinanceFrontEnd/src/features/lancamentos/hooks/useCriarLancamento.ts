import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarLancamento } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { CriarLancamentoRequest } from "@/features/lancamentos/types"

type CriarLancamentoVariables = {
  contaId: string
  request: CriarLancamentoRequest
}

// Criar um lancamento muda o fluxo de caixa - invalida por prefixo
// (lancamentosKeys.all) em vez de so a chave por conta: cobre num so lugar a
// visao agregada (fluxoCaixaTodasContas, consumida por LancamentosPage.tsx) e
// a chave legada por conta (useFluxoCaixa, ainda usada pelo dashboard).
export function useCriarLancamento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, request }: CriarLancamentoVariables) =>
      criarLancamento(contaId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
    },
  })
}
