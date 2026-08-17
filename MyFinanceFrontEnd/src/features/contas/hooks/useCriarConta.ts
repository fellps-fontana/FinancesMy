import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarConta } from "@/features/contas/api"
import { contasKeys } from "@/features/contas/query-keys"
import type { CriarContaRequest } from "@/features/contas/types"

// Estado de servidor da mutation "Nova conta" (clean-code.md "Organizacao
// (React)": mutation fica em hooks/, nao espalhada no componente). Invalida
// a lista combinada banco+investimento (useContas) para a conta nova
// aparecer sem precisar de reload manual.
export function useCriarConta() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarContaRequest) => criarConta(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasKeys.lista() })
    },
  })
}
