import { useQuery } from "@tanstack/react-query"
import { listarContasBanco } from "@/features/cartao/api"
import { dashboardKeys } from "@/features/dashboard/query-keys"
import { listarFluxoCaixa } from "@/features/lancamentos/api"
import type { LancamentoResponse } from "@/features/lancamentos/types"

const QUANTIDADE_PADRAO = 5

// "Ultimos lancamentos" (mockup 02, secao "Ultimos lancamentos") e uma
// PREVIA do fluxo de caixa (regra-de-negocio.md item 12: "Lancamento geral
// / fluxo de caixa (CAIXA)") somando TODAS as contas BANCO do usuario - nao
// de uma unica conta. Por isso este hook nao chama useFluxoCaixa
// (lancamentos/hooks, exige um contaId) e sim compoe, no client:
//   1. listarContasBanco (cartao/api.ts, ja usada hoje para achar a conta de
//      origem do pagamento de fatura) - lista as contas tipo BANCO ativas.
//   2. listarFluxoCaixa (lancamentos/api.ts) para cada conta.
//   3. Junta tudo, ordena por data desc e corta em `quantidade`.
//
// GAP CONHECIDO (mesmo espirito do comentario em cartao/api.ts sobre
// obterRelatorioCategoria): nao existe, hoje, um endpoint unico de
// "ultimos lancamentos entre contas" - LancamentosController so expoe
// fluxo-caixa POR conta (confirmado por busca em Controllers/). Agregar no
// client e o que da pra fazer sem tocar backend fora do escopo desta task;
// registrado aqui como candidato a um endpoint dedicado
// (GET /api/dashboard/ultimos-lancamentos) se o numero de contas banco do
// usuario crescer o bastante pra 1 chamada HTTP por conta pesar.
//
// Contas CARTAO e INVESTIMENTO ficam de fora de proposito: investimento nao
// tem lancamento, so `saldo_manual` (item 10); a compra de cartao e
// COMPETENCIA e nunca aparece no fluxo de caixa (item 12) - so o pagamento
// da fatura aparece, e essa perna ja e um lancamento na propria conta BANCO
// que pagou, entao somar a conta CARTAO tambem duplicaria a linha.
export function useUltimosLancamentos(quantidade: number = QUANTIDADE_PADRAO) {
  return useQuery({
    queryKey: dashboardKeys.ultimosLancamentos(quantidade),
    queryFn: async (): Promise<LancamentoResponse[]> => {
      const contasBanco = await listarContasBanco()
      const fluxosPorConta = await Promise.all(
        contasBanco.map((conta) => listarFluxoCaixa(conta.id)),
      )

      return fluxosPorConta
        .flat()
        .sort((a, b) => new Date(b.data).getTime() - new Date(a.data).getTime())
        .slice(0, quantidade)
    },
  })
}
