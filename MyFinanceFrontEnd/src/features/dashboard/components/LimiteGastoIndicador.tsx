import { Link } from "react-router-dom"
import { Card, CardContent } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { useGastoVsLimiteTodasCategorias } from "@/features/limite-gasto/hooks/useGastoVsLimiteTodasCategorias"
import {
  larguraBarraUso,
  obterEstadoLimite,
  rotuloUso,
  type EstadoLimite,
} from "@/features/limite-gasto/lib/estadoLimite"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

// Cor semantica por estado (identidade-visual.md: "cor com significado").
// estourado -> negativo; perto -> alerta; ok -> positivo.
const CONFIG_POR_ESTADO: Record<EstadoLimite, { barra: string; texto: string; label: string }> = {
  estourado: { barra: "bg-negativo", texto: "text-negativo", label: "Limite estourado" },
  perto: { barra: "bg-alerta", texto: "text-alerta", label: "Perto do limite" },
  ok: { barra: "bg-positivo", texto: "text-positivo", label: "Dentro do limite" },
}

type LimiteGastoIndicadorProps = {
  ano: number
  mes: number
  className?: string
}

export function LimiteGastoIndicador({ ano, mes, className }: LimiteGastoIndicadorProps) {
  const { data, isLoading, isError } = useGastoVsLimiteTodasCategorias(ano, mes)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-2">
          <span className="text-[13px] text-text-muted">Sublimite de gasto por categoria (mes)</span>
          <Link
            to="/planejamento"
            className="text-[12px] text-accent-soft underline-offset-4 hover:underline"
          >
            Ver planejamento
          </Link>
        </div>

        {isLoading && <p className="text-sm text-text-muted">Carregando limites...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar os limites de gasto.</p>
        )}

        {!isLoading && !isError && (data === undefined || data.length === 0) && (
          <p className="text-sm text-text-muted">
            Nenhuma categoria com sublimite definido no plano deste mes.
          </p>
        )}

        {!isLoading && !isError && data !== undefined && data.length > 0 && (
          <ul className="flex flex-col gap-3">
            {data.map((item) => (
              <LinhaLimiteGasto key={item.categoriaId} item={item} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

type LinhaLimiteGastoProps = {
  item: GastoVsLimiteResponse
}

function LinhaLimiteGasto({ item }: LinhaLimiteGastoProps) {
  const estado = obterEstadoLimite(item)
  const config = CONFIG_POR_ESTADO[estado]
  const larguraBarra = larguraBarraUso(item.percentualUtilizado, item.estourado)

  return (
    <li>
      <Link
        to={`/planejamento?categoriaId=${item.categoriaId}`}
        className={cn(
          "-mx-1 flex flex-col gap-1.5 rounded-lg px-1 py-0.5 transition-colors",
          "hover:bg-bg-surface-alt focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent",
        )}
      >
        <div className="flex items-center justify-between gap-2">
          <span className="text-sm text-text-body">{item.categoriaNome}</span>
          <span className={cn("text-[12px] font-medium", config.texto)}>{config.label}</span>
        </div>

        <div className="h-1.5 w-full overflow-hidden rounded-full bg-accent">
          <div
            className={cn("h-full rounded-full", config.barra)}
            style={{ width: `${larguraBarra}%` }}
          />
        </div>

        <div className="flex items-center justify-between text-[12px] text-text-faint">
          <span>
            {formatarMoeda(item.gastoRealizado)} de {formatarMoeda(item.valorLimite)}
          </span>
          <span>{rotuloUso(item.percentualUtilizado, item.estourado)}</span>
        </div>
      </Link>
    </li>
  )
}
