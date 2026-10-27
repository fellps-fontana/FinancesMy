import { useMutation, useQueryClient } from "@tanstack/react-query"
import { arquivarCategoria } from "@/features/categorias/api"
import { categoriasKeys } from "@/features/categorias/query-keys"
import { planejamentoMensalKeys } from "@/features/planejamento-mensal/query-keys"

// Arquivar e soft-delete (arquivada = true, regra-de-negocio.md item 7) -
// invalida a lista de categorias e o planejamento mensal (que exibe o selo de
// categoria arquivada nos sublimites).
export function useArquivarCategoria() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => arquivarCategoria(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriasKeys.lista() })
      queryClient.invalidateQueries({ queryKey: planejamentoMensalKeys.all })
    },
  })
}
