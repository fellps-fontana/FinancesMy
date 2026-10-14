import { cn } from "@/shared/lib/utils"
import { CompraFaturaItem, GRADE_COLUNAS_COMPRA } from "@/features/cartao/components/CompraFaturaItem"
import type { CategoriaResolvida } from "@/features/categorias/lib/construirMapaCategorias"
import type { CompraResponse } from "@/features/cartao/types"

type ListaComprasFaturaProps = {
  compras: CompraResponse[] | undefined
  carregando: boolean
  erro: string | null
  categoriaPorId: Record<string, CategoriaResolvida>
  onAntecipar: (compra: CompraResponse) => void
  onAbrirAcoes: (compra: CompraResponse) => void
}

// Tabela de compras da fatura (mockup s_cartao): card com cabecalho no
// desktop, lista simples no mobile.
export function ListaComprasFatura({
  compras,
  carregando,
  erro,
  categoriaPorId,
  onAntecipar,
  onAbrirAcoes,
}: ListaComprasFaturaProps) {
  if (erro) {
    return <p className="text-sm text-negativo">{erro}</p>
  }
  if (carregando) {
    return <p className="text-sm text-text-muted">Carregando compras...</p>
  }
  if (!compras || compras.length === 0) {
    return <p className="text-sm text-text-muted">Nenhuma compra nesta fatura.</p>
  }

  return (
    <div className="md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
      <div
        className={cn(
          "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted md:grid",
          GRADE_COLUNAS_COMPRA,
        )}
      >
        <span>Descricao</span>
        <span>Data</span>
        <span className="text-right">Valor</span>
        <span />
      </div>
      <ul>
        {compras.map((compra) => (
          <CompraFaturaItem
            key={compra.id}
            compra={compra}
            iconeCategoria={compra.categoriaId ? (categoriaPorId[compra.categoriaId]?.icone ?? null) : null}
            onAntecipar={onAntecipar}
            onAbrirAcoes={onAbrirAcoes}
          />
        ))}
      </ul>
    </div>
  )
}
