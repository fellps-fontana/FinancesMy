import { formatarMoeda } from "@/shared/lib/formatarMoeda"

type PatrimonioTotalResumoProps = {
  carregando: boolean
  patrimonioTotal: number
  quantidadeContas: number
}

// Componente de apresentacao (burro): recebe o total ja calculado (ver
// lib/calcularPatrimonioTotal.ts) e so formata para exibicao - nenhum
// calculo de dominio mora aqui (clean-code.md "Organizacao (React)"). Layout
// espelha o card "Patrimonio total" do mockup 03 Contas (mobile e desktop).
export function PatrimonioTotalResumo({
  carregando,
  patrimonioTotal,
  quantidadeContas,
}: PatrimonioTotalResumoProps) {
  return (
    <section className="flex items-center justify-between gap-4 rounded-xl border border-border bg-card px-5 py-5">
      <div className="flex flex-col gap-1.5">
        <span className="text-[13px] text-text-muted">Patrimônio total</span>
        <span className="text-[28px] font-medium text-text-primary">
          {carregando ? "Carregando..." : formatarMoeda(patrimonioTotal)}
        </span>
      </div>

      {!carregando && (
        <span className="text-[13px] text-text-faint">
          {quantidadeContas} {quantidadeContas === 1 ? "conta" : "contas"} cadastradas
        </span>
      )}
    </section>
  )
}
