import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { StatusFaturaBadge } from "@/features/cartao/components/StatusFaturaBadge"
import { formatarData } from "@/shared/lib/datas"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { FaturaResponse } from "@/features/cartao/types"

type FaturaResumoProps = {
  fatura: FaturaResponse
  onPagar: () => void
}

// Resumo da fatura selecionada (aba de mes): periodo, status e valores ja
// calculados pelo backend (FaturaSaldoCalculator, regra item 12). Pagar fica
// disponivel enquanto houver valor pendente (pagamento parcial permitido).
export function FaturaResumo({ fatura, onPagar }: FaturaResumoProps) {
  return (
    <div className="flex flex-wrap items-center gap-x-5 gap-y-2 rounded-2xl border-[0.5px] border-border bg-card px-5 py-3.5 text-[13px] text-text-muted">
      <StatusFaturaBadge status={fatura.status} />
      <span>
        Total <span className="font-medium text-text-primary">{formatarMoeda(fatura.valorTotal)}</span>
      </span>
      <span>
        Pago <span className="font-medium text-positivo">{formatarMoeda(fatura.valorPago)}</span>
      </span>
      <span>
        Pendente <span className="font-medium text-alerta">{formatarMoeda(fatura.valorPendente)}</span>
      </span>
      <span className="text-[12px] text-text-faint">Vence em {formatarData(fatura.dataVencimento)}</span>
      <div className="flex-1" />
      {fatura.valorPendente > 0 && (
        <Button type="button" size="sm" variant="outline" onClick={onPagar}>
          Pagar fatura
        </Button>
      )}
    </div>
  )
}

type FaturaAtualCardProps = {
  fatura: FaturaResponse | null
  carregando: boolean
}

// Resumo da fatura atual (coluna esquerda do mockup). "Limite usado" nao e
// exibido: o backend nao expoe limite do cartao (lacuna de backend).
export function FaturaAtualCard({ fatura, carregando }: FaturaAtualCardProps) {
  return (
    <Card>
      <CardContent className="flex flex-col gap-1">
        <div className="flex items-center justify-between">
          <span className="text-[13px] text-text-muted">Fatura atual</span>
          {fatura && <StatusFaturaBadge status={fatura.status} />}
        </div>
        {carregando && <span className="text-sm text-text-muted">Carregando...</span>}
        {!carregando && !fatura && (
          <span className="text-sm text-text-muted">Nenhuma fatura ainda.</span>
        )}
        {fatura && (
          <>
            <span className="mt-1 text-[28px] font-medium text-text-primary">
              {formatarMoeda(fatura.valorTotal)}
            </span>
            <span className="text-[12px] text-text-faint">
              Vence em {formatarData(fatura.dataVencimento)}
            </span>
          </>
        )}
      </CardContent>
    </Card>
  )
}
