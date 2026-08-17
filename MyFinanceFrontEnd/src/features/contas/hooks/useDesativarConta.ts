import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarConta } from "@/features/contas/api"
import { contasKeys } from "@/features/contas/query-keys"

// Mutation de desativacao de conta (soft-delete, regra-de-negocio.md item
// 8.3, mesmo principio aplicado a Conta). Invalida a lista desta feature -
// a conta desativada some da tela assim que o cache e revalidado.
export function useDesativarConta() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => desativarConta(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasKeys.lista() })
    },
  })
}
