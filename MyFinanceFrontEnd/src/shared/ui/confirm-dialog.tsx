import type { ReactNode } from "react"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { Button } from "@/shared/ui/button"
import { cn } from "@/shared/lib/utils"

type ConfirmDialogProps = {
  open: boolean
  title: string
  /** Corpo explicativo (ex: consequencia da acao). */
  description: ReactNode
  /** Icone (ex: <Trash2 />); o tile 34px e aplicado aqui. */
  icon?: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  /** Variante destrutiva: confirmar em coral (negativo). */
  destructive?: boolean
  /** Desabilita os botoes enquanto a acao esta em andamento. */
  loading?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/**
 * Dialogo de confirmacao (ex: "Cancelar Notebook Dell?"). Reusa a mecanica do
 * dialog (portal, Esc, clique fora, foco) via BottomSheet centralizado, sem o
 * cabecalho com X: o titulo vem no corpo, junto do icone, como no mockup.
 */
export function ConfirmDialog({
  open,
  title,
  description,
  icon,
  confirmLabel = "Confirmar",
  cancelLabel = "Voltar",
  destructive = false,
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <BottomSheet
      open={open}
      onClose={onCancel}
      title={title}
      hideHeader
      placement="center"
      className="max-w-[400px] gap-3"
    >
      {icon ? (
        <div
          aria-hidden="true"
          className={cn(
            "flex size-[34px] items-center justify-center rounded-[10px]",
            destructive ? "bg-negativo/15 text-negativo" : "bg-accent-deep text-accent-soft",
          )}
        >
          {icon}
        </div>
      ) : null}
      <h2 className="mt-1 text-[17px] font-medium text-text-primary">{title}</h2>
      <div className="text-sm leading-normal text-text-muted">{description}</div>
      <div className="mt-2.5 flex justify-end gap-2.5">
        <Button type="button" variant="outline" size="lg" disabled={loading} onClick={onCancel}>
          {cancelLabel}
        </Button>
        <Button
          type="button"
          size="lg"
          disabled={loading}
          onClick={onConfirm}
          className={destructive ? "bg-destructive text-background hover:bg-destructive/80" : undefined}
        >
          {confirmLabel}
        </Button>
      </div>
    </BottomSheet>
  )
}
