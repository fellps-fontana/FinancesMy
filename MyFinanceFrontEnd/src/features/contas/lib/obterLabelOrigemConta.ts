import type { OrigemConta } from "@/features/contas/types"

// Rotulo do badge de origem (identidade-visual.md: "manual -> neutro"; ver
// tambem regra-de-negocio.md item 1). "OFX" e o rotulo do mockup 03 Contas
// para conta Open Finance - v1 opera SO com MANUAL (nenhum dado real chega
// como OpenFinance ainda), mas o mapeamento fica pronto para quando a
// integracao entrar em v2, sem precisar mexer neste componente de novo.
const LABEL_POR_ORIGEM: Record<OrigemConta, string> = {
  Manual: "Manual",
  OpenFinance: "OFX",
}

export function obterLabelOrigemConta(origem: OrigemConta): string {
  return LABEL_POR_ORIGEM[origem]
}
