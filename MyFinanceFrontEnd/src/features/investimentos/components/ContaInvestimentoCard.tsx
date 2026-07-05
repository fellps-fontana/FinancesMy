import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import type { ContaResponse } from "@/features/investimentos/types"

type ContaInvestimentoCardProps = {
  conta: ContaResponse
}

// Componente de apresentacao (burro): recebe a conta pronta e exibe. Sem
// fetch, sem calculo - so leitura de dado ja resolvido pelo container.
export function ContaInvestimentoCard({ conta }: ContaInvestimentoCardProps) {
  if (conta.saldoManual === null) {
    // Mesmo estado invalido que o backend ja trata como grave em
    // ContaService.CalcularTotalInvestido (LogWarning) - nao mascarar em
    // silencio aqui so porque o fallback ?? 0 resolve a exibicao.
    console.warn(
      `Conta de investimento sem saldoManual - id=${conta.id} nome=${conta.nome}`,
    )
  }

  const saldo = conta.saldoManual ?? 0

  return (
    <Card>
      <CardContent className="flex items-center justify-between">
        <span className="text-sm font-medium text-card-foreground">{conta.nome}</span>
        <span className="text-sm font-medium text-card-foreground">{formatarMoeda(saldo)}</span>
      </CardContent>
    </Card>
  )
}
