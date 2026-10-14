import * as React from "react"

import { cn } from "@/shared/lib/utils"

type PageHeaderProps = Omit<React.ComponentProps<"div">, "title"> & {
  title: React.ReactNode
  /** Slot de acao (ex: botao primario) alinhado a direita. */
  action?: React.ReactNode
}

function PageHeader({ className, title, action, ...props }: PageHeaderProps) {
  return (
    <div
      data-slot="page-header"
      className={cn("flex items-center justify-between gap-3", className)}
      {...props}
    >
      <h1 className="text-[19px] leading-snug font-medium text-text-primary">
        {title}
      </h1>
      {action ? <div className="flex shrink-0 items-center gap-2">{action}</div> : null}
    </div>
  )
}

export { PageHeader }
export type { PageHeaderProps }
