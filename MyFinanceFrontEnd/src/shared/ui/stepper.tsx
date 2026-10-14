import { Minus, Plus } from "lucide-react"
import { cn } from "@/shared/lib/utils"

type StepperProps = {
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  step?: number
  /** Nome acessivel (ex: "Quantidade de parcelas"). */
  ariaLabel: string
  disabled?: boolean
  className?: string
}

/** Controle - N + com limites. O valor e sempre mantido dentro de [min, max]. */
export function Stepper({
  value,
  onChange,
  min = 1,
  max = Number.MAX_SAFE_INTEGER,
  step = 1,
  ariaLabel,
  disabled,
  className,
}: StepperProps) {
  const clamp = (n: number) => Math.min(max, Math.max(min, n))
  const buttonClass = cn(
    "flex size-9 items-center justify-center rounded-[10px] border border-border bg-accent text-text-body outline-none transition-colors",
    "hover:bg-accent/70 focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
  )

  return (
    <div
      role="group"
      aria-label={ariaLabel}
      className={cn("inline-flex items-center gap-2", className)}
    >
      <button
        type="button"
        aria-label="Diminuir"
        disabled={disabled || value <= min}
        onClick={() => onChange(clamp(value - step))}
        className={buttonClass}
      >
        <Minus className="size-4" aria-hidden="true" />
      </button>
      <span
        aria-live="polite"
        className="min-w-8 text-center text-sm font-medium tabular-nums text-text-primary"
      >
        {value}
      </span>
      <button
        type="button"
        aria-label="Aumentar"
        disabled={disabled || value >= max}
        onClick={() => onChange(clamp(value + step))}
        className={buttonClass}
      >
        <Plus className="size-4" aria-hidden="true" />
      </button>
    </div>
  )
}
