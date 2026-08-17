import { useEffect, type ReactNode } from "react"
import { X } from "lucide-react"
import { Button } from "@/shared/ui/button"
import { cn } from "@/shared/lib/utils"

type ModalSize = "sm" | "md" | "lg"

const SIZE_CLASS: Record<ModalSize, string> = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
}

type ModalProps = {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  size?: ModalSize
  className?: string
}

/**
 * Shell de overlay/dialog reutilizavel, extraido do padrao ja duplicado em
 * PagarFaturaModal (features/cartao), ModalNovoAtivo (features/investimentos)
 * e FormNovaConta (features/contas): fundo fixed inset-0 com backdrop, card
 * centralizado com role="dialog", header com titulo + botao fechar (X).
 *
 * Diferenca estrutural deliberada: nos 3 modais atuais a propria tag <form>
 * carregava role="dialog" + estilo de card. Aqui o Modal e agnostico de
 * form - so cuida da mecanica do dialog. O consumidor coloca seu
 * <form onSubmit=...> dentro de children, com campos e rodape de botoes.
 */
export function Modal({ open, onClose, title, children, size = "sm", className }: ModalProps) {
  useEffect(() => {
    if (!open) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose()
      }
    }

    document.addEventListener("keydown", handleKeyDown)
    return () => document.removeEventListener("keydown", handleKeyDown)
  }, [open, onClose])

  if (!open) {
    return null
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4"
      role="presentation"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(event) => event.stopPropagation()}
        className={cn(
          "flex w-full flex-col gap-4 rounded-xl border border-border bg-card px-5 py-5",
          "max-h-[calc(100vh-2rem)] overflow-y-auto",
          SIZE_CLASS[size],
          className,
        )}
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[19px] font-medium text-text-primary">{title}</h2>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            onClick={onClose}
            aria-label="Fechar formulario"
          >
            <X className="size-4" aria-hidden="true" />
          </Button>
        </div>

        {children}
      </div>
    </div>
  )
}
