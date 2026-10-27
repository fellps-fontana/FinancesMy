import type { CategoriaResponse } from "@/features/categorias/types"
import type {
  PlanoMensalResponse,
  SalvarPlanoMensalRequest,
  SublimiteResponse,
} from "@/features/planejamento-mensal/types"

// Estado de edicao: numeros ficam como texto para o usuario digitar "12," ou
// alternar entre R$ e % sem o campo sobrescrever o que esta sendo digitado.
export type LinhaSublimite = {
  categoriaId: string
  parentId: string | null
  nome: string
  icone: string | null
  categoriaArquivada: boolean
  percentual: string
  valor: string
  gastoRealizado: number
}

export type Rascunho = {
  rendaPrevista: string
  percentualInvestimento: string
  valorInvestimento: string
  sublimites: LinhaSublimite[]
}

export type ItemSublimitePreview = {
  categoriaId: string
  parentId: string | null
  nome: string
  icone: string | null
  categoriaArquivada: boolean
  percentualTexto: string
  valorTexto: string
  percentual: number
  valor: number
  temSublimite: boolean
  gastoRealizado: number
  fracaoUtilizada: number
  estourado: boolean
}

export type PlanoPreview = {
  ano: number
  mes: number
  somenteLeitura: boolean
  rendaPrevista: number
  fixas: number
  percentualInvestimento: number
  investimentoPlanejado: number
  investimento: number
  investimentoReduzido: boolean
  planoNaoFecha: boolean
  sobra: number
  naoAlocado: number
  gastoSemSublimite: number
  itens: ItemSublimitePreview[]
}

export type ExcessoPercentual = {
  parentId: string | null
  nomePai: string | null
  soma: number
}

export type OpcoesCopiaPlano = {
  copiarRenda: boolean
}

const PERCENTUAL_MAXIMO = 100
const CASAS_DECIMAIS = 100
// Teto tecnico da renda prevista (item 20): decimal(18,2) = ate 16 digitos
// inteiros. O valor exato no limite (9.999.999.999.999.999,99) nao e
// representavel em double; por isso a checagem e textual, nao numerica.
const RENDA_PREVISTA_MAX_DIGITOS_INTEIROS = 16
const DECIMAL_SIMPLES = /^\d+([.,]\d*)?$/

// Aceita "1234,5" e "1234.5". Vazio ou invalido -> null.
export function lerDecimal(texto: string): number | null {
  const normalizado = texto.trim().replace(",", ".")
  if (normalizado === "") return null
  const valor = Number(normalizado)
  return Number.isFinite(valor) ? valor : null
}

function rendaDentroDoTeto(texto: string): boolean {
  const normalizado = texto.trim()
  if (!DECIMAL_SIMPLES.test(normalizado)) return false
  const parteInteira = normalizado.split(/[.,]/)[0].replace(/^0+/, "")
  return parteInteira.length <= RENDA_PREVISTA_MAX_DIGITOS_INTEIROS
}

function arredondarDuasCasas(valor: number): number {
  if (!Number.isFinite(valor)) return 0
  const arredondado = Math.round((valor + Number.EPSILON) * CASAS_DECIMAIS) / CASAS_DECIMAIS
  return arredondado === 0 ? 0 : arredondado
}

export function formatarCampoDecimal(valor: number): string {
  const arredondado = arredondarDuasCasas(valor)
  if (arredondado === 0) return "0"
  return String(arredondado).replace(".", ",")
}

export function percentualParaReais(base: number, percentual: number): number {
  if (base <= 0 || percentual <= 0) return 0
  return (base * percentual) / PERCENTUAL_MAXIMO
}

export function reaisParaPercentual(base: number, valor: number): number {
  if (base <= 0 || valor <= 0) return 0
  return (valor / base) * PERCENTUAL_MAXIMO
}

function calcularSobra(renda: number, fixas: number, percentualInvestimento: number): number {
  const rendaValida = Math.max(0, renda)
  const pctValido = Math.max(0, percentualInvestimento)
  const investimentoPlanejado = (rendaValida * pctValido) / PERCENTUAL_MAXIMO
  const investimento = Math.max(0, Math.min(investimentoPlanejado, rendaValida - fixas))
  return Math.max(0, rendaValida - fixas - investimento)
}

function recalcularValoresSublimites(linhas: LinhaSublimite[], sobra: number): LinhaSublimite[] {
  const valoresPorPai = new Map<string, number>()

  for (const linha of linhas) {
    if (linha.parentId === null) {
      const pctPai = Math.max(0, lerDecimal(linha.percentual) ?? 0)
      valoresPorPai.set(linha.categoriaId, percentualParaReais(sobra, pctPai))
    }
  }

  return linhas.map((linha) => {
    if (linha.parentId === null) {
      const valorPai = valoresPorPai.get(linha.categoriaId) ?? 0
      return { ...linha, valor: formatarCampoDecimal(valorPai) }
    }
    const valorPai = valoresPorPai.get(linha.parentId) ?? 0
    const pctFilho = Math.max(0, lerDecimal(linha.percentual) ?? 0)
    const valorFilho = percentualParaReais(valorPai, pctFilho)
    return { ...linha, valor: formatarCampoDecimal(valorFilho) }
  })
}

function montarMapaGastos(plano: PlanoMensalResponse): Map<string, number> {
  const mapa = new Map<string, number>()
  for (const s of plano.sublimites) {
    mapa.set(s.categoriaId, s.gastoRealizado)
  }
  for (const g of plano.gastosPorCategoria ?? []) {
    mapa.set(g.categoriaId, g.gastoRealizado)
  }
  return mapa
}

function linhaDeSublimiteSalvo(
  salvo: SublimiteResponse,
  mapaGastos: Map<string, number>,
): LinhaSublimite {
  return {
    categoriaId: salvo.categoriaId,
    parentId: salvo.parentId,
    nome: salvo.nome,
    icone: salvo.icone ?? null,
    categoriaArquivada: salvo.categoriaArquivada,
    percentual: formatarCampoDecimal(salvo.percentual),
    valor: formatarCampoDecimal(salvo.valor),
    gastoRealizado: mapaGastos.get(salvo.categoriaId) ?? salvo.gastoRealizado,
  }
}

function obterSubcategoriasAtivas(
  pai: CategoriaResponse,
  todas: CategoriaResponse[],
): CategoriaResponse[] {
  const mapa = new Map<string, CategoriaResponse>()
  for (const sub of pai.subcategorias ?? []) {
    if (!sub.arquivada) mapa.set(sub.id, sub)
  }
  for (const cat of todas) {
    if (cat.parentId === pai.id && !cat.arquivada) {
      mapa.set(cat.id, cat)
    }
  }
  return Array.from(mapa.values()).sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"))
}

export function criarRascunho(
  plano: PlanoMensalResponse,
  categoriasDespesa?: CategoriaResponse[],
): Rascunho {
  const mapaGastos = montarMapaGastos(plano)
  const rendaPrevista = formatarCampoDecimal(plano.rendaPrevista)
  const percentualInvestimento = formatarCampoDecimal(plano.percentualInvestimento)
  const valorInvestimento = formatarCampoDecimal(plano.investimentoPlanejado)

  if (!categoriasDespesa || categoriasDespesa.length === 0) {
    return {
      rendaPrevista,
      percentualInvestimento,
      valorInvestimento,
      sublimites: plano.sublimites.map((s) => linhaDeSublimiteSalvo(s, mapaGastos)),
    }
  }

  const salvosPorId = new Map<string, SublimiteResponse>(
    plano.sublimites.map((s) => [s.categoriaId, s]),
  )
  const incluidas = new Set<string>()
  const linhas: LinhaSublimite[] = []

  const paisAtivos = categoriasDespesa
    .filter((c) => !c.parentId && !c.arquivada && c.tipo === "Despesa")
    .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"))

  for (const pai of paisAtivos) {
    const salvoPai = salvosPorId.get(pai.id)
    linhas.push({
      categoriaId: pai.id,
      parentId: null,
      nome: pai.nome,
      icone: pai.icone ?? salvoPai?.icone ?? null,
      categoriaArquivada: false,
      percentual: salvoPai ? formatarCampoDecimal(salvoPai.percentual) : "0",
      valor: salvoPai ? formatarCampoDecimal(salvoPai.valor) : "0",
      gastoRealizado: mapaGastos.get(pai.id) ?? salvoPai?.gastoRealizado ?? 0,
    })
    incluidas.add(pai.id)

    const subsAtivas = obterSubcategoriasAtivas(pai, categoriasDespesa)
    for (const sub of subsAtivas) {
      const salvoSub = salvosPorId.get(sub.id)
      linhas.push({
        categoriaId: sub.id,
        parentId: pai.id,
        nome: sub.nome,
        icone: sub.icone ?? salvoSub?.icone ?? null,
        categoriaArquivada: false,
        percentual: salvoSub ? formatarCampoDecimal(salvoSub.percentual) : "0",
        valor: salvoSub ? formatarCampoDecimal(salvoSub.valor) : "0",
        gastoRealizado: mapaGastos.get(sub.id) ?? salvoSub?.gastoRealizado ?? 0,
      })
      incluidas.add(sub.id)
    }

    for (const salvo of plano.sublimites) {
      if (salvo.parentId === pai.id && !incluidas.has(salvo.categoriaId)) {
        linhas.push(linhaDeSublimiteSalvo(salvo, mapaGastos))
        incluidas.add(salvo.categoriaId)
      }
    }
  }

  for (const salvo of plano.sublimites) {
    if (!incluidas.has(salvo.categoriaId)) {
      linhas.push(linhaDeSublimiteSalvo(salvo, mapaGastos))
      incluidas.add(salvo.categoriaId)
    }
  }

  return {
    rendaPrevista,
    percentualInvestimento,
    valorInvestimento,
    sublimites: linhas,
  }
}

// Aplica os percentuais de `planoOrigem` sobre o rascunho do mes destino,
// ignorando categorias arquivadas e recalculando os valores em R$ sobre a
// renda e as despesas fixas do mes destino (item 20).
export function aplicarCopiaDePlano(
  rascunhoDestino: Rascunho,
  planoOrigem: PlanoMensalResponse,
  fixasDestino: number,
  opcoes: OpcoesCopiaPlano = { copiarRenda: false },
): Rascunho {
  const rendaPrevista = opcoes.copiarRenda
    ? formatarCampoDecimal(planoOrigem.rendaPrevista)
    : rascunhoDestino.rendaPrevista
  const renda = Math.max(0, lerDecimal(rendaPrevista) ?? 0)
  const percentualInvestimento = formatarCampoDecimal(planoOrigem.percentualInvestimento)
  const pctInv = Math.max(0, lerDecimal(percentualInvestimento) ?? 0)
  const valorInvestimento = formatarCampoDecimal(percentualParaReais(renda, pctInv))
  const sobra = calcularSobra(renda, fixasDestino, pctInv)

  const percentuaisOrigemAtivos = new Map<string, number>()
  for (const s of planoOrigem.sublimites) {
    if (!s.categoriaArquivada && s.percentual > 0) {
      percentuaisOrigemAtivos.set(s.categoriaId, s.percentual)
    }
  }

  const linhasAtivas = rascunhoDestino.sublimites.filter((l) => !l.categoriaArquivada)
  const paisComSublimite = new Set<string>()

  for (const linha of linhasAtivas) {
    if (linha.parentId === null) {
      const pctPai = percentuaisOrigemAtivos.get(linha.categoriaId) ?? 0
      if (pctPai > 0) {
        paisComSublimite.add(linha.categoriaId)
      }
    }
  }

  const linhasComPercentuais = linhasAtivas.map((linha) => {
    if (linha.parentId === null) {
      const pct = percentuaisOrigemAtivos.get(linha.categoriaId) ?? 0
      return {
        ...linha,
        percentual: pct > 0 ? formatarCampoDecimal(pct) : "0",
      }
    }
    const paiTemSublimite = paisComSublimite.has(linha.parentId)
    const pct = paiTemSublimite ? (percentuaisOrigemAtivos.get(linha.categoriaId) ?? 0) : 0
    return {
      ...linha,
      percentual: pct > 0 ? formatarCampoDecimal(pct) : "0",
    }
  })

  return {
    rendaPrevista,
    percentualInvestimento,
    valorInvestimento,
    sublimites: mesclarPreservandoArquivadas(
      rascunhoDestino.sublimites,
      recalcularValoresSublimites(linhasComPercentuais, sobra),
    ),
  }
}

// Mantem as linhas arquivadas do destino intactas, na posicao original; as
// ativas vem do resultado recalculado (item 14: arquivada e intocavel).
function mesclarPreservandoArquivadas(
  originais: LinhaSublimite[],
  ativasRecalculadas: LinhaSublimite[],
): LinhaSublimite[] {
  const ativasPorId = new Map(ativasRecalculadas.map((l) => [l.categoriaId, l]))
  return originais.map((linha) =>
    linha.categoriaArquivada ? linha : (ativasPorId.get(linha.categoriaId) ?? linha),
  )
}

export function alterarRendaPrevista(
  rascunho: Rascunho,
  fixas: number,
  textoRenda: string,
): Rascunho {
  const renda = Math.max(0, lerDecimal(textoRenda) ?? 0)
  const pctInv = Math.max(0, lerDecimal(rascunho.percentualInvestimento) ?? 0)
  const valorInv = percentualParaReais(renda, pctInv)
  const sobra = calcularSobra(renda, fixas, pctInv)

  return {
    ...rascunho,
    rendaPrevista: textoRenda,
    valorInvestimento: formatarCampoDecimal(valorInv),
    sublimites: recalcularValoresSublimites(rascunho.sublimites, sobra),
  }
}

export function alterarPercentualInvestimento(
  rascunho: Rascunho,
  fixas: number,
  textoPercentual: string,
): Rascunho {
  const renda = Math.max(0, lerDecimal(rascunho.rendaPrevista) ?? 0)
  const pctInv = Math.max(0, lerDecimal(textoPercentual) ?? 0)
  const valorInv = percentualParaReais(renda, pctInv)
  const sobra = calcularSobra(renda, fixas, pctInv)

  return {
    ...rascunho,
    percentualInvestimento: textoPercentual,
    valorInvestimento: formatarCampoDecimal(valorInv),
    sublimites: recalcularValoresSublimites(rascunho.sublimites, sobra),
  }
}

export function alterarValorInvestimento(
  rascunho: Rascunho,
  fixas: number,
  textoValor: string,
): Rascunho {
  const renda = Math.max(0, lerDecimal(rascunho.rendaPrevista) ?? 0)
  const valorInv = Math.max(0, lerDecimal(textoValor) ?? 0)
  const pctInv = reaisParaPercentual(renda, valorInv)
  const pctInvTexto = formatarCampoDecimal(pctInv)
  const pctInvArredondado = Math.max(0, lerDecimal(pctInvTexto) ?? 0)
  const sobra = calcularSobra(renda, fixas, pctInvArredondado)

  return {
    ...rascunho,
    valorInvestimento: textoValor,
    percentualInvestimento: pctInvTexto,
    sublimites: recalcularValoresSublimites(rascunho.sublimites, sobra),
  }
}

export function alterarPercentualSublimite(
  rascunho: Rascunho,
  fixas: number,
  categoriaId: string,
  textoPercentual: string,
): Rascunho {
  const alvo = rascunho.sublimites.find((l) => l.categoriaId === categoriaId)
  if (!alvo) return rascunho

  const renda = Math.max(0, lerDecimal(rascunho.rendaPrevista) ?? 0)
  const pctInv = Math.max(0, lerDecimal(rascunho.percentualInvestimento) ?? 0)
  const sobra = calcularSobra(renda, fixas, pctInv)
  const pctNovo = Math.max(0, lerDecimal(textoPercentual) ?? 0)

  if (alvo.parentId === null) {
    const valorPai = percentualParaReais(sobra, pctNovo)
    return {
      ...rascunho,
      sublimites: rascunho.sublimites.map((linha) => {
        if (linha.categoriaId === categoriaId) {
          return {
            ...linha,
            percentual: textoPercentual,
            valor: formatarCampoDecimal(valorPai),
          }
        }
        if (linha.parentId === categoriaId) {
          // Sublimite de subcategoria arquivada e intocavel no rascunho (item 14).
          if (linha.categoriaArquivada) return linha
          if (pctNovo <= 0) {
            return { ...linha, percentual: "0", valor: "0" }
          }
          const pctSub = Math.max(0, lerDecimal(linha.percentual) ?? 0)
          return {
            ...linha,
            valor: formatarCampoDecimal(percentualParaReais(valorPai, pctSub)),
          }
        }
        return linha
      }),
    }
  }

  const pai = rascunho.sublimites.find((l) => l.categoriaId === alvo.parentId)
  const pctPai = pai ? Math.max(0, lerDecimal(pai.percentual) ?? 0) : 0
  const valorPai = percentualParaReais(sobra, pctPai)
  const valorSub = percentualParaReais(valorPai, pctNovo)

  return {
    ...rascunho,
    sublimites: rascunho.sublimites.map((linha) =>
      linha.categoriaId === categoriaId
        ? { ...linha, percentual: textoPercentual, valor: formatarCampoDecimal(valorSub) }
        : linha,
    ),
  }
}

export function alterarValorSublimite(
  rascunho: Rascunho,
  fixas: number,
  categoriaId: string,
  textoValor: string,
): Rascunho {
  const alvo = rascunho.sublimites.find((l) => l.categoriaId === categoriaId)
  if (!alvo) return rascunho

  const renda = Math.max(0, lerDecimal(rascunho.rendaPrevista) ?? 0)
  const pctInv = Math.max(0, lerDecimal(rascunho.percentualInvestimento) ?? 0)
  const sobra = calcularSobra(renda, fixas, pctInv)
  const valorNovo = Math.max(0, lerDecimal(textoValor) ?? 0)

  if (alvo.parentId === null) {
    const pctPai = reaisParaPercentual(sobra, valorNovo)
    const pctPaiTexto = formatarCampoDecimal(pctPai)
    const pctPaiArredondado = Math.max(0, lerDecimal(pctPaiTexto) ?? 0)
    const valorPaiBase = pctPaiArredondado > 0 ? valorNovo : 0

    return {
      ...rascunho,
      sublimites: rascunho.sublimites.map((linha) => {
        if (linha.categoriaId === categoriaId) {
          return {
            ...linha,
            valor: textoValor,
            percentual: pctPaiTexto,
          }
        }
        if (linha.parentId === categoriaId) {
          if (linha.categoriaArquivada) return linha
          if (pctPaiArredondado <= 0) {
            return { ...linha, percentual: "0", valor: "0" }
          }
          const pctSub = Math.max(0, lerDecimal(linha.percentual) ?? 0)
          return {
            ...linha,
            valor: formatarCampoDecimal(percentualParaReais(valorPaiBase, pctSub)),
          }
        }
        return linha
      }),
    }
  }

  const pai = rascunho.sublimites.find((l) => l.categoriaId === alvo.parentId)
  const pctPai = pai ? Math.max(0, lerDecimal(pai.percentual) ?? 0) : 0
  const valorPai = pai
    ? Math.max(0, lerDecimal(pai.valor) ?? percentualParaReais(sobra, pctPai))
    : 0
  const pctSub = reaisParaPercentual(valorPai, valorNovo)

  return {
    ...rascunho,
    sublimites: rascunho.sublimites.map((linha) =>
      linha.categoriaId === categoriaId
        ? { ...linha, valor: textoValor, percentual: formatarCampoDecimal(pctSub) }
        : linha,
    ),
  }
}

export function removerSublimite(rascunho: Rascunho, categoriaId: string): Rascunho {
  const alvo = rascunho.sublimites.find((l) => l.categoriaId === categoriaId)
  if (!alvo) return rascunho

  if (alvo.categoriaArquivada) {
    return {
      ...rascunho,
      sublimites: rascunho.sublimites.filter(
        (l) => l.categoriaId !== categoriaId && l.parentId !== categoriaId,
      ),
    }
  }

  return {
    ...rascunho,
    sublimites: rascunho.sublimites.map((linha) => {
      if (linha.categoriaArquivada) return linha
      if (linha.categoriaId === categoriaId || linha.parentId === categoriaId) {
        return { ...linha, percentual: "0", valor: "0" }
      }
      return linha
    }),
  }
}

export function calcularPlanoPreview(
  rascunho: Rascunho,
  planoBase: PlanoMensalResponse,
): PlanoPreview {
  const rendaPrevista = Math.max(0, lerDecimal(rascunho.rendaPrevista) ?? 0)
  const fixas = planoBase.fixas
  const percentualInvestimento = Math.max(0, lerDecimal(rascunho.percentualInvestimento) ?? 0)
  const investimentoPlanejado = (rendaPrevista * percentualInvestimento) / PERCENTUAL_MAXIMO
  const investimento = Math.max(0, Math.min(investimentoPlanejado, rendaPrevista - fixas))
  const investimentoReduzido = investimento < investimentoPlanejado
  const planoNaoFecha = rendaPrevista - fixas < 0
  const sobra = Math.max(0, rendaPrevista - fixas - investimento)

  const valoresPorPai = new Map<string, number>()
  const temSublimitePorPai = new Map<string, boolean>()

  for (const linha of rascunho.sublimites) {
    if (linha.parentId === null) {
      const pctPai = Math.max(0, lerDecimal(linha.percentual) ?? 0)
      const valorPai = percentualParaReais(sobra, pctPai)
      const temSublimitePai = pctPai > 0 || linha.categoriaArquivada
      valoresPorPai.set(linha.categoriaId, valorPai)
      temSublimitePorPai.set(linha.categoriaId, temSublimitePai)
    }
  }

  const itens: ItemSublimitePreview[] = rascunho.sublimites.map((linha) => {
    const percentual = Math.max(0, lerDecimal(linha.percentual) ?? 0)
    const ehPai = linha.parentId === null
    const valorPai = ehPai ? 0 : (valoresPorPai.get(linha.parentId ?? "") ?? 0)
    const paiTemSublimite = ehPai ? true : (temSublimitePorPai.get(linha.parentId ?? "") ?? false)
    const valor = ehPai
      ? (valoresPorPai.get(linha.categoriaId) ?? 0)
      : percentualParaReais(valorPai, percentual)
    const temSublimite = (percentual > 0 && paiTemSublimite) || linha.categoriaArquivada
    const fracaoUtilizada = temSublimite && valor > 0 ? linha.gastoRealizado / valor : 0
    const estourado = temSublimite && linha.gastoRealizado > valor

    return {
      categoriaId: linha.categoriaId,
      parentId: linha.parentId,
      nome: linha.nome,
      icone: linha.icone,
      categoriaArquivada: linha.categoriaArquivada,
      percentualTexto: linha.percentual,
      valorTexto: linha.valor,
      percentual,
      valor,
      temSublimite,
      gastoRealizado: linha.gastoRealizado,
      fracaoUtilizada,
      estourado,
    }
  })

  const somaSublimitesPrimeiroNivel = itens
    .filter((i) => i.parentId === null && i.temSublimite)
    .reduce((soma, i) => soma + i.valor, 0)

  const naoAlocado = sobra - somaSublimitesPrimeiroNivel

  const totalGastoVariavel =
    planoBase.gastoSemSublimite +
    planoBase.sublimites
      .filter((s) => s.parentId === null)
      .reduce((soma, s) => soma + s.gastoRealizado, 0)

  const somaGastoComSublimite = itens
    .filter((i) => i.parentId === null && i.temSublimite)
    .reduce((soma, i) => soma + i.gastoRealizado, 0)

  const gastoSemSublimite = Math.max(0, totalGastoVariavel - somaGastoComSublimite)

  return {
    ano: planoBase.ano,
    mes: planoBase.mes,
    somenteLeitura: planoBase.somenteLeitura,
    rendaPrevista,
    fixas,
    percentualInvestimento,
    investimentoPlanejado,
    investimento,
    investimentoReduzido,
    planoNaoFecha,
    sobra,
    naoAlocado,
    gastoSemSublimite,
    itens,
  }
}

// Dica visual e bloqueio de submit no cliente: parentId null = primeiro nivel.
export function somaPercentuais(
  linhas: Array<{ parentId: string | null; percentual: string | number }>,
  parentId: string | null,
): number {
  return linhas
    .filter((l) => l.parentId === parentId)
    .reduce((soma, l) => {
      const valor = typeof l.percentual === "number" ? l.percentual : (lerDecimal(l.percentual) ?? 0)
      return soma + Math.max(0, valor)
    }, 0)
}

export function excedeCem(soma: number): boolean {
  return arredondarDuasCasas(soma) > PERCENTUAL_MAXIMO
}

export function listarExcessosPercentuais(linhas: LinhaSublimite[]): ExcessoPercentual[] {
  const excessos: ExcessoPercentual[] = []
  const somaPrimeiroNivel = somaPercentuais(linhas, null)
  if (excedeCem(somaPrimeiroNivel)) {
    excessos.push({ parentId: null, nomePai: null, soma: somaPrimeiroNivel })
  }

  for (const linha of linhas) {
    if (linha.parentId === null) {
      const somaSub = somaPercentuais(linhas, linha.categoriaId)
      if (excedeCem(somaSub)) {
        excessos.push({ parentId: linha.categoriaId, nomePai: linha.nome, soma: somaSub })
      }
    }
  }

  return excessos
}

function percentualLinhaLegivel(linha: LinhaSublimite): boolean {
  if (!linha.categoriaArquivada && linha.percentual.trim() === "") return true
  const valor = lerDecimal(linha.percentual)
  return valor !== null && valor >= 0 && valor <= PERCENTUAL_MAXIMO
}

// Campos numericos legiveis e dentro da faixa basica (0..100 para percentuais, >= 0 para renda).
export function rascunhoLegivel(rascunho: Rascunho): boolean {
  const renda = lerDecimal(rascunho.rendaPrevista)
  const pctInv = lerDecimal(rascunho.percentualInvestimento)
  return (
    renda !== null &&
    renda >= 0 &&
    rendaDentroDoTeto(rascunho.rendaPrevista) &&
    pctInv !== null &&
    pctInv >= 0 &&
    pctInv <= PERCENTUAL_MAXIMO &&
    rascunho.sublimites.every(percentualLinhaLegivel)
  )
}

export function podeSalvarRascunho(rascunho: Rascunho): boolean {
  return rascunhoLegivel(rascunho) && listarExcessosPercentuais(rascunho.sublimites).length === 0
}

// Replace-all: envia apenas sublimites ativos com percentual > 0 e arquivados
// ainda nao removidos (para validacao do backend).
export function montarRequest(rascunho: Rascunho): SalvarPlanoMensalRequest | null {
  if (!podeSalvarRascunho(rascunho)) return null
  return {
    rendaPrevista: lerDecimal(rascunho.rendaPrevista) as number,
    percentualInvestimento: lerDecimal(rascunho.percentualInvestimento) as number,
    sublimites: rascunho.sublimites
      .filter((l) => l.categoriaArquivada || (lerDecimal(l.percentual) ?? 0) > 0)
      .map((l) => ({
        categoriaId: l.categoriaId,
        percentual: lerDecimal(l.percentual) ?? 0,
      })),
  }
}

function mesmoDecimal(a: number, b: number): boolean {
  return arredondarDuasCasas(a) === arredondarDuasCasas(b)
}

export function temAlteracoes(rascunho: Rascunho, plano: PlanoMensalResponse): boolean {
  if (!rascunhoLegivel(rascunho)) return true

  const renda = lerDecimal(rascunho.rendaPrevista)
  const pctInv = lerDecimal(rascunho.percentualInvestimento)
  if (renda === null || pctInv === null) return true
  if (!mesmoDecimal(renda, plano.rendaPrevista)) return true
  if (!mesmoDecimal(pctInv, plano.percentualInvestimento)) return true

  const salvosPorId = new Map<string, SublimiteResponse>(
    plano.sublimites.map((s) => [s.categoriaId, s]),
  )

  for (const salvo of plano.sublimites) {
    const linha = rascunho.sublimites.find((l) => l.categoriaId === salvo.categoriaId)
    if (salvo.categoriaArquivada) {
      if (!linha) return true
      const pct = lerDecimal(linha.percentual)
      if (pct === null || !mesmoDecimal(pct, salvo.percentual)) return true
    } else {
      const pct = linha ? (lerDecimal(linha.percentual) ?? 0) : 0
      if (!mesmoDecimal(pct, salvo.percentual)) return true
    }
  }

  for (const linha of rascunho.sublimites) {
    const pct = lerDecimal(linha.percentual) ?? 0
    if (!salvosPorId.has(linha.categoriaId) && pct > 0) return true
  }

  return false
}
