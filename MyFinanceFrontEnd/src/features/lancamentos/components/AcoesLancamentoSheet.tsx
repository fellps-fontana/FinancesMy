import type { ReactNode } from "react"
import { ArrowUpRight, Check, Pencil, Trash2 } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { resolverDisponibilidadeDeEdicao } from "@/features/lancamentos/lib/dicaDeEdicaoLancamento"
import type { LancamentoResponse } from "@/features/lancamentos/types"

type AcoesLancamentoSheetProps = {
  lancamento: LancamentoResponse | null
  podeCancelarCompra: boolean
  marcandoComoPago: boolean
  onFechar: () => void
  onEditar: (lancamento: LancamentoResponse) => void
  onExcluir: (lancamento: LancamentoResponse) => void
  onCancelarCompra: (lancamento: LancamentoResponse) => void
  onMarcarComoPago: (lancamento: LancamentoResponse) => void
  onIrParaOrigem: (rota: string) => void
}

type LinhaAcaoProps = {
  icone: ReactNode
  rotulo: string
  dica?: string | null
  destrutiva?: boolean
  disabled?: boolean
  onClick: () => void
}

function LinhaAcao({ icone, rotulo, dica, destrutiva, disabled, onClick }: LinhaAcaoProps) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className="flex items-center gap-3 rounded-xl bg-muted p-3.5 text-left outline-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-60"
    >
      <span className={cn("shrink-0", destrutiva ? "text-negativo" : "text-text-muted")}>{icone}</span>
      <span className="flex min-w-0 flex-col">
        <span className={cn("text-sm", destrutiva ? "text-negativo" : "text-text-body")}>{rotulo}</span>
        {dica && <span className="text-xs text-text-faint">{dica}</span>}
      </span>
    </button>
  )
}

// Folha de acoes mobile (mockup sheet:lanc:* / lmenu). Habilitacao de
// Editar/Excluir vem de `avulso` (item 18); sem avulso, a dica aponta a origem
// e, quando existe tela de origem, oferece o atalho.
export function AcoesLancamentoSheet({
  lancamento,
  podeCancelarCompra,
  marcandoComoPago,
  onFechar,
  onEditar,
  onExcluir,
  onCancelarCompra,
  onMarcarComoPago,
  onIrParaOrigem,
}: AcoesLancamentoSheetProps) {
  const titulo = lancamento?.descricao ?? "Sem descricao"
  const disponibilidade = lancamento ? resolverDisponibilidadeDeEdicao(lancamento) : null
  const rotaDestino = disponibilidade?.rotaDestino ?? null
  const rotuloDestino = disponibilidade?.rotuloDestino ?? null
  const ehParcela = lancamento?.compraParceladaId != null

  const subtitulo = lancamento
    ? ehParcela && lancamento.parcelaNumero !== null
      ? `Parcela ${lancamento.parcelaNumero} de ${lancamento.totalParcelas} · ${formatarMoeda(lancamento.valor)}`
      : formatarMoeda(lancamento.valor)
    : ""

  return (
    <BottomSheet open={lancamento !== null} onClose={onFechar} title={titulo} hideHeader>
      {lancamento && disponibilidade && (
        <>
          <div className="mx-auto h-1 w-9 rounded-full bg-border" aria-hidden="true" />
          <div>
            <h2 className="text-base font-medium text-text-primary">{titulo}</h2>
            <div className="mt-0.5 text-xs text-text-faint">{subtitulo}</div>
          </div>

          <div className="flex flex-col gap-2.5">
            {lancamento.status === "PENDENTE" && (
              <LinhaAcao
                icone={<Check className="size-[18px]" aria-hidden="true" />}
                rotulo="Marcar como pago"
                disabled={marcandoComoPago}
                onClick={() => onMarcarComoPago(lancamento)}
              />
            )}

            {ehParcela ? (
              podeCancelarCompra && (
                <LinhaAcao
                  icone={<Trash2 className="size-[18px]" aria-hidden="true" />}
                  rotulo="Cancelar compra parcelada"
                  destrutiva
                  onClick={() => onCancelarCompra(lancamento)}
                />
              )
            ) : (
              <>
                <LinhaAcao
                  icone={<Pencil className="size-[18px]" aria-hidden="true" />}
                  rotulo="Editar"
                  dica={disponibilidade.dica}
                  disabled={!disponibilidade.podeEditarExcluir}
                  onClick={() => onEditar(lancamento)}
                />
                <LinhaAcao
                  icone={<Trash2 className="size-[18px]" aria-hidden="true" />}
                  rotulo="Excluir"
                  destrutiva
                  dica={disponibilidade.dica}
                  disabled={!disponibilidade.podeEditarExcluir}
                  onClick={() => onExcluir(lancamento)}
                />
              </>
            )}

            {rotaDestino && rotuloDestino && (
              <LinhaAcao
                icone={<ArrowUpRight className="size-[18px]" aria-hidden="true" />}
                rotulo={`Ir para ${rotuloDestino}`}
                onClick={() => onIrParaOrigem(rotaDestino)}
              />
            )}
          </div>
        </>
      )}
    </BottomSheet>
  )
}
