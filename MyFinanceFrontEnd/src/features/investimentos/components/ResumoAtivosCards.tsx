import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarPercentual } from "@/features/investimentos/lib/formatarPercentual"
import { obterResumoPorTipo } from "@/features/investimentos/lib/obterResumoPorTipo"
import type { AtivosResumoResponse } from "@/features/investimentos/types"

type ResumoAtivosCardsProps = {
  resumo: AtivosResumoResponse | undefined
  carregando: boolean
}

// Componente de apresentacao (burro): os 3 cards de resumo do mockup "11
// Investimentos" (Total investido, Renda fixa, Renda variavel). Nenhum
// calculo de dominio mora aqui - obterResumoPorTipo so localiza o item ja
// pronto dentro de resumo.porTipo (ver lib/obterResumoPorTipo.ts).
//
// O card "Total investido" usa resumo.totalAtual (nao totalInvestido) -
// decisao registrada no briefing desta tarefa, confirmada batendo a
// matematica do mockup: o valor exibido la soma o valor ATUAL dos ativos, nao
// o custo de aquisicao original. Sem sparkline/"% no mes" (regra-de-
// negocio.md item 8, "Pendencias a definir": nao ha historico de snapshots de
// valor_atual na v1).
export function ResumoAtivosCards({ resumo, carregando }: ResumoAtivosCardsProps) {
  const rendaFixa = obterResumoPorTipo(resumo, "RENDA_FIXA")
  const rendaVariavel = obterResumoPorTipo(resumo, "RENDA_VARIAVEL")

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <Card size="sm">
        <CardContent className="flex flex-col gap-1">
          <span className="text-[13px] text-text-muted">Total investido</span>
          <span className="text-[28px] font-medium text-text-primary">
            {carregando ? "Carregando..." : formatarMoeda(resumo?.totalAtual ?? 0)}
          </span>
        </CardContent>
      </Card>

      <Card size="sm">
        <CardContent className="flex flex-col gap-1">
          <span className="text-[13px] text-text-muted">Renda fixa</span>
          <span className="text-[19px] font-medium text-text-primary">
            {carregando ? "Carregando..." : formatarMoeda(rendaFixa.valorAtual)}
          </span>
          {!carregando && (
            <span className="text-[12px] text-text-faint">
              {formatarPercentual(rendaFixa.percentualDaCarteira)} da carteira
            </span>
          )}
        </CardContent>
      </Card>

      <Card size="sm">
        <CardContent className="flex flex-col gap-1">
          <span className="text-[13px] text-text-muted">Renda variavel</span>
          <span className="text-[19px] font-medium text-text-primary">
            {carregando ? "Carregando..." : formatarMoeda(rendaVariavel.valorAtual)}
          </span>
          {!carregando && (
            <span className="text-[12px] text-text-faint">
              {formatarPercentual(rendaVariavel.percentualDaCarteira)} da carteira
            </span>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
