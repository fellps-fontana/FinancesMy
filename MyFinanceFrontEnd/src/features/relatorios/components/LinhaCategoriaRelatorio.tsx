import { ChevronRight } from "lucide-react"
import { Link } from "react-router-dom"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"
import { larguraDaBarra, type CorFatia } from "@/features/relatorios/lib/fatiasRelatorio"
import { formatarPercentualRelatorio } from "@/features/relatorios/lib/regrasRelatorio"
import type { LinhaGastoPorCategoria } from "@/features/relatorios/types"

type Props = {
  linha: LinhaGastoPorCategoria
  cor: CorFatia
  // Destino ja resolvido pela pagina (preserva mes/ano na query string).
  to: string
}

// Linha do relatorio (mockup s_relatorio): quadradinho na cor da fatia, nome,
// barra proporcional, % e valor. Valor e percentual vem prontos do backend.
export function LinhaCategoriaRelatorio({ linha, cor, to }: Props) {
  return (
    <Link
      to={to}
      className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg py-1 outline-none focus-visible:ring-2 focus-visible:ring-ring md:flex-nowrap"
    >
      <span className={cn("size-2.5 shrink-0 rounded-[3px]", cor.fundo)} aria-hidden="true" />
      <span className="min-w-0 flex-1 truncate text-sm text-text-body">{linha.nome}</span>
      <div
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(larguraDaBarra(linha.percentual))}
        aria-label={`Participacao de ${linha.nome}`}
        className="order-last h-1.5 basis-full overflow-hidden rounded-[5px] bg-accent md:order-none md:flex-[2] md:basis-0"
      >
        <div className={cn("h-full", cor.fundo)} style={{ width: `${larguraDaBarra(linha.percentual)}%` }} />
      </div>
      <span className="w-10 shrink-0 text-right text-[13px] text-text-muted">
        {formatarPercentualRelatorio(linha.percentual)}%
      </span>
      <span className="w-[92px] shrink-0 text-right text-sm whitespace-nowrap text-text-primary">
        {formatarMoeda(linha.valor)}
      </span>
      <ChevronRight className="size-3.5 shrink-0 text-text-faint" aria-hidden="true" />
    </Link>
  )
}
