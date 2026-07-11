import { cn } from "@/shared/lib/utils"
import type { StatusFatura } from "@/features/cartao/types"

const CONFIG_POR_STATUS: Record<StatusFatura, { label: string; className: string }> = {
  ABERTA: { label: "Aberta", className: "bg-alerta/15 text-alerta" },
  FECHADA: { label: "Fechada", className: "bg-muted text-text-muted" },
  PAGA: { label: "Paga", className: "bg-positivo/15 text-positivo" },
}

// Badge de status da fatura (regra de negocio item 12; mapeamento de cor
// conforme identidade-visual.md - "Status (badges): pago -> positivo;
// pendente -> alerta; manual -> neutro"). ABERTA ainda acumula compras
// (equivalente a pendente/atencao); FECHADA tem valor definitivo mas ainda
// nao foi paga (neutro, aguardando); PAGA e o estado final positivo.
export function StatusFaturaBadge({ status }: { status: StatusFatura }) {
  const config = CONFIG_POR_STATUS[status]

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
        config.className,
      )}
    >
      {config.label}
    </span>
  )
}
