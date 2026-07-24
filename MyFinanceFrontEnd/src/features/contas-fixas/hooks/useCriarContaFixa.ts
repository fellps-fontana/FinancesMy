import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarContaFixa } from "@/features/contas-fixas/api"
import { contasFixasKeys } from "@/features/contas-fixas/query-keys"
import type { CriarContaFixaRequest } from "@/features/contas-fixas/types"

// Criar uma ContaFixa gera o Lancamento do mes vigente (regra-de-negocio.md
// item 6), o que muda o que a lista exibe - invalida so a lista, o registro
// ainda nao existe do lado do cliente.
export function useCriarContaFixa() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarContaFixaRequest) => criarContaFixa(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.lista() })
    },
  })
}
