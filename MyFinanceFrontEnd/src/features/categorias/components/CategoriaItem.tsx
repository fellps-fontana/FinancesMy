import { Pencil, Archive, RotateCcw } from "lucide-react"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { cn } from "@/shared/lib/utils"
import { Badge } from "@/shared/ui/badge"
import { Button } from "@/shared/ui/button"
import { IconTile } from "@/shared/ui/icon-tile"
import { ProgressBar } from "@/shared/ui/progress-bar"
import type { CategoriaResponse } from "@/features/categorias/types"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

type CategoriaItemProps = {
  categoria: CategoriaResponse
  gastoVsLimitePorCategoriaId: Record<string, GastoVsLimiteResponse>
  onEditar: (categoria: CategoriaResponse) => void
  onArquivar: (categoria: CategoriaResponse) => void
  onReativar: (categoria: CategoriaResponse) => void
  /** Mobile: toque na linha abre a folha de acoes. */
  onAbrirAcoes: (categoria: CategoriaResponse) => void
  /** Subcategoria nao ganha card proprio: ja esta dentro do card do pai. */
  aninhada?: boolean
}

// Apresentacao pura (regra-de-negocio.md item 7: categoria/subcategoria via
// parentId, arquivar e a unica exclusao; item 14: limite mensal). Gasto,
// percentual e `estourado` chegam prontos do backend (GastoVsLimiteResponse) -
// aqui so se exibe: a barra recebe o percentual (fracao -> escala 0-100 da
// barra) e `estourado` decide a cor (coral = gasto acima do limite).
// Categoria sem limite nao tem barra. Desktop: icones de acao na linha;
// mobile: a linha inteira e um alvo de toque que abre a folha de acoes.
export function CategoriaItem({
  categoria,
  gastoVsLimitePorCategoriaId,
  onEditar,
  onArquivar,
  onReativar,
  onAbrirAcoes,
  aninhada = false,
}: CategoriaItemProps) {
  const Icone = obterIconeCatalogo(categoria.icone)
  const comparativo = gastoVsLimitePorCategoriaId[categoria.id]
  const estourado = comparativo?.estourado ?? false

  return (
    <div
      className={cn(
        "flex flex-col gap-3",
        !aninhada && "md:rounded-[14px] md:border-[0.5px] md:border-border md:bg-card md:p-4",
      )}
    >
      <div className="relative flex items-center gap-3 md:gap-3.5">
        <button
          type="button"
          aria-label={`Acoes de ${categoria.nome}`}
          onClick={() => onAbrirAcoes(categoria)}
          className="absolute inset-0 rounded-lg outline-none focus-visible:ring-3 focus-visible:ring-ring/50 md:hidden"
        />

        <IconTile>
          <Icone strokeWidth={1.6} aria-hidden="true" />
        </IconTile>

        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-2">
            <span className="flex min-w-0 items-center gap-2">
              <span
                className={cn(
                  "truncate text-sm",
                  categoria.arquivada ? "text-text-muted" : "text-text-body",
                )}
              >
                {categoria.nome}
              </span>
              {categoria.arquivada && <Badge variant="manual">Arquivada</Badge>}
            </span>
            {comparativo && (
              <span
                className={cn(
                  "shrink-0 text-[13px]",
                  estourado ? "text-negativo" : "text-text-primary",
                )}
              >
                {formatarMoeda(comparativo.gastoRealizado)} / {formatarMoeda(comparativo.valorLimite)}
              </span>
            )}
          </div>
          {comparativo && (
            <ProgressBar
              className="mt-1.5"
              percent={comparativo.percentualUtilizado * 100}
              exceeded={estourado}
              aria-label={`Uso do limite de ${categoria.nome}`}
            />
          )}
        </div>

        <div className="relative hidden items-center gap-1 md:flex">
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            title="Editar"
            aria-label={`Editar ${categoria.nome}`}
            onClick={() => onEditar(categoria)}
          >
            <Pencil className="size-4 text-text-muted" aria-hidden="true" />
          </Button>
          {categoria.arquivada ? (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              title="Reativar"
              aria-label={`Reativar ${categoria.nome}`}
              onClick={() => onReativar(categoria)}
            >
              <RotateCcw className="size-4 text-text-muted" aria-hidden="true" />
            </Button>
          ) : (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              title="Arquivar"
              aria-label={`Arquivar ${categoria.nome}`}
              onClick={() => onArquivar(categoria)}
            >
              <Archive className="size-4 text-text-muted" aria-hidden="true" />
            </Button>
          )}
        </div>
      </div>

      {categoria.subcategorias.length > 0 && (
        <div className="flex flex-col gap-3 border-l-[0.5px] border-border pl-4">
          {categoria.subcategorias.map((subcategoria) => (
            <CategoriaItem
              key={subcategoria.id}
              categoria={subcategoria}
              gastoVsLimitePorCategoriaId={gastoVsLimitePorCategoriaId}
              onEditar={onEditar}
              onArquivar={onArquivar}
              onReativar={onReativar}
              onAbrirAcoes={onAbrirAcoes}
              aninhada
            />
          ))}
        </div>
      )}
    </div>
  )
}
