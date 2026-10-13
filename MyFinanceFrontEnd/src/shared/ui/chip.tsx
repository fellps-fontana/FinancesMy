import * as React from "react"

import { cn } from "@/shared/lib/utils"

type ChipProps = Omit<React.ComponentProps<"button">, "type"> & {
  selected?: boolean
}

/** Pilula selecionavel (filtro). Selecionado: accent-deep/accent-soft. */
function Chip({ className, selected = false, ...props }: ChipProps) {
  return (
    <button
      type="button"
      data-slot="chip"
      aria-pressed={selected}
      className={cn(
        "inline-flex shrink-0 items-center justify-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-normal whitespace-nowrap transition-colors outline-none select-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50",
        selected
          ? "bg-accent-deep text-accent-soft"
          : "bg-muted text-muted-foreground hover:text-foreground",
        className
      )}
      {...props}
    />
  )
}

export { Chip }
export type { ChipProps }
