import { obterLabelOrigemConta } from "@/features/contas/lib/obterLabelOrigemConta"
import type { OrigemConta } from "@/features/contas/types"

type ContaBadgeOrigemProps = {
  origem: OrigemConta
}

// Badge de origem (regra-de-negocio.md item 1; identidade-visual.md:
// "manual -> neutro"). Manual e OpenFinance/OFX usam o MESMO par neutro
// (bg-muted + text-text-muted) - o documento so define cor propria para
// pago/pendente/sugerido, nao para diferenciar origem entre si (o mockup 03
// Contas tambem usa o mesmo estilo visual para os dois rotulos "OFX" e
// "Manual"). Mesma classe ja usada por StatusFaturaBadge/ContaFixaItem para
// o estado neutro.
export function ContaBadgeOrigem({ origem }: ContaBadgeOrigemProps) {
  return (
    <span className="inline-flex items-center rounded-[5px] bg-muted px-2 py-0.5 text-[11px] font-medium text-text-muted">
      {obterLabelOrigemConta(origem)}
    </span>
  )
}
