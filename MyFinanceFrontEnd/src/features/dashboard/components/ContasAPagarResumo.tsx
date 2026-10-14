import { Link } from "react-router-dom"
import { Badge } from "@/shared/ui/badge"
import { Card } from "@/shared/ui/card"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"
import { useContaCartaoAtual } from "@/features/cartao/hooks/useContaCartaoAtual"
import { useFaturas } from "@/features/cartao/hooks/useFaturas"
import type { FaturaResponse } from "@/features/cartao/types"
import { useFluxoCaixaTodasContas } from "@/features/lancamentos/hooks/useFluxoCaixaTodasContas"
import type { FluxoCaixaItem } from "@/features/lancamentos/types"

const QUANTIDADE_EXIBIDA = 3
const MS_POR_DIA = 24 * 60 * 60 * 1000

export type ItemAPagar = {
  chave: string
  titulo: string
  valor: number
  // yyyy-MM-dd (DateOnly do backend)
  vencimento: string
}

function paraDataLocal(dataIso: string): Date {
  const [ano, mes, dia] = dataIso.split("-").map(Number)
  return new Date(ano, mes - 1, dia)
}

// Dias entre hoje e o vencimento (negativo = atrasada). Parse manual evita o
// deslocamento de fuso de `new Date(iso)` (mesmo cuidado de formatarData.ts).
export function diasAteVencimento(vencimento: string, hoje: Date): number {
  const inicioHoje = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate())
  return Math.round((paraDataLocal(vencimento).getTime() - inicioHoje.getTime()) / MS_POR_DIA)
}

export function descreverVencimento(dias: number): string {
  if (dias < 0) return dias === -1 ? "Venceu ontem" : `Venceu ha ${-dias} dias`
  if (dias === 0) return "Vence hoje"
  if (dias === 1) return "Vence amanha"
  return `Vence em ${dias} dias`
}

// Conta a pagar nasce como lancamento PENDENTE (regra-de-negocio.md item 5);
// so DEBIT/SAIDA entra (item 2 CRITICA - decide por `tipo`/`classificacao`,
// nunca pelo sinal de `valor`). Transferencia nao e conta a pagar (item 3).
export function selecionarPendentesAPagar(fluxo: FluxoCaixaItem[]): ItemAPagar[] {
  const itens: ItemAPagar[] = []
  for (const item of fluxo) {
    if (item.tipoItem !== "LANCAMENTO") continue
    const { lancamento } = item
    if (
      lancamento.status === "PENDENTE" &&
      lancamento.tipo === "DEBIT" &&
      lancamento.classificacao === "SAIDA"
    ) {
      itens.push({
        chave: lancamento.id,
        titulo: lancamento.descricao ?? "Sem descricao",
        valor: lancamento.valor,
        vencimento: lancamento.data,
      })
    }
  }
  return itens
}

// Cartao entra como UMA conta a pagar = fatura ABERTA ou FECHADA com saldo
// pendente (itens 9 e 12). Usa o `valorPendente` ja calculado pelo backend;
// FUTURA (ciclo nao iniciado), PAGA ou sem pendencia ficam de fora. Escolhe a
// de vencimento mais proximo.
export function selecionarFaturaAPagar(
  faturas: FaturaResponse[],
  nomeCartao: string,
): ItemAPagar | null {
  const emAberto = faturas
    .filter(
      (fatura) => (fatura.status === "ABERTA" || fatura.status === "FECHADA") && fatura.valorPendente > 0,
    )
    .sort((a, b) => a.dataVencimento.localeCompare(b.dataVencimento))[0]

  if (emAberto === undefined) return null
  return {
    chave: `fatura-${emAberto.id}`,
    titulo: nomeCartao,
    valor: emAberto.valorPendente,
    vencimento: emAberto.dataVencimento,
  }
}

type ContasAPagarResumoProps = {
  className?: string
}

/**
 * Card "Contas a pagar" do dashboard desktop (mockup 02): proximos
 * vencimentos entre lancamentos PENDENTE de saida e a fatura em aberto do
 * cartao. Clique leva a /conciliacao. Valores vem prontos do backend.
 * Limitacao: considera so o cartao atual (primeiro) - nao ha hook agregado
 * de faturas de todos os cartoes.
 */
export function ContasAPagarResumo({ className }: ContasAPagarResumoProps) {
  const fluxo = useFluxoCaixaTodasContas()
  const { contaCartaoAtual } = useContaCartaoAtual(null)
  const faturas = useFaturas(contaCartaoAtual?.id ?? null)

  const hoje = new Date()
  const fatura =
    contaCartaoAtual !== null && faturas.data !== undefined
      ? selecionarFaturaAPagar(faturas.data, contaCartaoAtual.nome)
      : null
  const itens = [...selecionarPendentesAPagar(fluxo.data ?? []), ...(fatura ? [fatura] : [])]
    .sort((a, b) => a.vencimento.localeCompare(b.vencimento))
    .slice(0, QUANTIDADE_EXIBIDA)

  return (
    <Card className={cn("rounded-2xl py-5 [--card-spacing:--spacing(5.5)]", className)}>
      <Link
        to="/conciliacao"
        className="flex flex-col gap-3.5 rounded-2xl px-(--card-spacing) transition-colors hover:bg-muted/40 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        <span className="text-[15px] text-text-primary">Contas a pagar</span>

        {fluxo.isLoading && <p className="text-sm text-text-muted">Carregando contas...</p>}
        {fluxo.isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar as contas a pagar.</p>
        )}
        {!fluxo.isLoading && !fluxo.isError && itens.length === 0 && (
          <p className="text-sm text-text-muted">Nenhuma conta a pagar pendente.</p>
        )}

        {itens.length > 0 && (
          <ul className="flex flex-col gap-3">
            {itens.map((item) => {
              const dias = diasAteVencimento(item.vencimento, hoje)
              return (
                <li key={item.chave} className="flex items-center justify-between gap-3">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <span className="truncate text-sm text-text-body">{item.titulo}</span>
                    <span className="text-[12px] text-text-faint">{descreverVencimento(dias)}</span>
                  </div>
                  <div className="flex shrink-0 items-center gap-2">
                    {dias < 0 && <Badge variant="atrasado">Atrasado</Badge>}
                    <span className="text-sm text-text-primary">{formatarMoeda(item.valor)}</span>
                  </div>
                </li>
              )
            })}
          </ul>
        )}
      </Link>
    </Card>
  )
}
