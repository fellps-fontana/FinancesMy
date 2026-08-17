import type { FluxoCaixaItem, TipoLancamento } from "../types"

// Recorte de mes calendario (regra-de-negocio.md item 9) aplicado client-side,
// ja que o endpoint de fluxo-caixa nao aceita periodo como query param. `mes`
// e 1-indexed (Janeiro = 1), mesma convencao usada no resto do dominio (ver
// `mesAtual` em DashboardPage.tsx / ComparativoLimiteGastoPage.tsx).
// `data` chega como DateOnly do backend (string "yyyy-MM-dd") - parse manual
// dos componentes (em vez de `new Date(iso)`) evita o deslocamento de fuso
// que `Date` aplicaria ao interpretar a string como UTC meia-noite, mesmo
// cuidado ja tomado em `formatarData.ts`. Opera sobre FluxoCaixaItem (uniao
// LANCAMENTO/TRANSFERENCIA, types.ts) - `data` vive no nivel superior da
// uniao nos dois casos, entao o recorte de mes nao precisa checar `tipoItem`.
export function filtrarLancamentosDoMes(
  itens: FluxoCaixaItem[],
  ano: number,
  mes: number,
): FluxoCaixaItem[] {
  return itens.filter((item) => {
    const [anoItem, mesItem] = item.data.split("-").map(Number)
    return anoItem === ano && mesItem === mes
  })
}

// ---------------------------------------------------------------------------
// Classificacao (chips + resumo) - decide o que conta como entrada/saida real.
// ---------------------------------------------------------------------------

// So LANCAMENTO com classificacao ENTRADA/SAIDA conta como movimento real
// (regra-de-negocio.md itens 2 CRITICA, 3 e 12). TRANSFERENCIA (linha inteira
// da uniao - transferencia comum, pagamento de fatura ou emprestimo) NUNCA
// conta, mesma regra que ja valia para lancamento.classificacao ===
// "TRANSFERENCIA" antes do endpoint agregado existir - agora a exclusao
// acontece um nivel acima, por `tipoItem`, e COMPETENCIA_CARTAO tambem
// continua fora (item 12: compra de cartao nao entra no fluxo de caixa).
export function deveContarComoEntradaOuSaida(item: FluxoCaixaItem): item is Extract<
  FluxoCaixaItem,
  { tipoItem: "LANCAMENTO" }
> {
  return (
    item.tipoItem === "LANCAMENTO" &&
    (item.lancamento.classificacao === "ENTRADA" || item.lancamento.classificacao === "SAIDA")
  )
}

// ---------------------------------------------------------------------------
// Filtro por tipo (chip Todos/Entradas/Saidas do mockup "04 Lancamentos").
// ---------------------------------------------------------------------------

export type FiltroTipoLancamento = "TODOS" | "ENTRADAS" | "SAIDAS"

// Filtra SEMPRE por `tipo` (DEBIT/CREDIT) do LANCAMENTO, nunca pelo sinal cru
// de `valor` (regra-de-negocio.md item 2, CRITICA). "Entradas" = CREDIT,
// "Saidas" = DEBIT - mesmo mapeamento ja usado em LancamentoItem/
// FormLancamento. "TODOS" mantem TAMBEM as linhas de TRANSFERENCIA na lista
// (nao esconde a transferencia, so nao soma no resumo - ver
// deveContarComoEntradaOuSaida acima); "ENTRADAS"/"SAIDAS" excluem toda
// TRANSFERENCIA (transferencia/pagamento de fatura/emprestimo nunca sao
// entrada nem saida real, itens 2 CRITICA, 3, 12 e 13).
export function filtrarPorTipo(itens: FluxoCaixaItem[], filtro: FiltroTipoLancamento): FluxoCaixaItem[] {
  if (filtro === "TODOS") {
    return itens
  }

  const tipoAlvo: TipoLancamento = filtro === "ENTRADAS" ? "CREDIT" : "DEBIT"
  return itens.filter((item) => deveContarComoEntradaOuSaida(item) && item.lancamento.tipo === tipoAlvo)
}

// ---------------------------------------------------------------------------
// Resumo do periodo (cards Entradas/Saidas/Saldo do mockup).
// ---------------------------------------------------------------------------

export type ResumoLancamentos = {
  totalEntradas: number
  totalSaidas: number
  saldo: number
}

// Soma por `tipo` (regra-de-negocio.md item 2, CRITICA) - nunca por sinal de
// `valor`. Antes de somar, exige deveContarComoEntradaOuSaida: TRANSFERENCIA
// inteira (transferencia comum, pagamento de fatura de cartao ou emprestimo -
// itens 3, 12 e 13) nunca e gasto nem receita, fica de fora do resumo. Funcao
// pura e testavel: nenhum calculo de dominio mora no componente
// (clean-code.md, "Organizacao (React)"), so aqui.
export function calcularResumoLancamentos(itens: FluxoCaixaItem[]): ResumoLancamentos {
  let totalEntradas = 0
  let totalSaidas = 0

  for (const item of itens) {
    if (!deveContarComoEntradaOuSaida(item)) {
      continue
    }

    if (item.lancamento.tipo === "CREDIT") {
      totalEntradas += item.lancamento.valor
    } else {
      totalSaidas += item.lancamento.valor
    }
  }

  return { totalEntradas, totalSaidas, saldo: totalEntradas - totalSaidas }
}

// ---------------------------------------------------------------------------
// Agrupamento por data (lista do mockup: "Hoje, 5 de julho" / "Ontem, 4 de
// julho" / "2 de julho").
// ---------------------------------------------------------------------------

export type GrupoLancamentosPorData = {
  data: string
  rotulo: string
  itens: FluxoCaixaItem[]
}

const formatadorDiaMes = new Intl.DateTimeFormat("pt-BR", { day: "numeric", month: "long" })

// Parse manual dos componentes de "yyyy-MM-dd" (mesmo cuidado de
// filtrarLancamentosDoMes acima e de cartao/lib/formatarData.ts) - evita o
// deslocamento de fuso de `new Date(iso)`.
function paraDataLocal(dataIso: string): Date {
  const [ano, mes, dia] = dataIso.split("-").map(Number)
  return new Date(ano, mes - 1, dia)
}

function mesmaData(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
}

// Rotulo relativo (Hoje/Ontem) so pra exibicao - `hoje` e parametro (default
// `new Date()`) pra manter a funcao pura e testavel sem depender do relogio
// do sistema em teste.
export function rotularDataLancamento(dataIso: string, hoje: Date = new Date()): string {
  const dataLancamento = paraDataLocal(dataIso)
  const diaMes = formatadorDiaMes.format(dataLancamento)

  if (mesmaData(dataLancamento, hoje)) {
    return `Hoje, ${diaMes}`
  }

  const ontem = new Date(hoje)
  ontem.setDate(ontem.getDate() - 1)
  if (mesmaData(dataLancamento, ontem)) {
    return `Ontem, ${diaMes}`
  }

  return diaMes
}

// Agrupa por `data` (uma linha logica por dia, mais recente primeiro - mesma
// ordem do mockup). Nao reordena os itens dentro do mesmo dia (mantem a ordem
// que a API ja devolveu) - LANCAMENTO e TRANSFERENCIA convivem no mesmo grupo,
// quem decide o componente de renderizacao e o pai (LancamentosPage.tsx), por
// `tipoItem`.
export function agruparLancamentosPorData(
  itens: FluxoCaixaItem[],
  hoje: Date = new Date(),
): GrupoLancamentosPorData[] {
  const porData = new Map<string, FluxoCaixaItem[]>()

  for (const item of itens) {
    const grupo = porData.get(item.data)
    if (grupo) {
      grupo.push(item)
    } else {
      porData.set(item.data, [item])
    }
  }

  return Array.from(porData.entries())
    .sort(([dataA], [dataB]) => (dataA < dataB ? 1 : dataA > dataB ? -1 : 0))
    .map(([data, grupoItens]) => ({ data, rotulo: rotularDataLancamento(data, hoje), itens: grupoItens }))
}

// ---------------------------------------------------------------------------
// Navegacao de mes (seta esquerda/direita do mockup, "Julho 2026").
// ---------------------------------------------------------------------------

function paraAnoMes(mesReferencia: string): { ano: number; mes: number } {
  const [ano, mes] = mesReferencia.split("-").map(Number)
  return { ano, mes }
}

function formatarMesReferenciaIso(ano: number, mes: number): string {
  return `${ano}-${String(mes).padStart(2, "0")}`
}

// Soma/subtrai meses a um "yyyy-MM" (delta negativo anda pro passado).
// `new Date(ano, mes)` normaliza estouro de mes/ano automaticamente (ex:
// mes 13 vira janeiro do ano seguinte), sem `if` de virada de ano manual.
export function somarMeses(mesReferencia: string, delta: number): string {
  const { ano, mes } = paraAnoMes(mesReferencia)
  const data = new Date(ano, mes - 1 + delta, 1)
  return formatarMesReferenciaIso(data.getFullYear(), data.getMonth() + 1)
}

const formatadorMesExtenso = new Intl.DateTimeFormat("pt-BR", { month: "long" })

// "yyyy-MM" -> "Julho 2026" (mockup "04 Lancamentos"). Nao reusa
// formatarMesReferencia (cartao/lib/formatarData.ts, que produz "Julho de
// 2026") porque esse arquivo esta fora do escopo de edicao desta tarefa -
// mesmo formato, texto ligeiramente diferente (sem o "de"), decisao tomada
// aqui pra bater com o mockup literalmente.
export function formatarRotuloMes(mesReferencia: string): string {
  const { ano, mes } = paraAnoMes(mesReferencia)
  const nomeMes = formatadorMesExtenso.format(new Date(ano, mes - 1, 1))
  return `${nomeMes.charAt(0).toUpperCase()}${nomeMes.slice(1)} ${ano}`
}
