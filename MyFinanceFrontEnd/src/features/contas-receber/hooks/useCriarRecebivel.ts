import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarRecebivel } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"
import type { CriarRecebivelRequest } from "@/features/contas-receber/types"

// Um novo recebivel pode entrar na projecao do mes corrente se a
// data_prevista cair nele (regra-de-negocio.md item 9) - por isso invalida
// tanto a lista quanto o total esperado do mes, mesma logica aplicada a
// useCriarContaInvestimento em investimentos/.
export function useCriarRecebivel() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarRecebivelRequest) => criarRecebivel(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
