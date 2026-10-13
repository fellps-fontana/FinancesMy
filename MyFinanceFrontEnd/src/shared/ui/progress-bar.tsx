import * as React from "react"

import { cn } from "@/shared/lib/utils"

type ProgressBarProps = Omit<React.ComponentProps<"div">, "children"> & {
  /** Percentual ja calculado pelo chamador (0 a 100+); a barra so clampa o preenchimento visual. */
  percent: number
  /** Limite estourado: preenchimento em negativo em vez de accent. */
  exceeded?: boolean
}

function ProgressBar({
  className,
  percent,
  exceeded = false,
  ...props
}: ProgressBarProps) {
  const fill = Math.min(100, Math.max(0, percent))
  return (
    <div
      data-slot="progress-bar"
      data-state={exceeded ? "estourado" : "normal"}
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(fill)}
      className={cn("h-[5px] overflow-hidden rounded-[5px] bg-muted", className)}
      {...props}
    >
      <div
        className={cn("h-full", exceeded ? "bg-negativo" : "bg-primary")}
        style={{ width: `${fill}%` }}
      />
    </div>
  )
}

export { ProgressBar }
export type { ProgressBarProps }
