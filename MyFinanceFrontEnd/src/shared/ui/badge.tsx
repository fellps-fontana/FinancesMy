import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/shared/lib/utils"

/*
 * Badge de status/origem (identidade-visual.md): fundo neutro (bg-muted)
 * com cor semantica no texto; raio 5px. Cor carrega significado de dominio.
 */
const badgeVariants = cva(
  "inline-flex shrink-0 items-center justify-center rounded-badge px-2 py-0.5 text-[11px] leading-4 font-normal whitespace-nowrap",
  {
    variants: {
      variant: {
        pago: "bg-muted text-positivo",
        pendente: "bg-muted text-alerta",
        atrasado: "bg-muted text-negativo",
        manual: "bg-muted text-muted-foreground",
        sugerido: "bg-muted text-primary",
        antecipada: "border-[0.5px] border-primary px-1.5 py-px text-accent-soft",
        parcela: "bg-accent-deep text-accent-soft",
      },
    },
    defaultVariants: { variant: "manual" },
  }
)

type BadgeVariant = NonNullable<VariantProps<typeof badgeVariants>["variant"]>

function Badge({
  className,
  variant,
  ...props
}: React.ComponentProps<"span"> & { variant?: BadgeVariant }) {
  return (
    <span
      data-slot="badge"
      data-variant={variant ?? "manual"}
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  )
}

export { Badge, badgeVariants }
export type { BadgeVariant }
