import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { formatadorPercentual } from "@/features/planejamento-mensal/lib/percentual"
import type { PlanoPreview, Rascunho } from "@/features/planejamento-mensal/lib/rascunho"

type CartaoResumoLeituraProps = {
  rotulo: string
  valor: string
  detalhe?: string
}

function CartaoResumoLeitura({ rotulo, valor, detalhe }: CartaoResumoLeituraProps) {
  return (
    <div className="flex flex-col justify-between gap-1 rounded-[12px] border-[0.5px] border-border bg-card p-3.5">
      <div className="flex flex-col gap-0.5">
        <span className="text-[13px] text-text-muted">{rotulo}</span>
        <span className="text-[19px] font-medium text-text-primary">{valor}</span>
      </div>
      {detalhe && <span className="text-[12px] text-text-faint">{detalhe}</span>}
    </div>
  )
}

type ResumoPlanoProps = {
  plano: PlanoPreview
  rascunho: Rascunho | null
  onAlterarRenda?: (valor: string) => void
  onAlterarPercentualInvestimento?: (valor: string) => void
  onAlterarValorInvestimento?: (valor: string) => void
}

// Apresentacao do resumo: em meses editaveis, renda prevista (R$) e
// investimento (% e R$ sincronizados bidirecionalmente) sao editados inline.
export function ResumoPlano({
  plano,
  rascunho,
  onAlterarRenda,
  onAlterarPercentualInvestimento,
  onAlterarValorInvestimento,
}: ResumoPlanoProps) {
  const editavel = !plano.somenteLeitura && rascunho !== null
  const percentualFormatado = `${formatadorPercentual.format(plano.percentualInvestimento)}% da renda`
  const detalheInvestimento = plano.investimentoReduzido
    ? `Efetivo ${formatarMoeda(plano.investimento)} · planejado ${formatarMoeda(plano.investimentoPlanejado)}`
    : `${percentualFormatado} · efetivo ${formatarMoeda(plano.investimento)}`

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {editavel ? (
        <div className="flex flex-col justify-between gap-2 rounded-[12px] border-[0.5px] border-border bg-card p-3.5">
          <div className="flex items-center justify-between gap-2">
            <Label htmlFor="renda-prevista" className="text-[13px] font-normal text-text-muted">
              Renda prevista
            </Label>
            <span className="text-[12px] text-text-faint">
              {formatarMoeda(plano.rendaPrevista)}
            </span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="text-[13px] text-text-muted">R$</span>
            <Input
              id="renda-prevista"
              inputMode="decimal"
              value={rascunho.rendaPrevista}
              onChange={(e) => onAlterarRenda?.(e.target.value)}
              aria-label="Renda prevista (R$)"
              className="h-8 text-sm font-medium"
            />
          </div>
        </div>
      ) : (
        <CartaoResumoLeitura rotulo="Renda prevista" valor={formatarMoeda(plano.rendaPrevista)} />
      )}

      <CartaoResumoLeitura
        rotulo="Contas fixas"
        valor={formatarMoeda(plano.fixas)}
        detalhe="Contas fixas e assinaturas do mês"
      />

      {editavel ? (
        <div className="flex flex-col justify-between gap-2 rounded-[12px] border-[0.5px] border-border bg-card p-3.5">
          <div className="flex items-center justify-between gap-2">
            <span className="text-[13px] text-text-muted">Investimento</span>
            <span className="text-[12px] font-medium text-text-primary">
              {formatarMoeda(plano.investimento)}
            </span>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div className="flex items-center gap-1">
              <Input
                id="percentual-investimento"
                inputMode="decimal"
                value={rascunho.percentualInvestimento}
                onChange={(e) => onAlterarPercentualInvestimento?.(e.target.value)}
                aria-label="Investimento (% da renda)"
                className="h-8 text-right text-sm"
              />
              <span className="text-[12px] text-text-muted">%</span>
            </div>
            <div className="flex items-center gap-1">
              <span className="text-[12px] text-text-muted">R$</span>
              <Input
                id="valor-investimento"
                inputMode="decimal"
                value={rascunho.valorInvestimento}
                onChange={(e) => onAlterarValorInvestimento?.(e.target.value)}
                aria-label="Investimento planejado (R$)"
                className="h-8 text-right text-sm"
              />
            </div>
          </div>
          <span className="text-[12px] text-text-faint">{detalheInvestimento}</span>
        </div>
      ) : (
        <CartaoResumoLeitura
          rotulo="Investimento"
          valor={formatarMoeda(plano.investimento)}
          detalhe={detalheInvestimento}
        />
      )}

      <CartaoResumoLeitura
        rotulo="Sobra para sublimites"
        valor={formatarMoeda(plano.sobra)}
        detalhe="Renda menos fixas e investimento"
      />
    </div>
  )
}
