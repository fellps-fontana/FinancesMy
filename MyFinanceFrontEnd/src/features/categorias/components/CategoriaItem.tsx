import { useState } from "react"
import { cn } from "@/shared/lib/utils"
import { ApiError } from "@/shared/api/client"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { CampoLimiteGasto } from "@/features/categorias/components/CampoLimiteGasto"
import { useArquivarCategoria } from "@/features/categorias/hooks/useArquivarCategoria"
import { useReativarCategoria } from "@/features/categorias/hooks/useReativarCategoria"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"
import type { LimiteGastoResponse } from "@/features/limite-gasto/types"

// CampoLimiteGasto (ver comentario em types.ts) espera "DESPESA"/"RECEITA"
// em caixa alta, mas o backend serializa TipoCategoria em PascalCase
// ("Despesa"/"Receita" — Program.cs registra JsonStringEnumConverter sem
// naming policy). Conversao explicita e tipada em vez de `.toUpperCase()`
// solto, pra nao depender de cast e deixar o de-para visivel.
const TIPO_CATEGORIA_MAIUSCULO: Record<TipoCategoria, "DESPESA" | "RECEITA"> = {
  Despesa: "DESPESA",
  Receita: "RECEITA",
}

// Arquivada e estado intencional do usuario (soft-delete, regra-de-negocio.md
// item 7), nao uma pendencia — por isso segue o mesmo par neutro (bg-muted +
// text-text-muted) que identidade-visual.md reserva para "manual -> neutro",
// mesmo espirito ja usado em ContaFixaItem para o par ativa/inativa.
const BADGE_ARQUIVADA_CLASSNAME = "bg-muted text-text-muted"

type CategoriaItemProps = {
  categoria: CategoriaResponse
  limitesPorCategoriaId: Record<string, LimiteGastoResponse>
  onEditar: (categoria: CategoriaResponse) => void
}

// Componente recursivo: renderiza a categoria e, logo abaixo, suas
// subcategorias (regra-de-negocio.md item 7 — auto-relacionamento via
// parentId). A indentacao por nivel nasce da propria recursao (cada chamada
// aninhada ja vem envolvida no wrapper com `pl-4 border-l` do nivel pai) —
// nao precisa de um prop `nivel` nem calculo de profundidade aqui.
//
// Arquivar/Reativar sao MUTUAMENTE EXCLUSIVOS (nunca os dois ao mesmo
// tempo): dependem so de `categoria.arquivada`, mesmo par de acoes/estado ja
// usado em ContaFixaItem para ativa/inativa (regra-de-negocio.md item 6).
//
// Editar: este componente so dispara o gatilho via `onEditar`, sem importar
// formulario nenhum - quem decide renderizar o FormCategoria (TASK-102, ja
// existente e em uso via onEditar em CategoriasPage) e o componente pai.
export function CategoriaItem({ categoria, limitesPorCategoriaId, onEditar }: CategoriaItemProps) {
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: arquivar, isPending: arquivando } = useArquivarCategoria()
  const { mutate: reativar, isPending: reativando } = useReativarCategoria()

  function handleArquivar() {
    setErro(null)
    arquivar(categoria.id, {
      onError: (error) => {
        console.error("Falha ao arquivar categoria", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel arquivar a categoria. Tente novamente.",
        )
      },
    })
  }

  function handleReativar() {
    setErro(null)
    reativar(categoria.id, {
      onError: (error) => {
        console.error("Falha ao reativar categoria", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel reativar a categoria. Tente novamente.",
        )
      },
    })
  }

  const limiteAtual = limitesPorCategoriaId[categoria.id] ?? null

  return (
    <div className="flex flex-col gap-2">
      <Card size="sm">
        <CardContent className="flex flex-col gap-2">
          <div className="flex items-start justify-between gap-2">
            <span className="text-[14px] font-medium text-text-primary">{categoria.nome}</span>

            {categoria.arquivada && (
              <span
                className={cn(
                  "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
                  BADGE_ARQUIVADA_CLASSNAME,
                )}
              >
                Arquivada
              </span>
            )}
          </div>

          {categoria.tipo === "Despesa" && (
            <CampoLimiteGasto
              categoriaId={categoria.id}
              categoriaTipo={TIPO_CATEGORIA_MAIUSCULO[categoria.tipo]}
              limiteAtual={limiteAtual}
            />
          )}

          {erro && (
            <Alert variant="destructive">
              <AlertDescription>{erro}</AlertDescription>
            </Alert>
          )}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="sm" onClick={() => onEditar(categoria)}>
              Editar
            </Button>

            {categoria.arquivada ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={reativando}
                onClick={handleReativar}
              >
                {reativando ? "Reativando..." : "Reativar"}
              </Button>
            ) : (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={arquivando}
                onClick={handleArquivar}
              >
                {arquivando ? "Arquivando..." : "Arquivar"}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {categoria.subcategorias.length > 0 && (
        <div className="flex flex-col gap-2 border-l border-border pl-4">
          {categoria.subcategorias.map((subcategoria) => (
            <CategoriaItem
              key={subcategoria.id}
              categoria={subcategoria}
              limitesPorCategoriaId={limitesPorCategoriaId}
              onEditar={onEditar}
            />
          ))}
        </div>
      )}
    </div>
  )
}
