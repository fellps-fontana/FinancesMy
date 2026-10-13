import { ChevronRight } from "lucide-react"
import { Link } from "react-router-dom"
import { IconTile } from "@/shared/ui/icon-tile"
import { ProgressBar } from "@/shared/ui/progress-bar"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { LinhaGastoPorCategoria } from "@/features/relatorios/types"

type Props = {
  linha: LinhaGastoPorCategoria
  // Destino ja resolvido pela pagina (preserva mes/ano na query string).
  to: string
}

const formatadorPercentual = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })

// Valor, percentual e barra vem prontos do backend; aqui so se exibe.
export function LinhaCategoriaRelatorio({ linha, to }: Props) {
  const Icone = obterIconeCatalogo(linha.icone)
  return (
    <Link
      to={to}
      className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-lg py-1 outline-none focus-visible:ring-2 focus-visible:ring-ring md:flex-nowrap"
    >
      <IconTile className="size-[30px]">
        <Icone strokeWidth={1.6} aria-hidden="true" />
      </IconTile>
      <span className="min-w-0 flex-1 truncate text-[14px] text-foreground">{linha.nome}</span>
      <ProgressBar
        percent={linha.percentual}
        className="order-last basis-full md:order-none md:w-40 md:basis-auto lg:w-56"
      />
      <span className="w-12 text-right text-[13px] text-muted-foreground">
        {formatadorPercentual.format(linha.percentual)}%
      </span>
      <span className="w-[100px] text-right text-[14px] text-text-primary">
        {formatarMoeda(linha.valor)}
      </span>
      <ChevronRight className="size-3.5 shrink-0 text-text-faint" aria-hidden="true" />
    </Link>
  )
}
