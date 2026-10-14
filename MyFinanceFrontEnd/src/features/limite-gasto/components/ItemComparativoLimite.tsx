import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { larguraBarraPercentual, obterEstadoLimite, type EstadoLimite } from "@/features/limite-gasto/lib/estadoLimite"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

const formatadorPercentual = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  maximumFractionDigits: 0,
})

// Grade compartilhada entre o cabecalho (pagina) e as linhas (desktop):
// Categoria / Gasto / Limite / barra / %.
export const GRADE_COLUNAS_LIMITE = "md:grid-cols-[2fr_1fr_1fr_2fr_0.6fr]"

type ConfigEstado = {
  dot: string
  barra: string
  texto: string
  badge: string
  label: string | null
}

// Cor com significado (identidade-visual.md "Principios"): estourado ->
// negativo; perto -> alerta; ok -> positivo. Mesmo mapeamento de
// LimiteGastoIndicador.tsx (dashboard).
const CONFIG_POR_ESTADO: Record<EstadoLimite, ConfigEstado> = {
  estourado: {
    dot: "bg-negativo",
    barra: "bg-negativo",
    texto: "text-negativo",
    badge: "bg-negativo/15 text-negativo",
    label: "Estourado",
  },
  perto: {
    dot: "bg-alerta",
    barra: "bg-alerta",
    texto: "text-alerta",
    badge: "bg-alerta/15 text-alerta",
    label: "Perto do limite",
  },
  ok: {
    dot: "bg-positivo",
    barra: "bg-positivo",
    texto: "text-positivo",
    badge: "",
    label: null,
  },
}

type ItemComparativoLimiteProps = {
  item: GastoVsLimiteResponse
  // Alvo do deep-link `?categoriaId=`: so destaque visual.
  destacado?: boolean
}

// Apresentacao pura (regra-de-negocio.md item 14): valores ja vem calculados do
// backend. Desktop (md+): linha em grid; mobile: card.
export function ItemComparativoLimite({ item, destacado = false }: ItemComparativoLimiteProps) {
  const config = CONFIG_POR_ESTADO[obterEstadoLimite(item)]
  const percentual = formatadorPercentual.format(item.percentualUtilizado)

  const barra = (
    <div
      className="h-1.5 w-full overflow-hidden rounded-full bg-accent"
      role="progressbar"
      aria-label={`Uso do limite de ${item.categoriaNome}`}
      aria-valuenow={Math.round(item.percentualUtilizado * 100)}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <div
        className={cn("h-full rounded-full", config.barra)}
        style={{ width: `${larguraBarraPercentual(item.percentualUtilizado)}%` }}
      />
    </div>
  )

  const selo = config.label ? (
    <span
      className={cn(
        "inline-flex shrink-0 items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
        config.badge,
      )}
    >
      {config.label}
    </span>
  ) : null

  return (
    <li
      className={cn(
        "rounded-[14px] border-[0.5px] border-border bg-card p-3.5 md:rounded-none md:border-0 md:border-b-[0.5px] md:bg-transparent md:px-[22px] md:py-3.5 md:last:border-b-0",
        destacado && "bg-accent-deep/30 ring-1 ring-accent md:bg-accent-deep/30",
      )}
    >
      {/* Mobile */}
      <div className="flex flex-col gap-2 md:hidden">
        <div className="flex items-center gap-2.5">
          <span className={cn("h-2.5 w-2.5 shrink-0 rounded-[3px]", config.dot)} aria-hidden="true" />
          <span className="min-w-0 flex-1 truncate text-sm text-text-body">{item.categoriaNome}</span>
          {selo}
        </div>
        {barra}
        <div className="flex items-center justify-between text-[12px] text-text-faint">
          <span>
            {formatarMoeda(item.gastoRealizado)} de {formatarMoeda(item.valorLimite)}
          </span>
          <span className={cn("font-medium", config.texto)}>{percentual}</span>
        </div>
      </div>

      {/* Desktop */}
      <div className={cn("hidden items-center gap-4 md:grid", GRADE_COLUNAS_LIMITE)}>
        <div className="flex min-w-0 items-center gap-2.5">
          <span className={cn("h-2.5 w-2.5 shrink-0 rounded-[3px]", config.dot)} aria-hidden="true" />
          <span className="truncate text-sm text-text-body">{item.categoriaNome}</span>
          {selo}
        </div>
        <span className="text-right text-sm text-text-primary">{formatarMoeda(item.gastoRealizado)}</span>
        <span className="text-right text-sm text-text-muted">{formatarMoeda(item.valorLimite)}</span>
        {barra}
        <span className={cn("text-right text-sm font-medium", config.texto)}>{percentual}</span>
      </div>
    </li>
  )
}
