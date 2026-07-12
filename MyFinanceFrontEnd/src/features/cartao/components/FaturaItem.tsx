import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { StatusFaturaBadge } from "@/features/cartao/components/StatusFaturaBadge"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import type { FaturaResponse } from "@/features/cartao/types"

type FaturaItemProps = {
  fatura: FaturaResponse
  onPagar: () => void
}

// Componente de apresentacao: mostra o agregado da fatura ja calculado pelo
// backend (FaturaSaldoCalculator, regra de negocio item 12). Sem lista de
// compras individuais - GAP CONHECIDO: nao ha endpoint para listar os
// lancamentos de uma fatura (CartaoComprasController so tem POST/PUT).
export function FaturaItem({ fatura, onPagar }: FaturaItemProps) {
  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-2">
        <div className="flex items-center justify-between">
          <span className="text-[13px] text-text-muted">
            {formatarData(fatura.dataFechamento)} - {formatarData(fatura.dataVencimento)}
          </span>
          <StatusFaturaBadge status={fatura.status} />
        </div>

        <div className="flex items-baseline justify-between">
          <span className="text-[19px] font-medium text-text-primary">
            {formatarMoeda(fatura.valorTotal)}
          </span>
          <span className="text-[12px] text-text-faint">
            Vence em {formatarData(fatura.dataVencimento)}
          </span>
        </div>

        <div className="flex items-center justify-between text-[13px] text-text-muted">
          <span>
            Pago <span className="font-medium text-positivo">{formatarMoeda(fatura.valorPago)}</span>
          </span>
          <span>
            Pendente{" "}
            <span className="font-medium text-alerta">{formatarMoeda(fatura.valorPendente)}</span>
          </span>
        </div>

        {fatura.valorPendente > 0 && (
          <div className="flex justify-end">
            <Button type="button" size="sm" variant="outline" onClick={onPagar}>
              Pagar
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
