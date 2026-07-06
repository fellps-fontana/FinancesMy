import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"

type TotalInvestidoResumoProps = {
  carregando: boolean
  totalInvestido: number | undefined
}

// Componente de apresentacao (burro): so recebe dado pronto e exibe. Nenhum
// calculo de formatacao mora aqui - vem de formatarMoeda.
export function TotalInvestidoResumo({ carregando, totalInvestido }: TotalInvestidoResumoProps) {
  return (
    <section className="flex flex-col gap-1 rounded-xl border border-border bg-card px-4 py-4">
      <span className="text-[13px] text-muted-foreground">Total investido</span>
      <span className="text-[28px] font-medium text-foreground">
        {carregando ? "Carregando..." : formatarMoeda(totalInvestido ?? 0)}
      </span>
    </section>
  )
}
