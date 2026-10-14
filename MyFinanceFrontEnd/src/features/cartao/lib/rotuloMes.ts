import { formatarMesReferencia } from "@/shared/lib/datas"
import type { FaturaResponse } from "@/features/cartao/types"

// "2026-07-06" -> "Julho" (mes de vencimento, como as abas do mockup).
export function rotuloMes(fatura: FaturaResponse): string {
  return formatarMesReferencia(fatura.dataVencimento.slice(0, 7)).split(" ")[0]
}
