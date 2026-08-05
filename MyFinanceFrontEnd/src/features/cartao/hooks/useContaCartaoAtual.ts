import { useQuery } from "@tanstack/react-query"
import { listarContasCartao } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import type { ContaResponse } from "@/features/cartao/types"

export type UseContaCartaoAtualResult = {
  contasCartao: ContaResponse[]
  contaCartaoAtual: ContaResponse | null
  isLoading: boolean
  isError: boolean
}

/**
 * Contas CARTAO da pagina - GET /api/contas?tipo=cartao (ContasController.
 * ListarContas -> IContaService.ListarContasPorTipo, endpoint confirmado no
 * backend atual, sem restricao de quantidade). O backend ja suporta N contas
 * tipo=CARTAO (ContaService.CriarContaAsync/ValidarCartao nao limitam
 * quantidade); o gap de so exibir a primeira era 100% front - corrigido
 * expondo `contasCartao` (lista completa) junto de `contaCartaoAtual` (a
 * selecionada).
 *
 * `contaSelecionadaId` e estado de UI (qual cartao o usuario esta olhando),
 * mantido pelo container (ContaCartaoPage) - este hook so decide qual conta
 * da lista corresponde a esse id, com fallback pra primeira quando nada foi
 * selecionado ainda ou quando o id selecionado nao existe mais na lista
 * (ex: conta removida em outra aba).
 *
 * Lista vazia = ainda nao existe conta CARTAO cadastrada -> ContaCartaoPage
 * mostra o formulario de criacao. Apos criar uma conta (useCriarContaCartao),
 * a query e invalidada e esta lista e refeita automaticamente - sem precisar
 * de estado local proprio aqui.
 *
 * Substitui o hack de localStorage usado antes desse endpoint existir no
 * backend (GET /api/contas so aceitava tipo=investimento). O gap ficou
 * resolvido no commit que adicionou IContaService.ListarContasPorTipo.
 */
export function useContaCartaoAtual(contaSelecionadaId: string | null): UseContaCartaoAtualResult {
  const { data, isLoading, isError } = useQuery({
    queryKey: cartaoKeys.contasCartao(),
    queryFn: listarContasCartao,
  })

  const contasCartao = data ?? []
  const contaCartaoAtual =
    contasCartao.find((conta) => conta.id === contaSelecionadaId) ?? contasCartao[0] ?? null

  return {
    contasCartao,
    contaCartaoAtual,
    isLoading,
    isError,
  }
}
