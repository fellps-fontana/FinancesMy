import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarContaFixa } from "@/features/contas-fixas/api"
import { contasFixasKeys } from "@/features/contas-fixas/query-keys"

// Desativar (ativa true->false) remove os Lancamentos futuros ainda nao
// pagos vinculados a ContaFixa (regra-de-negocio.md item 6) - invalida a
// lista e o registro pontual.
export function useDesativarContaFixa() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => desativarContaFixa(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.lista() })
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.porId(id) })
    },
  })
}
