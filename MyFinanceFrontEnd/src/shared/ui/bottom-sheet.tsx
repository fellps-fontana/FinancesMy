import { useEffect, useRef, type ReactNode } from "react"
import { createPortal } from "react-dom"
import { X } from "lucide-react"
import { Button } from "@/shared/ui/button"
import { cn } from "@/shared/lib/utils"

type BottomSheetProps = {
  open: boolean
  onClose: () => void
  /** Nome acessivel do dialogo; tambem exibido no cabecalho. */
  title: string
  children: ReactNode
  /** Oculta o cabecalho visivel (titulo/X); `title` segue como aria-label. */
  hideHeader?: boolean
  /**
   * "sheet" (padrao): folha inferior no mobile, modal centralizado no desktop (md+).
   * "center": sempre centralizado (usado pelo ConfirmDialog).
   */
  placement?: "sheet" | "center"
  className?: string
}

const FOCUSABLE =
  "a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex='-1'])"

/**
 * Folha inferior (mobile) que vira modal centralizado no desktop. Mesma
 * mecanica do Modal (portal, Esc, clique fora) mais armadilha de foco e
 * restauracao do foco ao fechar. Responsividade 100% por classes (md:).
 */
export function BottomSheet({
  open,
  onClose,
  title,
  children,
  hideHeader = false,
  placement = "sheet",
  className,
}: BottomSheetProps) {
  const panelRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return

    const previouslyFocused = document.activeElement as HTMLElement | null
    const panel = panelRef.current
    panel?.querySelector<HTMLElement>(FOCUSABLE)?.focus()
    if (panel && !panel.contains(document.activeElement)) panel.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose()
        return
      }
      if (event.key !== "Tab" || !panel) return

      const items = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE))
      if (items.length === 0) {
        event.preventDefault()
        return
      }
      const first = items[0]
      const last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener("keydown", handleKeyDown)
    return () => {
      document.removeEventListener("keydown", handleKeyDown)
      previouslyFocused?.focus()
    }
  }, [open, onClose])

  if (!open) return null

  const isSheet = placement === "sheet"

  return createPortal(
    <div
      className={cn(
        "fixed inset-0 z-50 flex justify-center bg-background/80",
        isSheet ? "items-end md:items-center md:px-4" : "items-center px-4",
      )}
      role="presentation"
      onClick={onClose}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
        className={cn(
          "flex w-full max-w-[480px] flex-col gap-4 border border-border bg-card p-[22px] outline-none",
          "max-h-[calc(100vh-2rem)] overflow-y-auto",
          isSheet ? "rounded-t-2xl border-b-0 md:rounded-2xl md:border-b" : "rounded-2xl",
          className,
        )}
      >
        {hideHeader ? null : (
          <div className="flex items-center justify-between">
            <h2 className="text-[19px] font-medium text-text-primary">{title}</h2>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              onClick={onClose}
              aria-label="Fechar"
            >
              <X className="size-4" aria-hidden="true" />
            </Button>
          </div>
        )}
        {children}
      </div>
    </div>,
    document.body,
  )
}
