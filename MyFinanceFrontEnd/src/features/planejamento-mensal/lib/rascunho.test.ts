import { describe, expect, it } from "vitest"
import type { CategoriaResponse } from "@/features/categorias/types"
import type {
  PlanoMensalResponse,
  SublimiteResponse,
} from "@/features/planejamento-mensal/types"
import {
  aplicarCopiaDePlano,
  alterarPercentualInvestimento,
  alterarPercentualSublimite,
  alterarValorInvestimento,
  alterarValorSublimite,
  calcularPlanoPreview,
  criarRascunho,
  lerDecimal,
  listarExcessosPercentuais,
  montarRequest,
  percentualParaReais,
  reaisParaPercentual,
  removerSublimite,
  somaPercentuais,
  temAlteracoes,
  type ItemSublimitePreview,
  type LinhaSublimite,
  type PlanoPreview,
  type Rascunho,
} from "@/features/planejamento-mensal/lib/rascunho"

// ---------------------------------------------------------------------------
// Fabricas. Valores default: renda 5000, fixas 1000, investimento 10%
// (planejado 500), sobra 3500. Qualquer teste sobrescreve so o que importa.
// ---------------------------------------------------------------------------

function plano(over: Partial<PlanoMensalResponse> = {}): PlanoMensalResponse {
  return {
    ano: 2026,
    mes: 10,
    somenteLeitura: false,
    rendaPrevista: 5000,
    fixas: 1000,
    percentualInvestimento: 10,
    investimentoPlanejado: 500,
    investimento: 500,
    investimentoReduzido: false,
    planoNaoFecha: false,
    sobra: 3500,
    naoAlocado: 0,
    gastoSemSublimite: 0,
    sublimites: [],
    gastosPorCategoria: [],
    ...over,
  }
}

function sublimiteSalvo(
  over: Partial<SublimiteResponse> & Pick<SublimiteResponse, "categoriaId">,
): SublimiteResponse {
  return {
    parentId: null,
    nome: over.categoriaId,
    icone: null,
    categoriaArquivada: false,
    percentual: 0,
    valor: 0,
    gastoRealizado: 0,
    fracaoUtilizada: 0,
    estourado: false,
    ...over,
  }
}

function linha(
  over: Partial<LinhaSublimite> & Pick<LinhaSublimite, "categoriaId">,
): LinhaSublimite {
  return {
    parentId: null,
    nome: over.categoriaId,
    icone: null,
    categoriaArquivada: false,
    percentual: "0",
    valor: "0",
    gastoRealizado: 0,
    ...over,
  }
}

function pai(
  categoriaId: string,
  percentual: string,
  extras: Partial<LinhaSublimite> = {},
): LinhaSublimite {
  return linha({ categoriaId, percentual, ...extras })
}

function filho(
  categoriaId: string,
  parentId: string,
  percentual: string,
  extras: Partial<LinhaSublimite> = {},
): LinhaSublimite {
  return linha({ categoriaId, parentId, percentual, ...extras })
}

function rascunho(
  sublimites: LinhaSublimite[],
  over: Partial<Pick<Rascunho, "rendaPrevista" | "percentualInvestimento" | "valorInvestimento">> = {},
): Rascunho {
  return {
    rendaPrevista: "5000",
    percentualInvestimento: "10",
    valorInvestimento: "500",
    ...over,
    sublimites,
  }
}

function categoria(
  over: Partial<CategoriaResponse> & Pick<CategoriaResponse, "id" | "nome">,
): CategoriaResponse {
  return {
    tipo: "Despesa",
    subcategorias: [],
    arquivada: false,
    ...over,
  }
}

function itemDe(preview: PlanoPreview, categoriaId: string): ItemSublimitePreview {
  const achado = preview.itens.find((i) => i.categoriaId === categoriaId)
  if (!achado) throw new Error(`item ${categoriaId} ausente no preview`)
  return achado
}

// ---------------------------------------------------------------------------
// 1. calcularPlanoPreview - item 20 (calculo) e item 14 (estouro, arquivada)
// ---------------------------------------------------------------------------

describe("calcularPlanoPreview - item 20 (investimento, sobra, plano que nao fecha)", () => {
  it("caso feliz: investimento = renda x % e sobra = renda - fixas - investimento", () => {
    const preview = calcularPlanoPreview(rascunho([pai("alim", "20")]), plano())

    expect(preview.investimentoPlanejado).toBe(500)
    expect(preview.investimento).toBe(500)
    expect(preview.sobra).toBe(3500)
    expect(preview.investimentoReduzido).toBe(false)
    expect(preview.planoNaoFecha).toBe(false)
  })

  it("investimento reduzido quando nao cabe na renda menos fixas (sobra zera)", () => {
    const preview = calcularPlanoPreview(
      rascunho([], { rendaPrevista: "1200", percentualInvestimento: "50" }),
      plano({ fixas: 1000 }),
    )

    expect(preview.investimentoPlanejado).toBe(600)
    expect(preview.investimento).toBe(200)
    expect(preview.investimentoReduzido).toBe(true)
    expect(preview.sobra).toBe(0)
    expect(preview.planoNaoFecha).toBe(false)
  })

  it("plano nao fecha quando renda < fixas: investimento, sobra e sublimites zerados", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20")], { rendaPrevista: "800", percentualInvestimento: "10" }),
      plano({ fixas: 1000 }),
    )

    expect(preview.planoNaoFecha).toBe(true)
    expect(preview.investimento).toBe(0)
    expect(preview.sobra).toBe(0)
    expect(itemDe(preview, "alim").valor).toBe(0)
  })

  it("borda: renda igual as fixas NAO e plano que nao fecha, mas zera a sobra", () => {
    const preview = calcularPlanoPreview(
      rascunho([], { rendaPrevista: "1000", percentualInvestimento: "10" }),
      plano({ fixas: 1000 }),
    )

    expect(preview.planoNaoFecha).toBe(false)
    expect(preview.sobra).toBe(0)
    expect(preview.investimento).toBe(0)
    expect(preview.investimentoReduzido).toBe(true)
  })

  it("base zero (renda 0, fixas 0) produz zeros sem NaN nem divisao por zero", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20")], { rendaPrevista: "0", percentualInvestimento: "0" }),
      plano({ fixas: 0 }),
    )

    expect(preview.planoNaoFecha).toBe(false)
    expect(preview.investimento).toBe(0)
    expect(preview.investimentoReduzido).toBe(false)
    expect(preview.sobra).toBe(0)
    expect(itemDe(preview, "alim").valor).toBe(0)
    expect(itemDe(preview, "alim").fracaoUtilizada).toBe(0)
  })
})

describe("calcularPlanoPreview - item 20 (sublimites e naoAlocado)", () => {
  it("sublimite de primeiro nivel = sobra x percentual / 100", () => {
    const preview = calcularPlanoPreview(rascunho([pai("alim", "20")]), plano())

    expect(itemDe(preview, "alim").valor).toBe(700)
    expect(itemDe(preview, "alim").temSublimite).toBe(true)
  })

  it("subcategoria = sublimite do pai x percentual / 100", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20"), filho("rest", "alim", "50")]),
      plano(),
    )

    expect(itemDe(preview, "rest").valor).toBe(350)
    expect(itemDe(preview, "rest").temSublimite).toBe(true)
  })

  it("naoAlocado = sobra - soma dos sublimites de primeiro nivel (subcategoria nao entra)", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20"), pai("lazer", "30"), filho("rest", "alim", "50")]),
      plano(),
    )

    // sobra 3500 - (alim 700 + lazer 1050); subcategoria de 350 nao subtrai
    expect(preview.naoAlocado).toBe(1750)
  })

  it("subcategoria com pai em 0% nao tem sublimite (valor 0)", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "0"), filho("rest", "alim", "50")]),
      plano(),
    )

    expect(itemDe(preview, "rest").temSublimite).toBe(false)
    expect(itemDe(preview, "rest").valor).toBe(0)
  })

  it("pai com 0% nao tem sublimite e nao estoura, mesmo com gasto", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "0", { gastoRealizado: 100 })]),
      plano(),
    )

    expect(itemDe(preview, "alim").temSublimite).toBe(false)
    expect(itemDe(preview, "alim").estourado).toBe(false)
  })
})

describe("calcularPlanoPreview - item 14 (estouro e gasto sem sublimite)", () => {
  it("estourado quando gasto passa do valor do sublimite", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20", { gastoRealizado: 800 })]),
      plano(),
    )

    expect(itemDe(preview, "alim").estourado).toBe(true)
    expect(itemDe(preview, "alim").fracaoUtilizada).toBeCloseTo(800 / 700)
  })

  it("borda: gasto exatamente igual ao sublimite NAO estoura", () => {
    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20", { gastoRealizado: 700 })]),
      plano(),
    )

    expect(itemDe(preview, "alim").estourado).toBe(false)
  })

  it("gasto de categoria que perdeu o sublimite passa a contar em gasto_sem_sublimite", () => {
    const base = plano({
      gastoSemSublimite: 300,
      sublimites: [sublimiteSalvo({ categoriaId: "alim", percentual: 20, gastoRealizado: 200 })],
    })

    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "0", { gastoRealizado: 200 })]),
      base,
    )

    expect(preview.gastoSemSublimite).toBe(500)
  })

  it("gasto de categoria com sublimite NAO entra em gasto_sem_sublimite", () => {
    const base = plano({
      gastoSemSublimite: 300,
      sublimites: [sublimiteSalvo({ categoriaId: "alim", percentual: 20, gastoRealizado: 200 })],
    })

    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "20", { gastoRealizado: 200 })]),
      base,
    )

    expect(preview.gastoSemSublimite).toBe(300)
  })

  it("categoria arquivada com sublimite continua nos calculos (sublimite, naoAlocado, estouro)", () => {
    const base = plano({
      sublimites: [
        sublimiteSalvo({
          categoriaId: "transp",
          categoriaArquivada: true,
          percentual: 10,
          gastoRealizado: 400,
        }),
      ],
    })

    const preview = calcularPlanoPreview(
      rascunho([pai("transp", "10", { categoriaArquivada: true, gastoRealizado: 400 })]),
      base,
    )

    const item = itemDe(preview, "transp")
    expect(item.temSublimite).toBe(true)
    expect(item.valor).toBe(350)
    expect(item.estourado).toBe(true)
    expect(preview.naoAlocado).toBe(3150)
    expect(preview.gastoSemSublimite).toBe(0)
  })

  it("subcategoria arquivada com pai em 0% continua nos calculos com o percentual salvo (valor 0)", () => {
    const base = plano({
      sublimites: [
        sublimiteSalvo({ categoriaId: "alim", percentual: 20, valor: 700 }),
        sublimiteSalvo({
          categoriaId: "antigo",
          parentId: "alim",
          categoriaArquivada: true,
          percentual: 5,
          valor: 35,
          gastoRealizado: 60,
        }),
      ],
    })

    const preview = calcularPlanoPreview(
      rascunho([pai("alim", "0"), filho("antigo", "alim", "5", { categoriaArquivada: true, gastoRealizado: 60 })]),
      base,
    )

    const item = itemDe(preview, "antigo")
    expect(item.percentualTexto).toBe("5")
    expect(item.temSublimite).toBe(true)
    expect(item.valor).toBe(0)
    expect(item.estourado).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// 2. Conversao R$ <-> % e edicao do campo digitado
// ---------------------------------------------------------------------------

describe("conversao R$ <-> % (base zero e arredondamento)", () => {
  it("percentualParaReais calcula base x percentual / 100", () => {
    expect(percentualParaReais(1000, 20)).toBe(200)
  })

  it("percentualParaReais com base zero, base negativa ou percentual zero retorna 0", () => {
    expect(percentualParaReais(0, 20)).toBe(0)
    expect(percentualParaReais(-500, 20)).toBe(0)
    expect(percentualParaReais(1000, 0)).toBe(0)
  })

  it("reaisParaPercentual calcula valor / base x 100", () => {
    expect(reaisParaPercentual(3500, 700)).toBeCloseTo(20)
  })

  it("reaisParaPercentual com base zero ou valor zero retorna 0", () => {
    expect(reaisParaPercentual(0, 700)).toBe(0)
    expect(reaisParaPercentual(3500, 0)).toBe(0)
  })

  it("alterarPercentualInvestimento arredonda o R$ para 2 casas e preserva o % digitado", () => {
    const resultado = alterarPercentualInvestimento(
      rascunho([], { rendaPrevista: "1000", percentualInvestimento: "10" }),
      0,
      "33,333",
    )

    expect(resultado.percentualInvestimento).toBe("33,333")
    expect(resultado.valorInvestimento).toBe("333,33")
  })

  it("alterarValorInvestimento converte R$ em % com 2 casas", () => {
    const resultado = alterarValorInvestimento(
      rascunho([], { rendaPrevista: "3000", percentualInvestimento: "0" }),
      0,
      "1000",
    )

    expect(resultado.percentualInvestimento).toBe("33,33")
  })

  it("alterarValorInvestimento nao sobrescreve o texto que o usuario esta digitando", () => {
    const resultado = alterarValorInvestimento(rascunho([]), 1000, "12,")

    expect(resultado.valorInvestimento).toBe("12,")
    expect(resultado.percentualInvestimento).toBe("0,24")
  })

  it("alterarPercentualSublimite no pai nao sobrescreve o % digitado", () => {
    const resultado = alterarPercentualSublimite(
      rascunho([pai("alim", "0")]),
      1000,
      "alim",
      "20,",
    )

    const linhaAlim = resultado.sublimites.find((l) => l.categoriaId === "alim")
    expect(linhaAlim?.percentual).toBe("20,")
    expect(linhaAlim?.valor).toBe("700")
  })

  it("alterarValorSublimite no pai converte R$ em % sobre a sobra e preserva o R$ digitado", () => {
    const resultado = alterarValorSublimite(
      rascunho([pai("alim", "0")]),
      1000,
      "alim",
      "700,",
    )

    const linhaAlim = resultado.sublimites.find((l) => l.categoriaId === "alim")
    expect(linhaAlim?.valor).toBe("700,")
    expect(linhaAlim?.percentual).toBe("20")
  })

  it("alterarValorSublimite na subcategoria converte R$ em % sobre o R$ do pai", () => {
    const resultado = alterarValorSublimite(
      rascunho([pai("alim", "20", { valor: "700" }), filho("rest", "alim", "0")]),
      1000,
      "rest",
      "350",
    )

    const linhaRest = resultado.sublimites.find((l) => l.categoriaId === "rest")
    expect(linhaRest?.percentual).toBe("50")
    expect(linhaRest?.valor).toBe("350")
  })

  it("borda: alterarValorSublimite na subcategoria com pai de R$ 0 resulta em 0%", () => {
    const resultado = alterarValorSublimite(
      rascunho([pai("alim", "0", { valor: "0" }), filho("rest", "alim", "0")]),
      1000,
      "rest",
      "350",
    )

    const linhaRest = resultado.sublimites.find((l) => l.categoriaId === "rest")
    expect(linhaRest?.percentual).toBe("0")
  })

  it("alterarPercentualSublimite na subcategoria arredonda o R$ para 2 casas sobre o pai", () => {
    const resultado = alterarPercentualSublimite(
      rascunho([pai("alim", "20"), filho("rest", "alim", "0")]),
      1000,
      "rest",
      "33,333",
    )

    const linhaRest = resultado.sublimites.find((l) => l.categoriaId === "rest")
    expect(linhaRest?.percentual).toBe("33,333")
    expect(linhaRest?.valor).toBe("233,33")
  })
})

// ---------------------------------------------------------------------------
// 3. Zerar pai zera filhos; remover sublimite
// ---------------------------------------------------------------------------

describe("alterarPercentualSublimite / removerSublimite (hierarquia, item 14)", () => {
  it("zerar o pai zera o % e o R$ das subcategorias, sem mexer em irmaos", () => {
    const resultado = alterarPercentualSublimite(
      rascunho([
        pai("alim", "20", { valor: "700" }),
        filho("rest", "alim", "50", { valor: "350" }),
        pai("lazer", "30", { valor: "1050" }),
      ]),
      1000,
      "alim",
      "0",
    )

    const porId = (id: string) => resultado.sublimites.find((l) => l.categoriaId === id)
    expect(porId("alim")?.percentual).toBe("0")
    expect(porId("alim")?.valor).toBe("0")
    expect(porId("rest")?.percentual).toBe("0")
    expect(porId("rest")?.valor).toBe("0")
    expect(porId("lazer")?.percentual).toBe("30")
    expect(porId("lazer")?.valor).toBe("1050")
  })

  it("zerar o pai preserva o percentual e o R$ da subcategoria arquivada (intocavel ate remover)", () => {
    const resultado = alterarPercentualSublimite(
      rascunho([
        pai("alim", "20", { valor: "700" }),
        filho("rest", "alim", "50", { valor: "350" }),
        filho("antigo", "alim", "5", { valor: "35", categoriaArquivada: true }),
      ]),
      1000,
      "alim",
      "0",
    )

    const porId = (id: string) => resultado.sublimites.find((l) => l.categoriaId === id)
    expect(porId("rest")?.percentual).toBe("0")
    expect(porId("rest")?.valor).toBe("0")
    expect(porId("antigo")?.percentual).toBe("5")
    expect(porId("antigo")?.valor).toBe("35")
  })

  it("alterarValorSublimite zerando o pai (R$ 0) preserva a subcategoria arquivada; as ativas zeram", () => {
    const resultado = alterarValorSublimite(
      rascunho([
        pai("alim", "20", { valor: "700" }),
        filho("rest", "alim", "50", { valor: "350" }),
        filho("antigo", "alim", "5", { valor: "35", categoriaArquivada: true }),
      ]),
      1000,
      "alim",
      "0",
    )

    const porId = (id: string) => resultado.sublimites.find((l) => l.categoriaId === id)
    expect(porId("alim")).toMatchObject({ percentual: "0", valor: "0" })
    expect(porId("rest")).toMatchObject({ percentual: "0", valor: "0" })
    expect(porId("antigo")).toMatchObject({ percentual: "5", valor: "35" })
  })

  it("removerSublimite de pai ativo preserva a subcategoria arquivada intacta", () => {
    const resultado = removerSublimite(
      rascunho([
        pai("alim", "20", { valor: "700" }),
        filho("rest", "alim", "50", { valor: "350" }),
        filho("antigo", "alim", "5", { valor: "35", categoriaArquivada: true }),
      ]),
      "alim",
    )

    const porId = (id: string) => resultado.sublimites.find((l) => l.categoriaId === id)
    expect(porId("alim")?.percentual).toBe("0")
    expect(porId("rest")).toMatchObject({ percentual: "0", valor: "0" })
    expect(porId("antigo")).toMatchObject({ percentual: "5", valor: "35" })
  })

  it("removerSublimite de categoria de primeiro nivel zera a categoria e as subcategorias", () => {
    const resultado = removerSublimite(
      rascunho([pai("alim", "20", { valor: "700" }), filho("rest", "alim", "50", { valor: "350" })]),
      "alim",
    )

    const porId = (id: string) => resultado.sublimites.find((l) => l.categoriaId === id)
    expect(porId("alim")?.percentual).toBe("0")
    expect(porId("rest")?.percentual).toBe("0")
    expect(porId("rest")?.valor).toBe("0")
  })

  it("removerSublimite de subcategoria zera so a subcategoria; o pai mantem o sublimite", () => {
    const resultado = removerSublimite(
      rascunho([pai("alim", "20", { valor: "700" }), filho("rest", "alim", "50", { valor: "350" })]),
      "rest",
    )

    const porId = (id: string) => resultado.sublimites.find((l) => l.categoriaId === id)
    expect(porId("rest")?.percentual).toBe("0")
    expect(porId("alim")?.percentual).toBe("20")
    expect(porId("alim")?.valor).toBe("700")
  })

  it("removerSublimite de categoria arquivada remove a linha e as subcategorias arquivadas dela", () => {
    const resultado = removerSublimite(
      rascunho([
        pai("transp", "10", { categoriaArquivada: true }),
        filho("antigo", "transp", "5", { categoriaArquivada: true }),
        pai("lazer", "30"),
      ]),
      "transp",
    )

    const ids = resultado.sublimites.map((l) => l.categoriaId)
    expect(ids).toEqual(["lazer"])
  })
})

// ---------------------------------------------------------------------------
// 4. montarRequest, temAlteracoes, lerDecimal, somas e excessos
// ---------------------------------------------------------------------------

describe("montarRequest (envia so o que o backend precisa)", () => {
  it("envia apenas sublimites com percentual > 0, convertendo texto com virgula para numero", () => {
    const request = montarRequest(
      rascunho(
        [pai("alim", "20"), filho("rest", "alim", "0"), pai("lazer", "0,5"), pai("viagem", "0")],
        { rendaPrevista: "5000", percentualInvestimento: "10,5" },
      ),
    )

    expect(request).toEqual({
      rendaPrevista: 5000,
      percentualInvestimento: 10.5,
      sublimites: [
        { categoriaId: "alim", percentual: 20 },
        { categoriaId: "lazer", percentual: 0.5 },
      ],
    })
  })

  it("categoria arquivada com sublimite salvo e enviada com o percentual salvo", () => {
    const request = montarRequest(
      rascunho([pai("alim", "20"), pai("transp", "10", { categoriaArquivada: true })]),
    )

    expect(request?.sublimites).toEqual([
      { categoriaId: "alim", percentual: 20 },
      { categoriaId: "transp", percentual: 10 },
    ])
  })

  it("rascunho ilegivel (renda, % de investimento ou percentual de linha invalidos) retorna null", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "abc" }))).toBeNull()
    expect(montarRequest(rascunho([], { percentualInvestimento: "150" }))).toBeNull()
    expect(montarRequest(rascunho([pai("alim", "xyz")]))).toBeNull()
  })

  it("borda: percentual de investimento 100 e aceito, 100,01 e recusado", () => {
    expect(montarRequest(rascunho([], { percentualInvestimento: "100" }))).not.toBeNull()
    expect(montarRequest(rascunho([], { percentualInvestimento: "100,01" }))).toBeNull()
  })

  it("soma de primeiro nivel acima de 100 retorna null; exatamente 100 e aceito", () => {
    expect(montarRequest(rascunho([pai("a", "60"), pai("b", "40")]))).not.toBeNull()
    expect(montarRequest(rascunho([pai("a", "60"), pai("b", "40,01")]))).toBeNull()
  })

  it("renda no teto exato 9999999999999999,99 e aceita", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "9999999999999999,99" }))).not.toBeNull()
  })

  it("renda com zeros a esquerda dentro do teto (0009999999999999999) e aceita", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "0009999999999999999" }))).not.toBeNull()
  })

  it("renda em notacao cientifica (1e30) e recusada", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "1e30" }))).toBeNull()
  })

  it("renda em hexadecimal (0x10) e recusada", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "0x10" }))).toBeNull()
  })

  it("renda negativa (-1) e recusada", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "-1" }))).toBeNull()
  })

  it("renda acima do teto tecnico (9.999.999.999.999.999,99) retorna null (item 20)", () => {
    expect(montarRequest(rascunho([], { rendaPrevista: "10000000000000000" }))).toBeNull()
  })
})

describe("temAlteracoes (dirty state)", () => {
  const base = plano({
    sublimites: [sublimiteSalvo({ categoriaId: "alim", percentual: 20, valor: 700 })],
  })

  it("rascunho igual ao plano salvo nao tem alteracoes", () => {
    expect(temAlteracoes(rascunho([pai("alim", "20")]), base)).toBe(false)
  })

  it("borda: mesma renda escrita com formato diferente nao conta como alteracao", () => {
    expect(
      temAlteracoes(rascunho([pai("alim", "20")], { rendaPrevista: "5000,00" }), base),
    ).toBe(false)
  })

  it("renda, % de investimento ou sublimite alterados contam como alteracao", () => {
    expect(temAlteracoes(rascunho([pai("alim", "20")], { rendaPrevista: "5001" }), base)).toBe(true)
    expect(
      temAlteracoes(rascunho([pai("alim", "20")], { percentualInvestimento: "10,5" }), base),
    ).toBe(true)
    expect(temAlteracoes(rascunho([pai("alim", "0")]), base)).toBe(true)
  })

  it("sublimite novo com % > 0 e alteracao; novo com 0% nao e", () => {
    expect(
      temAlteracoes(rascunho([pai("alim", "20"), pai("lazer", "5")]), base),
    ).toBe(true)
    expect(
      temAlteracoes(rascunho([pai("alim", "20"), pai("lazer", "0")]), base),
    ).toBe(false)
  })

  it("sublimite arquivado salvo que sumiu do rascunho e alteracao", () => {
    const comArquivada = plano({
      sublimites: [
        sublimiteSalvo({ categoriaId: "alim", percentual: 20, valor: 700 }),
        sublimiteSalvo({ categoriaId: "transp", categoriaArquivada: true, percentual: 10 }),
      ],
    })

    expect(temAlteracoes(rascunho([pai("alim", "20")]), comArquivada)).toBe(true)
  })
})

describe("lerDecimal", () => {
  it("aceita virgula e ponto como separador decimal", () => {
    expect(lerDecimal("1234,5")).toBe(1234.5)
    expect(lerDecimal("1234.5")).toBe(1234.5)
  })

  it("ignora espacos ao redor", () => {
    expect(lerDecimal(" 12 ")).toBe(12)
  })

  it("vazio ou so espacos retorna null", () => {
    expect(lerDecimal("")).toBeNull()
    expect(lerDecimal("   ")).toBeNull()
  })

  it("texto invalido ou infinito retorna null", () => {
    expect(lerDecimal("abc")).toBeNull()
    expect(lerDecimal("Infinity")).toBeNull()
  })
})

describe("somaPercentuais e listarExcessosPercentuais (item 20: soma <= 100)", () => {
  it("soma apenas as linhas do mesmo pai (null = primeiro nivel)", () => {
    const linhas = [
      pai("alim", "20"),
      pai("lazer", "30,5"),
      filho("rest", "alim", "50"),
    ]

    expect(somaPercentuais(linhas, null)).toBeCloseTo(50.5)
    expect(somaPercentuais(linhas, "alim")).toBe(50)
  })

  it("linhas vazias ou percentual vazio contam como 0", () => {
    expect(somaPercentuais([], null)).toBe(0)
    expect(somaPercentuais([pai("alim", "")], null)).toBe(0)
  })

  it("primeiro nivel exatamente 100 nao e excesso; 100,01 e excesso", () => {
    expect(listarExcessosPercentuais([pai("a", "60"), pai("b", "40")])).toEqual([])

    const excessos = listarExcessosPercentuais([pai("a", "60"), pai("b", "40,01")])
    expect(excessos).toHaveLength(1)
    expect(excessos[0]?.parentId).toBeNull()
    expect(excessos[0]?.soma).toBeCloseTo(100.01)
  })

  it("subcategorias do mesmo pai somadas acima de 100 geram excesso com nome do pai", () => {
    const linhas = [
      pai("alim", "0", { nome: "Alimentacao" }),
      filho("s1", "alim", "50"),
      filho("s2", "alim", "50,5"),
    ]

    const excessos = listarExcessosPercentuais(linhas)
    expect(excessos).toEqual([{ parentId: "alim", nomePai: "Alimentacao", soma: 100.5 }])
  })

  it("subcategorias de pais diferentes nao se somam", () => {
    const linhas = [
      pai("a", "0"),
      pai("b", "0"),
      filho("s1", "a", "60"),
      filho("s2", "b", "60"),
    ]

    expect(listarExcessosPercentuais(linhas)).toEqual([])
  })
})

// ---------------------------------------------------------------------------
// 5. criarRascunho
// ---------------------------------------------------------------------------

describe("criarRascunho (despesas ativas + arquivadas com sublimite)", () => {
  const restaurante = categoria({ id: "rest", nome: "Restaurante", parentId: "alim" })
  const mercado = categoria({ id: "merc", nome: "Mercado", parentId: "alim", arquivada: true })
  const alim = categoria({
    id: "alim",
    nome: "Alimentacao",
    subcategorias: [restaurante],
  })
  const lazer = categoria({ id: "lazer", nome: "Lazer" })
  const salario = categoria({ id: "salario", nome: "Salario", tipo: "Receita" })
  const transp = categoria({ id: "transp", nome: "Transporte", arquivada: true })
  const antigo = categoria({ id: "antigo", nome: "Antigo", arquivada: true })
  const categorias = [alim, restaurante, mercado, lazer, salario, transp, antigo]

  const planoComSalvos = plano({
    sublimites: [
      sublimiteSalvo({ categoriaId: "alim", percentual: 20, valor: 700 }),
      sublimiteSalvo({ categoriaId: "rest", parentId: "alim", percentual: 50, valor: 350, gastoRealizado: 80 }),
      sublimiteSalvo({
        categoriaId: "transp",
        categoriaArquivada: true,
        percentual: 10,
        valor: 350,
        gastoRealizado: 400,
      }),
    ],
    gastosPorCategoria: [
      { categoriaId: "alim", gastoRealizado: 450 },
      { categoriaId: "rest", gastoRealizado: 120 },
    ],
  })

  it("inclui despesas ativas e arquivadas que ja tinham sublimite; exclui receita e arquivada sem sublimite", () => {
    const rascunhoCriado = criarRascunho(planoComSalvos, categorias)
    const ids = rascunhoCriado.sublimites.map((l) => l.categoriaId)

    expect(ids).toEqual(["alim", "rest", "lazer", "transp"])
    expect(ids).not.toContain("salario")
    expect(ids).not.toContain("antigo")
    expect(ids).not.toContain("merc")
  })

  it("categoria ativa sem sublimite salvo nasce com 0%, R$ 0 e sem gasto", () => {
    const lazerLinha = criarRascunho(planoComSalvos, categorias).sublimites.find(
      (l) => l.categoriaId === "lazer",
    )

    expect(lazerLinha).toMatchObject({ percentual: "0", valor: "0", gastoRealizado: 0 })
  })

  it("gasto realizado vem de gastosPorCategoria; sem entrada la, cai no gasto do sublimite salvo", () => {
    const linhas = criarRascunho(planoComSalvos, categorias).sublimites
    const porId = (id: string) => linhas.find((l) => l.categoriaId === id)

    expect(porId("alim")?.gastoRealizado).toBe(450)
    expect(porId("rest")?.gastoRealizado).toBe(120)
    expect(porId("transp")?.gastoRealizado).toBe(400)
  })

  it("sublimites salvos viram percentual/valor em texto e mantem a hierarquia", () => {
    const linhas = criarRascunho(planoComSalvos, categorias).sublimites
    const porId = (id: string) => linhas.find((l) => l.categoriaId === id)

    expect(porId("alim")).toMatchObject({ parentId: null, percentual: "20", valor: "700" })
    expect(porId("rest")).toMatchObject({ parentId: "alim", percentual: "50", valor: "350" })
    expect(porId("transp")).toMatchObject({ categoriaArquivada: true, percentual: "10" })
  })

  it("copia renda, % de investimento e R$ de investimento do plano", () => {
    const rascunhoCriado = criarRascunho(planoComSalvos, categorias)

    expect(rascunhoCriado).toMatchObject({
      rendaPrevista: "5000",
      percentualInvestimento: "10",
      valorInvestimento: "500",
    })
  })

  it("inclui subcategoria arquivada com sublimite salvo sob pai ativo, com parentId correto", () => {
    const subAntiga = categoria({ id: "antiga", nome: "Antiga", parentId: "alim", arquivada: true })
    const planoComSubArquivada = plano({
      sublimites: [
        sublimiteSalvo({ categoriaId: "alim", percentual: 20, valor: 700 }),
        sublimiteSalvo({
          categoriaId: "antiga",
          parentId: "alim",
          categoriaArquivada: true,
          percentual: 5,
          valor: 35,
        }),
      ],
    })

    const linhas = criarRascunho(planoComSubArquivada, [...categorias, subAntiga]).sublimites
    const antiga = linhas.find((l) => l.categoriaId === "antiga")

    expect(antiga).toMatchObject({ parentId: "alim", categoriaArquivada: true, percentual: "5", valor: "35" })
  })

  it("sem lista de categorias, usa somente os sublimites salvos", () => {
    const ids = criarRascunho(planoComSalvos).sublimites.map((l) => l.categoriaId)

    expect(ids).toEqual(["alim", "rest", "transp"])
  })
})

// ---------------------------------------------------------------------------
// 6. aplicarCopiaDePlano (item 20: copiar de outro mes)
// ---------------------------------------------------------------------------

describe("aplicarCopiaDePlano (copiar de outro mes, item 20)", () => {
  // Destino: renda 5000, % investimento 10. Origem: renda 9000, % investimento 15.
  // Com fixas 1000 e copia de investimento 15%: planejado 750, sobra 3250.
  const rascunhoDestino = rascunho(
    [
      pai("alim", "20", { valor: "700" }),
      filho("rest", "alim", "50", { valor: "350" }),
      pai("lazer", "15", { valor: "525" }),
      filho("cinema", "lazer", "5", { valor: "26,25" }),
      pai("viagem", "0"),
      filho("passagem", "viagem", "0"),
      pai("transp", "10", { valor: "350", categoriaArquivada: true }),
    ],
    { rendaPrevista: "5000", percentualInvestimento: "10", valorInvestimento: "500" },
  )

  const planoOrigem = plano({
    rendaPrevista: 9000,
    percentualInvestimento: 15,
    sublimites: [
      sublimiteSalvo({ categoriaId: "alim", percentual: 30 }),
      sublimiteSalvo({ categoriaId: "rest", parentId: "alim", percentual: 40 }),
      sublimiteSalvo({ categoriaId: "cinema", parentId: "lazer", percentual: 5 }),
      sublimiteSalvo({ categoriaId: "viagem", categoriaArquivada: true, percentual: 25 }),
      sublimiteSalvo({ categoriaId: "passagem", parentId: "viagem", percentual: 10 }),
    ],
  })

  const porId = (r: Rascunho, id: string) => r.sublimites.find((l) => l.categoriaId === id)

  it("copia o percentual de investimento da origem e recalcula o R$ sobre a renda do destino", () => {
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000)

    expect(resultado.percentualInvestimento).toBe("15")
    expect(resultado.valorInvestimento).toBe("750")
  })

  it("nao copia a renda por padrao; com copiarRenda a renda da origem entra", () => {
    const semRenda = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000)
    expect(semRenda.rendaPrevista).toBe("5000")

    const comRenda = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000, { copiarRenda: true })
    expect(comRenda.rendaPrevista).toBe("9000")
    expect(comRenda.valorInvestimento).toBe("1350")
  })

  it("categoria ativa com sublimite na origem recebe o percentual da origem e R$ sobre a sobra do destino", () => {
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000)

    expect(porId(resultado, "alim")).toMatchObject({ percentual: "30", valor: "975" })
    expect(porId(resultado, "rest")).toMatchObject({ percentual: "40", valor: "390" })
  })

  it("categoria ativa sem sublimite na origem vira 0%, inclusive a subcategoria dela", () => {
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000)

    expect(porId(resultado, "lazer")).toMatchObject({ percentual: "0", valor: "0" })
    expect(porId(resultado, "cinema")).toMatchObject({ percentual: "0", valor: "0" })
  })

  it("categoria arquivada na origem e ignorada: a categoria ativa no destino fica em 0%", () => {
    const destinoComViagem = rascunho([pai("viagem", "0")])
    const resultado = aplicarCopiaDePlano(destinoComViagem, planoOrigem, 1000)

    expect(porId(resultado, "viagem")).toMatchObject({ percentual: "0", valor: "0" })
  })

  it("subcategoria cujo pai esta arquivado na origem ou em 0% fica em 0%", () => {
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000)

    // passagem: pai viagem arquivado na origem. cinema: pai lazer sem sublimite na origem.
    expect(porId(resultado, "passagem")).toMatchObject({ percentual: "0", valor: "0" })
    expect(porId(resultado, "cinema")?.percentual).toBe("0")
  })

  it("valores em R$ sao recalculados sobre as fixas do destino (sobra muda com fixas)", () => {
    // fixas 2000: sobra = 5000 - 2000 - 750 = 2250; alim 30% = 675
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 2000)

    expect(porId(resultado, "alim")?.valor).toBe("675")
  })

  it("borda: investimento reduzido pelas fixas do destino deixa sobra zero e sublimites em R$ 0", () => {
    // fixas 4500: investimento cai para 500, sobra = 0
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 4500)

    expect(porId(resultado, "alim")?.valor).toBe("0")
    expect(porId(resultado, "rest")?.valor).toBe("0")
  })

  it("arquivada do destino fica intacta (%, R$ e posicao) mesmo com o mesmo categoriaId na origem", () => {
    const destino = rascunho([
      pai("alim", "20", { valor: "700" }),
      pai("transp", "10", { valor: "350", categoriaArquivada: true }),
      filho("antigo", "alim", "5", { valor: "35", categoriaArquivada: true }),
      pai("lazer", "15", { valor: "525" }),
    ])
    const origem = plano({
      rendaPrevista: 9000,
      percentualInvestimento: 15,
      sublimites: [
        sublimiteSalvo({ categoriaId: "alim", percentual: 30 }),
        sublimiteSalvo({ categoriaId: "transp", percentual: 25 }),
        sublimiteSalvo({ categoriaId: "antigo", parentId: "alim", percentual: 40 }),
      ],
    })

    const resultado = aplicarCopiaDePlano(destino, origem, 1000)

    expect(resultado.sublimites.map((l) => l.categoriaId)).toEqual(["alim", "transp", "antigo", "lazer"])
    expect(porId(resultado, "transp")).toMatchObject({ percentual: "10", valor: "350", categoriaArquivada: true })
    expect(porId(resultado, "antigo")).toMatchObject({ percentual: "5", valor: "35", categoriaArquivada: true })
  })

  it("subcategoria arquivada de pai ativo no destino fica intacta (%, R$ e posicao) na copia", () => {
    const destino = rascunho([
      pai("alim", "20", { valor: "700" }),
      filho("rest", "alim", "50", { valor: "350" }),
      filho("antigo", "alim", "5", { valor: "35", categoriaArquivada: true }),
    ])
    const origem = plano({
      rendaPrevista: 9000,
      percentualInvestimento: 15,
      sublimites: [sublimiteSalvo({ categoriaId: "alim", percentual: 30 })],
    })

    const resultado = aplicarCopiaDePlano(destino, origem, 1000)

    expect(resultado.sublimites.map((l) => l.categoriaId)).toEqual(["alim", "rest", "antigo"])
    expect(porId(resultado, "antigo")).toMatchObject({ parentId: "alim", percentual: "5", valor: "35" })
  })

  it("arquivada no destino com sublimite nao e apagada pela copia (item 14: continua no plano)", () => {
    const resultado = aplicarCopiaDePlano(rascunhoDestino, planoOrigem, 1000)

    const transp = porId(resultado, "transp")
    expect(transp).toBeDefined()
    expect(transp?.percentual).toBe("10")
  })
})
