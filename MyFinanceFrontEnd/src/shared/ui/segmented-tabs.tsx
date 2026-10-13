import { useRef, type KeyboardEvent } from "react"
import { cn } from "@/shared/lib/utils"

export type SegmentedTabOption<T extends string> = {
  value: T
  label: string
}

type SegmentedTabsProps<T extends string> = {
  options: ReadonlyArray<SegmentedTabOption<T>>
  value: T
  onChange: (value: T) => void
  /** Nome acessivel do grupo (ex: "Tipo de lancamento"). */
  ariaLabel: string
  className?: string
}

/**
 * Abas segmentadas (ex: Despesas / Receitas). role="tablist" com navegacao por
 * setas/Home/End e roving tabindex. Nao renderiza painel: o consumidor decide
 * o conteudo a partir de `value`.
 */
export function SegmentedTabs<T extends string>({
  options,
  value,
  onChange,
  ariaLabel,
  className,
}: SegmentedTabsProps<T>) {
  const refs = useRef<Array<HTMLButtonElement | null>>([])

  function handleKeyDown(event: KeyboardEvent, index: number) {
    const last = options.length - 1
    let next = index
    if (event.key === "ArrowRight") next = index === last ? 0 : index + 1
    else if (event.key === "ArrowLeft") next = index === 0 ? last : index - 1
    else if (event.key === "Home") next = 0
    else if (event.key === "End") next = last
    else return

    event.preventDefault()
    onChange(options[next].value)
    refs.current[next]?.focus()
  }

  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className={cn("flex gap-1 rounded-[10px] bg-accent p-1", className)}
    >
      {options.map((option, index) => {
        const selected = option.value === value
        return (
          <button
            key={option.value}
            ref={(el) => {
              refs.current[index] = el
            }}
            type="button"
            role="tab"
            aria-selected={selected}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(option.value)}
            onKeyDown={(event) => handleKeyDown(event, index)}
            className={cn(
              "flex-1 rounded-lg px-3 py-2 text-sm outline-none transition-colors",
              "focus-visible:ring-3 focus-visible:ring-ring/50",
              selected
                ? "bg-card font-medium text-text-primary"
                : "text-text-muted hover:text-text-body",
            )}
          >
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
