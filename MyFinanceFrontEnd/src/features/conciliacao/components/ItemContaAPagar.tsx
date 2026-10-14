import type { ReactNode } from "react"
import type { LucideIcon } from "lucide-react"
import { Badge, type BadgeVariant } from "@/shared/ui/badge"
import { IconTile } from "@/shared/ui/icon-tile"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"

type ItemContaAPagarProps = {
  descricao: string
  icone: LucideIcon
  rotuloVencimento: string
  vencimentoAtrasado?: boolean
  valor: number
  statusLabel: string
  statusVariant: BadgeVariant
  // Slot da acao (Pagar / link). Ausente na aba Conciliado.
  acao?: ReactNode
  erro?: string | null
}

// Apresentacao pura de uma linha do mockup "Conciliacao": desktop em grid
// (descricao / vencimento / valor / status / acao), mobile em card.
export function ItemContaAPagar({
  descricao,
  icone: Icone,
  rotuloVencimento,
  vencimentoAtrasado = false,
  valor,
  statusLabel,
  statusVariant,
  acao,
  erro,
}: ItemContaAPagarProps) {
  const corVencimento = vencimentoAtrasado ? "text-negativo" : "text-text-faint md:text-text-muted"

  return (
    <li className="flex flex-col gap-1.5 rounded-[14px] border-[0.5px] border-border bg-card p-3.5 md:grid md:grid-cols-[2.4fr_1fr_1fr_1fr_0.8fr] md:items-center md:gap-0 md:rounded-none md:border-0 md:border-b-[0.5px] md:bg-transparent md:px-[22px] md:py-3.5 md:last:border-b-0">
      <div className="flex items-center gap-3">
        <IconTile>
          <Icone className="size-4" strokeWidth={1.6} aria-hidden="true" />
        </IconTile>
        <div className="flex min-w-0 flex-1 flex-col">
          <span className="truncate text-[14px] text-text-body">{descricao}</span>
          <span className={cn("text-xs md:hidden", corVencimento)}>{rotuloVencimento}</span>
        </div>
        <div className="flex flex-col items-end gap-1 md:hidden">
          <span className="text-[14px] text-text-primary">{formatarMoeda(valor)}</span>
          <Badge variant={statusVariant}>{statusLabel}</Badge>
        </div>
      </div>

      <span className={cn("hidden text-[13px] md:block", corVencimento)}>{rotuloVencimento}</span>
      <span className="hidden text-right text-[14px] text-text-primary md:block">{formatarMoeda(valor)}</span>
      <span className="hidden justify-self-end md:block">
        <Badge variant={statusVariant}>{statusLabel}</Badge>
      </span>

      {acao ? <div className="flex justify-end md:justify-self-end">{acao}</div> : <span className="hidden md:block" />}
      {erro ? <p className="text-xs text-negativo md:col-span-5">{erro}</p> : null}
    </li>
  )
}
