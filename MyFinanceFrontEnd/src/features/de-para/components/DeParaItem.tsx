import { ArrowRight, Pencil, Trash2 } from "lucide-react"
import { Badge } from "@/shared/ui/badge"
import { Button } from "@/shared/ui/button"
import { IconTile } from "@/shared/ui/icon-tile"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import type { DeParaCategoriaResponse } from "@/features/de-para/types"

type DeParaItemProps = {
  vinculo: DeParaCategoriaResponse
  onEditar: (vinculo: DeParaCategoriaResponse) => void
  onRemover: (vinculo: DeParaCategoriaResponse) => void
}

// Vinculo cadastrado (regra-de-negocio.md item 7): existir ja significa
// "Mapeado" - pendente/sugerido sao v2 e nao sao renderizados.
// Desktop (md+): linha em grid; mobile: card com "vira".
export function DeParaItem({ vinculo, onEditar, onRemover }: DeParaItemProps) {
  const nomeCategoria = vinculo.categoria?.nome ?? "Categoria removida"
  const Icone = obterIconeCatalogo(vinculo.categoria?.icone)

  const acoes = (
    <div className="flex shrink-0 items-center gap-1">
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label={`Editar vinculo de ${vinculo.categoriaPierre}`}
        onClick={() => onEditar(vinculo)}
      >
        <Pencil aria-hidden="true" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label={`Remover vinculo de ${vinculo.categoriaPierre}`}
        onClick={() => onRemover(vinculo)}
      >
        <Trash2 aria-hidden="true" />
      </Button>
    </div>
  )

  const categoriaDoApp = (
    <div className="flex min-w-0 flex-1 items-center gap-2.5 rounded-[9px] bg-muted px-3 py-2">
      <IconTile className="size-6 rounded-md [&_svg]:size-3.5">
        <Icone aria-hidden="true" />
      </IconTile>
      <span className="truncate text-[13px] text-text-body">{nomeCategoria}</span>
    </div>
  )

  return (
    <li className="rounded-[14px] border-[0.5px] border-border bg-card p-3.5 md:rounded-none md:border-0 md:border-b md:bg-transparent md:px-[22px] md:py-3.5 md:last:border-b-0">
      {/* Mobile */}
      <div className="flex flex-col gap-2.5 md:hidden">
        <div className="flex items-center justify-between gap-2">
          <span className="min-w-0 truncate text-xs text-text-faint">{vinculo.categoriaPierre}</span>
          <Badge variant="pago">Mapeado</Badge>
        </div>
        <div className="flex items-center gap-2.5">
          <span className="text-[13px] text-text-muted">vira</span>
          <ArrowRight className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
          {categoriaDoApp}
          {acoes}
        </div>
      </div>

      {/* Desktop */}
      <div className="hidden items-center gap-4 md:grid md:grid-cols-[2fr_0.4fr_2fr_1fr_auto]">
        <span className="truncate text-sm text-text-body">{vinculo.categoriaPierre}</span>
        <ArrowRight className="size-3.5 text-text-muted" aria-hidden="true" />
        {categoriaDoApp}
        <Badge variant="pago" className="justify-self-end">
          Mapeado
        </Badge>
        {acoes}
      </div>
    </li>
  )
}
