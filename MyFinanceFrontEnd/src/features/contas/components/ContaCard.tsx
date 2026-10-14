import { Link } from "react-router-dom"
import { Banknote, Pencil, Power } from "lucide-react"
import { Button } from "@/shared/ui/button"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { obterSubtituloConta } from "@/features/contas/lib/obterSubtituloConta"
import { ContaIcone } from "@/features/contas/components/ContaIcone"
import { ContaBadgeOrigem } from "@/features/contas/components/ContaBadgeOrigem"
import type { ContaResponse } from "@/features/contas/types"

type ContaCardProps = {
  conta: ContaResponse
  onEditar: () => void
  onEditarSaldo: () => void
  onDesativar: () => void
}

// Apresentacao pura do item de conta (mockup s_contas): icone, nome,
// subtitulo, saldo e badge de origem, mais as acoes de atualizar saldo e
// desativar. Cartao nao tem saldo manual: o saldo e a fatura vivem em /cartao,
// entao o card leva para la em vez de oferecer as acoes.
export function ContaCard({ conta, onEditar, onEditarSaldo, onDesativar }: ContaCardProps) {
  const ehCartao = conta.tipo === "Cartao"

  return (
    <div className="flex items-center gap-3.5 rounded-[14px] border-[0.5px] border-border bg-card p-3.5 md:p-[18px]">
      <ContaIcone conta={conta} />

      <div className="min-w-0 flex-1">
        <div className="truncate text-sm text-text-body">{conta.nome}</div>
        <div className="mt-0.5 truncate text-xs text-text-faint">{obterSubtituloConta(conta)}</div>
      </div>

      {ehCartao ? (
        <>
          <Link
            to="/cartao"
            className="shrink-0 rounded-md text-[13px] text-primary outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
          >
            Ver cartao
          </Link>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={`Editar ${conta.nome}`}
            title="Editar"
            onClick={onEditar}
          >
            <Pencil className="size-4 text-text-muted" aria-hidden="true" />
          </Button>
        </>
      ) : (
        <>
          <div className="shrink-0 text-right">
            <div className="text-sm whitespace-nowrap text-text-primary">{formatarMoeda(conta.saldo)}</div>
            <div className="mt-0.5 flex justify-end">
              <ContaBadgeOrigem origem={conta.origem} />
            </div>
          </div>
          <div className="flex shrink-0 items-center">
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={`Editar ${conta.nome}`}
              title="Editar"
              onClick={onEditar}
            >
              <Pencil className="size-4 text-text-muted" aria-hidden="true" />
            </Button>
            {!conta.saldoCalculado && (
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={`Atualizar saldo de ${conta.nome}`}
                title="Atualizar saldo"
                onClick={onEditarSaldo}
              >
                <Banknote className="size-4 text-text-muted" aria-hidden="true" />
              </Button>
            )}
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={`Desativar ${conta.nome}`}
              title="Desativar"
              onClick={onDesativar}
            >
              <Power className="size-4 text-negativo" aria-hidden="true" />
            </Button>
          </div>
        </>
      )}
    </div>
  )
}
