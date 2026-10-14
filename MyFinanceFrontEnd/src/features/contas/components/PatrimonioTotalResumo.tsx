import { formatarMoeda } from "@/shared/lib/formatarMoeda"

type PatrimonioTotalResumoProps = {
  carregando: boolean
  /** Falha ao carregar o total investido: o patrimonio nao pode ser exibido. */
  erro?: boolean
  patrimonioTotal: number
  quantidadeContas: number
}

// Apresentacao pura do card "Patrimonio total" (mockup s_contas): o total vem
// de lib/calcularPatrimonioTotal.ts (saldos Banco + total investido do
// backend); aqui so se formata.
export function PatrimonioTotalResumo({
  carregando,
  erro = false,
  patrimonioTotal,
  quantidadeContas,
}: PatrimonioTotalResumoProps) {
  return (
    <section className="flex items-center justify-between gap-4 rounded-2xl border-[0.5px] border-border bg-card p-5 md:p-[22px]">
      <div>
        <span className="text-[13px] text-text-muted">Patrimonio total</span>
        <div className="mt-1.5 text-[28px] leading-tight font-medium text-text-primary">
          {carregando ? "Carregando..." : erro ? "Indisponivel" : formatarMoeda(patrimonioTotal)}
        </div>
      </div>

      {erro && !carregando && (
        <span className="shrink-0 text-[13px] text-negativo">Nao foi possivel carregar o total investido.</span>
      )}

      {!carregando && !erro && (
        <span className="shrink-0 text-[13px] text-text-faint">
          {quantidadeContas} {quantidadeContas === 1 ? "conta" : "contas"}
        </span>
      )}
    </section>
  )
}
