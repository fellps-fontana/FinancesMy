import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/api"
import { recebiveisRecorrentesKeys } from "@/features/recebiveis-recorrentes/query-keys"
// Import cross-feature apenas das CHAVES (nao de hook/componente) de
// contas-receber: criar um molde MATERIALIZA ocorrencias de Conta a Receber
// (item 15), entao a lista de contas a receber e o total esperado do mes
// (item 9) mudam junto. As chaves sao contrato estavel da feature vizinha -
// mesma direcao de dependencia que contas-fixas ja tem com contas-receber.
import { contasReceberKeys } from "@/features/contas-receber/query-keys"
import type { CriarRecebivelRecorrenteRequest } from "@/features/recebiveis-recorrentes/types"

export function useCriarRecebivelRecorrente() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarRecebivelRecorrenteRequest) => criarRecebivelRecorrente(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.lista() })
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
