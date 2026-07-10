import { useQuery } from "@tanstack/react-query"
import { listarContasCartao } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import type { ContaResponse } from "@/features/cartao/types"

export type UseContaCartaoAtualResult = {
  contaCartaoAtual: ContaResponse | null
  isLoading: boolean
  isError: boolean
}

/**
 * Conta CARTAO "atual" da pagina - GET /api/contas?tipo=cartao
 * (ContasController.ListarContas -> IContaService.ListarContasPorTipo,
 * endpoint confirmado no backend atual). Pega a primeira conta da lista: o
 * dominio nao tem hoje um conceito de "cartao favorito/principal", so uma
 * tela que assume uma conta ativa por vez (regra de negocio item 12 nao
 * define multiplos cartoes simultaneos na v1).
 *
 * Lista vazia = ainda nao existe conta CARTAO cadastrada -> ContaCartaoPage
 * mostra o formulario de criacao. Apos criar uma conta (useCriarContaCartao),
 * a query e invalidada e esta lista e refeita automaticamente - sem precisar
 * de estado local proprio.
 *
 * Substitui o hack de localStorage usado antes desse endpoint existir no
 * backend (GET /api/contas so aceitava tipo=investimento). O gap ficou
 * resolvido no commit que adicionou IContaService.ListarContasPorTipo.
 */
export function useContaCartaoAtual(): UseContaCartaoAtualResult {
  const { data, isLoading, isError } = useQuery({
    queryKey: cartaoKeys.contasCartao(),
    queryFn: listarContasCartao,
  })

  return {
    contaCartaoAtual: data?.[0] ?? null,
    isLoading,
    isError,
  }
}
