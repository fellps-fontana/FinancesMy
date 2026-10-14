import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarPercentual } from "@/features/investimentos/lib/formatarPercentual"
import { obterResumoPorTipo } from "@/features/investimentos/lib/obterResumoPorTipo"
import type { AtivosResumoResponse } from "@/features/investimentos/types"

type ResumoAtivosCardsProps = {
  /** GET /api/contas/investimentos/total; undefined enquanto carrega. */
  totalInvestido: number | undefined
  resumo: AtivosResumoResponse | undefined
  carregando: boolean
}

const CLASSE_CARD = "rounded-2xl border-[0.5px] border-border bg-card p-5 md:p-[22px]"

// Apresentacao pura (mockup s_investimentos): Total investido (consolidado
// pelo backend) e, so a partir de md, os tiles Renda fixa / Renda variavel
// com o % da carteira ja calculado pelo backend (obterResumoPorTipo so
// localiza o item). Sem "% no mes": nao ha historico de valor na v1.
export function ResumoAtivosCards({ totalInvestido, resumo, carregando }: ResumoAtivosCardsProps) {
  const rendaFixa = obterResumoPorTipo(resumo, "RENDA_FIXA")
  const rendaVariavel = obterResumoPorTipo(resumo, "RENDA_VARIAVEL")

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
      <div className={CLASSE_CARD}>
        <span className="text-[13px] text-text-muted">Total investido</span>
        <div className="mt-2 text-[28px] font-medium text-text-primary">
          {totalInvestido === undefined ? "Carregando..." : formatarMoeda(totalInvestido)}
        </div>
      </div>

      <div className={`${CLASSE_CARD} hidden md:block`}>
        <span className="text-[13px] text-text-muted">Renda fixa</span>
        <div className="mt-2 text-[19px] font-medium text-text-primary">
          {carregando ? "Carregando..." : formatarMoeda(rendaFixa.valorAtual)}
        </div>
        {!carregando && (
          <div className="mt-1 text-xs text-text-faint">
            {formatarPercentual(rendaFixa.percentualDaCarteira)} da carteira
          </div>
        )}
      </div>

      <div className={`${CLASSE_CARD} hidden md:block`}>
        <span className="text-[13px] text-text-muted">Renda variavel</span>
        <div className="mt-2 text-[19px] font-medium text-text-primary">
          {carregando ? "Carregando..." : formatarMoeda(rendaVariavel.valorAtual)}
        </div>
        {!carregando && (
          <div className="mt-1 text-xs text-text-faint">
            {formatarPercentual(rendaVariavel.percentualDaCarteira)} da carteira
          </div>
        )}
      </div>
    </div>
  )
}
