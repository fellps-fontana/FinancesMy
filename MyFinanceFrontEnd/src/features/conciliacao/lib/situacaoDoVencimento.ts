import type { FluxoCaixaItem } from "@/features/lancamentos/types"

export type SituacaoVencimento = "atrasado" | "pendente"

export type ResultadoSituacao = {
  situacao: SituacaoVencimento
  // Dias inteiros entre hoje e o vencimento (negativo = ja venceu).
  dias: number
  rotulo: string
}

const MS_POR_DIA = 24 * 60 * 60 * 1000

// `data` chega como DateOnly "yyyy-MM-dd"; parse manual em UTC evita o
// deslocamento de fuso de `new Date(iso)` (mesmo cuidado de filtrarPeriodo.ts).
function diasDesdeEpoch(ano: number, mes: number, dia: number): number {
  return Math.floor(Date.UTC(ano, mes - 1, dia) / MS_POR_DIA)
}

function plural(dias: number): string {
  return dias === 1 ? "1 dia" : `${dias} dias`
}

// Situacao de uma conta a pagar frente ao vencimento (regra-de-negocio.md
// item 5: conta a pagar nasce PENDENTE). Venceu antes de hoje = atrasado;
// hoje ou depois = ainda pendente.
export function situacaoDoVencimento(vencimento: string, hoje: Date = new Date()): ResultadoSituacao {
  const [ano, mes, dia] = vencimento.split("-").map(Number)
  const dias = diasDesdeEpoch(ano, mes, dia) - diasDesdeEpoch(hoje.getFullYear(), hoje.getMonth() + 1, hoje.getDate())

  if (dias < 0) return { situacao: "atrasado", dias, rotulo: `Venceu ha ${plural(-dias)}` }
  if (dias === 0) return { situacao: "pendente", dias, rotulo: "Vence hoje" }
  return { situacao: "pendente", dias, rotulo: `Vence em ${plural(dias)}` }
}

// A classificacao vem sempre do backend (item 2 CRITICA) - nunca do sinal.
// Lancamento avulso DEBIT, ainda PENDENTE e classificado SAIDA = conta a pagar
// que o usuario quita manualmente.
export function ehContaAPagar(item: FluxoCaixaItem): item is Extract<FluxoCaixaItem, { tipoItem: "LANCAMENTO" }> {
  return (
    item.tipoItem === "LANCAMENTO" &&
    item.lancamento.tipo === "DEBIT" &&
    item.lancamento.status === "PENDENTE" &&
    item.lancamento.classificacao === "SAIDA" &&
    !item.lancamento.oculto
  )
}

// Compra de cartao pendente (COMPETENCIA_CARTAO): nao se paga por lancamento,
// o pagamento e da fatura na tela /cartao (item 12).
export function ehCompraCartaoPendente(item: FluxoCaixaItem): item is Extract<FluxoCaixaItem, { tipoItem: "LANCAMENTO" }> {
  return (
    item.tipoItem === "LANCAMENTO" &&
    item.lancamento.tipo === "DEBIT" &&
    item.lancamento.status === "PENDENTE" &&
    item.lancamento.classificacao === "COMPETENCIA_CARTAO" &&
    !item.lancamento.oculto
  )
}

// Conciliado (v1): pagos do mes, saida real.
export function ehPagoDoMes(item: FluxoCaixaItem, ano: number, mes: number): item is Extract<FluxoCaixaItem, { tipoItem: "LANCAMENTO" }> {
  if (item.tipoItem !== "LANCAMENTO") return false
  const [anoItem, mesItem] = item.data.split("-").map(Number)
  return (
    item.lancamento.status === "PAGO" &&
    item.lancamento.classificacao === "SAIDA" &&
    !item.lancamento.oculto &&
    anoItem === ano &&
    mesItem === mes
  )
}

export type ResumoContasAbertas = { emAberto: number; atrasado: number }

// Em aberto = soma de tudo ainda nao pago; Atrasado = parcela vencida.
export function resumirContasAbertas(
  contas: ReadonlyArray<{ valor: number; vencimento: string | null }>,
  hoje: Date = new Date(),
): ResumoContasAbertas {
  return contas.reduce<ResumoContasAbertas>(
    (acc, conta) => {
      const atrasada = conta.vencimento !== null && situacaoDoVencimento(conta.vencimento, hoje).situacao === "atrasado"
      return { emAberto: acc.emAberto + conta.valor, atrasado: acc.atrasado + (atrasada ? conta.valor : 0) }
    },
    { emAberto: 0, atrasado: 0 },
  )
}
