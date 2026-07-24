import { useMutation, useQueryClient } from "@tanstack/react-query"
import { reativarContaFixa } from "@/features/contas-fixas/api"
import { contasFixasKeys } from "@/features/contas-fixas/query-keys"

// Reativar (ativa false->true) gera de novo os Lancamentos a partir do mes
// vigente (regra-de-negocio.md item 6) - mesma invalidacao de
// useDesativarContaFixa.
export function useReativarContaFixa() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => reativarContaFixa(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.lista() })
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.porId(id) })
    },
  })
}
