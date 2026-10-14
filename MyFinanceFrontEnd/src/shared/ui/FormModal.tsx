import type { ReactNode } from "react"
import { X } from "lucide-react"
import { BottomSheet } from "@/shared/ui/bottom-sheet"

// Estilos de campo e botao dos modais de formulario (mockups m_cartao, cAnt, m_fixa):
// campo 14px, padding 12x14, raio 10, fundo de superficie alternativa; botoes
// 11x18, raio 10.
export const CLASSE_CAMPO =
  "h-auto rounded-[10px] border-border bg-accent px-3.5 py-3 text-sm text-text-body placeholder:text-text-faint dark:bg-accent"

export const CLASSE_BOTAO_CANCELAR =
  "h-auto flex-1 rounded-[10px] border-border bg-accent px-[18px] py-[11px] text-sm text-text-body hover:bg-accent/70 md:flex-none dark:bg-accent dark:hover:bg-accent/70"

export const CLASSE_BOTAO_PRIMARIO =
  "h-auto flex-1 rounded-[10px] px-[18px] py-[11px] text-sm font-medium md:flex-none"

type FormModalProps = {
  open?: boolean
  title: string
  onClose: () => void
  children: ReactNode
}

// Caixa dos modais de formulario: 480px (ou largura da tela - 32px), padding
// 26px, gap 16px, cabecalho com titulo 19px e X de 18px. No mobile segue como
// folha inferior (comportamento do BottomSheet).
export function FormModal({ open = true, title, onClose, children }: FormModalProps) {
  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={title}
      hideHeader
      className="p-[26px] md:max-w-[min(480px,calc(100vw-2rem))]"
    >
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-[19px] font-medium text-pretty text-text-primary">{title}</h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Fechar"
          className="shrink-0 rounded-md text-text-muted outline-none hover:text-text-body focus-visible:ring-3 focus-visible:ring-ring/50"
        >
          <X className="size-[18px]" aria-hidden="true" />
        </button>
      </div>
      {children}
    </BottomSheet>
  )
}
