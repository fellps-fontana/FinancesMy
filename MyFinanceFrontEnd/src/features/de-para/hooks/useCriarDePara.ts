import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarDePara } from "@/features/de-para/api"
import { deParaKeys } from "@/features/de-para/query-keys"
import type { CriarDeParaCategoriaRequest } from "@/features/de-para/types"

export function useCriarDePara() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarDeParaCategoriaRequest) => criarDePara(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: deParaKeys.lista() })
    },
  })
}
