import { CreditCard, Plus } from "lucide-react"
import { Card } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"

type CartaoVisualProps = {
  nome: string
  selecionado?: boolean
  onSelecionar?: () => void
}

// Representacao visual do cartao fisico (preserva a intencao da branch de
// referencia, so trocando Card/CSS Module por shared/ui/card.tsx + Tailwind).
// O numero mascarado e decorativo: o backend nao expoe (e nao deveria expor)
// numero de cartao nem nome do titular - dado que nao existe no schema de Conta.
//
// Tambem funciona como item selecionavel na faixa de cartoes de
// ContaCartaoPage (regra de negocio item 12 - o backend ja suporta N contas
// tipo=CARTAO, sem restricao de quantidade). Quando `onSelecionar` e
// passado, o card vira clicavel/focavel; o cartao em foco ganha o anel roxo
// de acao (--primary, identidade-visual.md "accent (roxo)") e opacidade
// plena, enquanto os demais ficam esmaecidos - o estado "qual cartao estou
// vendo agora" precisa ficar visivel de relance, nao so no texto abaixo.
export function CartaoVisual({ nome, selecionado = false, onSelecionar }: CartaoVisualProps) {
  return (
    <Card
      className={cn(
        "flex w-56 shrink-0 flex-col gap-6 bg-accent-deep px-5 py-5 text-accent-soft ring-0 transition-opacity",
        onSelecionar && "cursor-pointer",
        onSelecionar && (selecionado ? "opacity-100 ring-2 ring-primary" : "opacity-60 hover:opacity-90"),
      )}
      onClick={onSelecionar}
      role={onSelecionar ? "button" : undefined}
      tabIndex={onSelecionar ? 0 : undefined}
      aria-pressed={onSelecionar ? selecionado : undefined}
      onKeyDown={
        onSelecionar
          ? (event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault()
                onSelecionar()
              }
            }
          : undefined
      }
    >
      <div className="flex items-center justify-between">
        <span className="text-[13px] font-medium">{nome}</span>
        <CreditCard className="size-5" strokeWidth={1.6} aria-hidden="true" />
      </div>
      <span className="text-sm tracking-[0.2em] text-text-primary">•••• •••• •••• ••••</span>
    </Card>
  )
}

type CartaoVisualNovoProps = {
  onClick: () => void
}

// Tile "adicionar cartao": mesmas dimensoes do CartaoVisual, mas com fundo
// semi-transparente e borda tracejada para marcar visualmente que NAO e um
// cartao real - e a acao de cadastrar um novo (mockup 05, faixa de cartoes).
// O backend ja permite N contas tipo=CARTAO (ver hooks/useContaCartaoAtual.ts
// e ContaService.CriarContaAsync/ValidarCartao, sem limite de quantidade),
// entao esta acao fica sempre disponivel ao lado dos cartoes existentes.
export function CartaoVisualNovo({ onClick }: CartaoVisualNovoProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex w-56 shrink-0 flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-border bg-card/40 px-5 py-5 text-text-muted transition-colors hover:bg-card/70 hover:text-text-body"
    >
      <Plus className="size-5" strokeWidth={1.6} aria-hidden="true" />
      <span className="text-[13px] font-medium">Novo cartao</span>
    </button>
  )
}
