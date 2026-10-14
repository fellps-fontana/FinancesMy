import { ChevronsRight } from "lucide-react"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { CompraResponse } from "@/features/cartao/types"

type AcoesCompraSheetProps = {
  compra: CompraResponse | null
  /** Parcelas elegiveis segundo o backend; null enquanto carrega. */
  quantidadeElegiveis: number | null
  onAnteciparParcelas: () => void
  onFechar: () => void
}

function descreverElegiveis(quantidade: number | null): string {
  if (quantidade === null) return "Carregando parcelas..."
  if (quantidade === 0) return "Nenhuma parcela elegivel"
  return `${quantidade} ${quantidade === 1 ? "parcela disponivel" : "parcelas disponiveis"}`
}

// Folha de acoes mobile da compra parcelada (mockup mobile cmenu).
export function AcoesCompraSheet({
  compra,
  quantidadeElegiveis,
  onAnteciparParcelas,
  onFechar,
}: AcoesCompraSheetProps) {
  const titulo = compra?.descricao ?? "Sem descricao"
  const subtitulo =
    compra && compra.parcelaNumero !== null
      ? `Parcela ${compra.parcelaNumero} de ${compra.totalParcelas} · ${formatarMoeda(compra.valor)}`
      : compra
        ? formatarMoeda(compra.valor)
        : ""

  return (
    <BottomSheet open={compra !== null} onClose={onFechar} title={titulo} hideHeader>
      <div className="mx-auto h-1 w-9 rounded-full bg-border" aria-hidden="true" />
      <div>
        <h2 className="text-base font-medium text-text-primary">{titulo}</h2>
        <div className="mt-0.5 text-xs text-text-faint">{subtitulo}</div>
      </div>
      <button
        type="button"
        disabled={quantidadeElegiveis === null || quantidadeElegiveis === 0}
        onClick={onAnteciparParcelas}
        className="flex items-center gap-3 rounded-xl bg-muted p-3.5 text-left outline-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-60"
      >
        <ChevronsRight className="size-[18px] shrink-0 text-accent-soft" aria-hidden="true" />
        <span className="flex min-w-0 flex-col">
          <span className="text-sm text-accent-soft">Antecipar parcelas</span>
          <span className="text-xs text-text-faint">{descreverElegiveis(quantidadeElegiveis)}</span>
        </span>
      </button>
    </BottomSheet>
  )
}
