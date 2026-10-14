import * as React from "react"

import { cn } from "@/shared/lib/utils"

/** Quadrado arredondado 34px (accent-deep) com icone accent-soft dentro. */
function IconTile({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="icon-tile"
      className={cn(
        "flex size-[34px] shrink-0 items-center justify-center rounded-[10px] bg-accent-deep text-accent-soft [&_svg:not([class*='size-'])]:size-[18px]",
        className
      )}
      {...props}
    />
  )
}

export { IconTile }
