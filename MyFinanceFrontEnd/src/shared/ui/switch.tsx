import { cn } from "@/shared/lib/utils"

type SwitchProps = {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  /** Rotulo principal exibido ao lado do controle. */
  label: string
  /** Texto auxiliar abaixo do rotulo. */
  description?: string
  disabled?: boolean
  id?: string
  className?: string
}

/**
 * Toggle on/off (role="switch"). A linha inteira e clicavel, como no mockup
 * (toggle Parcelar). Apresentacao pura: o estado vive no consumidor.
 */
export function Switch({
  checked,
  onCheckedChange,
  label,
  description,
  disabled,
  id,
  className,
}: SwitchProps) {
  return (
    <button
      type="button"
      id={id}
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onCheckedChange(!checked)}
      className={cn(
        "flex w-full items-center justify-between gap-3 rounded-lg text-left outline-none",
        "focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
        className,
      )}
    >
      <span className="flex min-w-0 flex-col">
        <span className="text-sm text-text-body">{label}</span>
        {description ? (
          <span className="mt-0.5 text-xs text-text-faint">{description}</span>
        ) : null}
      </span>
      <span
        aria-hidden="true"
        className={cn(
          "flex h-6 w-10 shrink-0 items-center rounded-full p-0.5 transition-colors",
          checked ? "justify-end bg-primary" : "justify-start bg-accent",
        )}
      >
        <span className="size-5 rounded-full bg-card shadow-sm" />
      </span>
    </button>
  )
}
