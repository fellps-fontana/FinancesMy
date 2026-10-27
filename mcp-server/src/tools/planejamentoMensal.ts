import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api, ApiError } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda } from "../format.js";
import type { CategoriaResponse } from "./categorias.js";

const PERCENTUAL_MAXIMO = 100;
const BASE_PERCENTUAL = 100;
const FATOR_DUAS_CASAS = 100;
const ANO_MINIMO = 1;
const ANO_MAXIMO = 9999;
const MES_MINIMO = 1;
const MES_MAXIMO = 12;
const CASAS_PERCENTUAL_PADRAO = 2;
const CASAS_FRACAO_UTILIZADA = 0;
const INDENTACAO_CATEGORIA = "  ";
const INDENTACAO_SUBCATEGORIA = "    ";
const SEPARADOR_HIERARQUIA = " > ";
const SUFIXO_ARQUIVADA = " (arquivada)";
const CAMINHO_CATEGORIAS = "api/Categorias";
const ROTULO_INVESTIMENTO = "Investimento";
const UNIDADE = { PERCENTUAL: "percentual", REAIS: "reais" } as const;

type Unidade = (typeof UNIDADE)[keyof typeof UNIDADE];

export interface SublimitePlanoResponse {
  categoriaId: string;
  parentId: string | null;
  nome: string;
  icone: string | null;
  categoriaArquivada: boolean;
  percentual: number;
  valor: number;
  gastoRealizado: number;
  fracaoUtilizada: number;
  estourado: boolean;
}

export interface GastoCategoriaResponse {
  categoriaId: string;
  gastoRealizado: number;
}

export interface PlanoMensalResponse {
  ano: number;
  mes: number;
  somenteLeitura: boolean;
  rendaPrevista: number;
  fixas: number;
  percentualInvestimento: number;
  investimentoPlanejado: number;
  investimento: number;
  investimentoReduzido: boolean;
  planoNaoFecha: boolean;
  sobra: number;
  naoAlocado: number;
  gastoSemSublimite: number;
  sublimites: SublimitePlanoResponse[];
  gastosPorCategoria: GastoCategoriaResponse[];
}

interface MedidaInformada {
  unidade: Unidade;
  quantidade: number;
}

interface SublimiteEntrada {
  categoriaId: string;
  medida: MedidaInformada;
}

export interface PercentualSublimite {
  categoriaId: string;
  percentual: number;
}

interface SubcategoriaEmReais {
  categoriaId: string;
  paiId: string;
  valor: number;
}

interface PlanoMensalEnvio {
  rendaPrevista: number;
  percentualInvestimento: number;
  sublimites: PercentualSublimite[];
}

interface LinhaCategoria {
  id: string;
  nome: string;
  parentId: string | null;
  arquivada: boolean;
}

interface CategoriaIndice {
  parentId: string | null;
  arquivada: boolean;
  rotulo: string;
}

export type IndiceCategorias = Map<string, CategoriaIndice>;

const campoAno = z.number().int().min(ANO_MINIMO).max(ANO_MAXIMO).describe("Ano com 4 digitos (1 a 9999)");
const campoMes = z.number().int().min(MES_MINIMO).max(MES_MAXIMO).describe("Mes (1 a 12)");

function caminhoPlano(ano: number, mes: number): string {
  return `api/planejamento-mensal/${ano}/${mes}`;
}

function formatarNumero(valor: number, casas: number): string {
  return valor.toLocaleString("pt-BR", { minimumFractionDigits: 0, maximumFractionDigits: casas });
}

function formatarPercentual(valor: number, casas = CASAS_PERCENTUAL_PADRAO): string {
  return `${formatarNumero(valor, casas)}%`;
}

function formatarPeriodo(ano: number, mes: number): string {
  return `${String(mes).padStart(2, "0")}/${ano}`;
}

function mensagemMesSemPlano(ano: number, mes: number): string {
  return `Sem plano para ${formatarPeriodo(ano, mes)}: mes passado sem plano. Mes passado nao ganha plano retroativo.`;
}

function mensagemSemPlanoNaOrigem(ano: number, mes: number): string {
  return `Sem plano na origem (${formatarPeriodo(ano, mes)}): nada para copiar.`;
}

function arredondarPercentual(valor: number): number {
  return Math.round(valor * FATOR_DUAS_CASAS) / FATOR_DUAS_CASAS;
}

function valorDoPercentual(percentual: number, base: number): number {
  return (base * percentual) / BASE_PERCENTUAL;
}

function percentualSobreBase(valor: number, base: number, rotulo: string): number {
  if (valor <= 0) return 0;
  if (base <= 0) {
    throw new Error(`${rotulo}: sem base para converter R$ em % (sobra zerada). Informe em percentual.`);
  }
  const percentual = arredondarPercentual((valor / base) * BASE_PERCENTUAL);
  if (percentual > PERCENTUAL_MAXIMO) {
    throw new Error(`${rotulo}: valor em R$ acima da base (passa de ${PERCENTUAL_MAXIMO}%).`);
  }
  return percentual;
}

function lerMedida(percentual: number | undefined, valor: number | undefined, rotulo: string): MedidaInformada {
  if (percentual !== undefined && valor === undefined) {
    return { unidade: UNIDADE.PERCENTUAL, quantidade: percentual };
  }
  if (valor !== undefined && percentual === undefined) {
    return { unidade: UNIDADE.REAIS, quantidade: valor };
  }
  throw new Error(`${rotulo}: informe percentual OU valor (R$), exatamente um.`);
}

function ehEmReais(entrada: SublimiteEntrada): boolean {
  return entrada.medida.unidade === UNIDADE.REAIS;
}

function garantirCategoriasUnicas(entradas: SublimiteEntrada[]): void {
  const vistas = new Set<string>();
  for (const { categoriaId } of entradas) {
    if (vistas.has(categoriaId)) {
      throw new Error(`Categoria ${categoriaId} aparece mais de uma vez em sublimites. Envie um sublimite por categoria.`);
    }
    vistas.add(categoriaId);
  }
}

function percentualDoInvestimento(medida: MedidaInformada, rendaPrevista: number): number {
  if (medida.unidade === UNIDADE.PERCENTUAL) return medida.quantidade;
  return percentualSobreBase(medida.quantidade, rendaPrevista, ROTULO_INVESTIMENTO);
}

function investimentoCalculado(rendaPrevista: number, percentualInvestimento: number, fixas: number): number {
  return Math.max(0, Math.min(valorDoPercentual(percentualInvestimento, rendaPrevista), rendaPrevista - fixas));
}

function calcularSobra(rendaPrevista: number, fixas: number, percentualInvestimento: number): number {
  return Math.max(0, rendaPrevista - fixas - investimentoCalculado(rendaPrevista, percentualInvestimento, fixas));
}

function paiDaCategoriaParaSublimite(indice: IndiceCategorias, categoriaId: string): string | null {
  const entrada = indice.get(categoriaId);
  if (!entrada) throw new Error(`Categoria ${categoriaId} nao encontrada em ${CAMINHO_CATEGORIAS}.`);
  if (entrada.arquivada) throw new Error(`Categoria ${categoriaId} esta arquivada: nao recebe sublimite.`);
  return entrada.parentId;
}

function mensagemPaiSemSublimite(sub: SubcategoriaEmReais): string {
  return `Subcategoria ${sub.categoriaId} em R$ exige sublimite > 0 do pai ${sub.paiId} no mesmo envio. Informe o pai ou use percentual.`;
}

function resolverPercentuaisSublimites(
  entradas: SublimiteEntrada[],
  sobra: number,
  paiDe: (categoriaId: string) => string | null
): PercentualSublimite[] {
  const percentuais = new Map<string, number>();
  const subcategoriasEmReais: SubcategoriaEmReais[] = [];

  for (const { categoriaId, medida } of entradas) {
    if (medida.unidade === UNIDADE.PERCENTUAL) {
      percentuais.set(categoriaId, medida.quantidade);
      continue;
    }
    const paiId = paiDe(categoriaId);
    if (paiId === null) {
      percentuais.set(categoriaId, percentualSobreBase(medida.quantidade, sobra, `Sublimite ${categoriaId} em R$`));
    } else {
      subcategoriasEmReais.push({ categoriaId, paiId, valor: medida.quantidade });
    }
  }

  for (const sub of subcategoriasEmReais) {
    if (sub.valor <= 0) {
      percentuais.set(sub.categoriaId, 0);
      continue;
    }
    const percentualDoPai = percentuais.get(sub.paiId) ?? 0;
    if (percentualDoPai <= 0) throw new Error(mensagemPaiSemSublimite(sub));
    const valorDoPai = valorDoPercentual(percentualDoPai, sobra);
    percentuais.set(sub.categoriaId, percentualSobreBase(sub.valor, valorDoPai, `Subcategoria ${sub.categoriaId} em R$`));
  }

  return [...percentuais].map(([categoriaId, percentual]) => ({ categoriaId, percentual }));
}

async function buscarPlanoOuNull(ano: number, mes: number): Promise<PlanoMensalResponse | null> {
  try {
    return await api.get<PlanoMensalResponse>(caminhoPlano(ano, mes));
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}

function achatarCategorias(categorias: CategoriaResponse[]): LinhaCategoria[] {
  return categorias.flatMap((categoria) => [
    { id: categoria.id, nome: categoria.nome, parentId: categoria.parentId ?? null, arquivada: categoria.arquivada },
    ...(categoria.subcategorias ?? []).map((sub) => ({
      id: sub.id,
      nome: sub.nome,
      parentId: categoria.id,
      arquivada: sub.arquivada,
    })),
  ]);
}

function montarRotulo(linha: LinhaCategoria, nomePorId: Map<string, string>): string {
  const nome = linha.arquivada ? `${linha.nome}${SUFIXO_ARQUIVADA}` : linha.nome;
  const nomePai = linha.parentId ? nomePorId.get(linha.parentId) : undefined;
  return nomePai ? `${nomePai}${SEPARADOR_HIERARQUIA}${nome}` : nome;
}

function indexarCategorias(categorias: CategoriaResponse[]): IndiceCategorias {
  const linhas = achatarCategorias(categorias);
  const nomePorId = new Map(linhas.map((linha) => [linha.id, linha.nome] as const));
  return new Map(
    linhas.map((linha): [string, CategoriaIndice] => [
      linha.id,
      { parentId: linha.parentId, arquivada: linha.arquivada, rotulo: montarRotulo(linha, nomePorId) },
    ])
  );
}

async function buscarIndiceCategorias(): Promise<IndiceCategorias> {
  const [ativas, arquivadas] = await Promise.all([
    api.get<CategoriaResponse[]>(CAMINHO_CATEGORIAS),
    api.get<CategoriaResponse[]>(CAMINHO_CATEGORIAS, { arquivada: true }),
  ]);
  return indexarCategorias([...ativas, ...arquivadas]);
}

async function resolverSublimitesSalvar(
  ano: number,
  mes: number,
  rendaPrevista: number,
  percentualInvestimento: number,
  entradas: SublimiteEntrada[]
): Promise<PercentualSublimite[]> {
  if (!entradas.some(ehEmReais)) {
    return entradas.map(({ categoriaId, medida }) => ({ categoriaId, percentual: medida.quantidade }));
  }
  const planoAtual = await buscarPlanoOuNull(ano, mes);
  if (!planoAtual) throw new Error(mensagemMesSemPlano(ano, mes));
  const indice = await buscarIndiceCategorias();
  const sobra = calcularSobra(rendaPrevista, planoAtual.fixas, percentualInvestimento);
  return resolverPercentuaisSublimites(entradas, sobra, (categoriaId) =>
    paiDaCategoriaParaSublimite(indice, categoriaId)
  );
}

function filtrarPercentualPositivo(sublimites: PercentualSublimite[]): PercentualSublimite[] {
  return sublimites.filter((sublimite) => sublimite.percentual > 0);
}

function montarCorpoPlano(
  rendaPrevista: number,
  percentualInvestimento: number,
  sublimites: PercentualSublimite[]
): PlanoMensalEnvio {
  return { rendaPrevista, percentualInvestimento, sublimites: filtrarPercentualPositivo(sublimites) };
}

function ehPrimeiroNivelCopiavel(sublimite: SublimitePlanoResponse): boolean {
  return !sublimite.categoriaArquivada && !sublimite.parentId;
}

function selecionarSublimitesCopiaveis(sublimites: SublimitePlanoResponse[]): PercentualSublimite[] {
  const primeiroNivelCopiado = new Set(sublimites.filter(ehPrimeiroNivelCopiavel).map((s) => s.categoriaId));
  return sublimites
    .filter((s) => !s.categoriaArquivada && (!s.parentId || primeiroNivelCopiado.has(s.parentId)))
    .map((s) => ({ categoriaId: s.categoriaId, percentual: s.percentual }));
}

function montarCorpoCopia(origem: PlanoMensalResponse, destino: PlanoMensalResponse, copiarRenda: boolean): PlanoMensalEnvio {
  return montarCorpoPlano(
    copiarRenda ? origem.rendaPrevista : destino.rendaPrevista,
    origem.percentualInvestimento,
    selecionarSublimitesCopiaveis(origem.sublimites)
  );
}

function garantirMesesDistintos(anoOrigem: number, mesOrigem: number, anoDestino: number, mesDestino: number): void {
  if (anoOrigem === anoDestino && mesOrigem === mesDestino) {
    throw new Error("Origem e destino sao o mesmo mes.");
  }
}

function formatarAvisoInvestimento(plano: PlanoMensalResponse): string[] {
  if (!plano.investimentoReduzido) return [];
  return [
    `AVISO: investimento reduzido de ${formatarMoeda(plano.investimentoPlanejado)} para ${formatarMoeda(plano.investimento)} (fixas nao deixam a renda cobrir o planejado).`,
  ];
}

function formatarAlertaPlanoNaoFecha(plano: PlanoMensalResponse): string[] {
  if (!plano.planoNaoFecha) return [];
  return ["ALERTA: plano nao fecha (fixas maiores que a renda prevista)."];
}

function formatarLinhaSublimite(sublimite: SublimitePlanoResponse): string {
  const indentacao = sublimite.parentId ? INDENTACAO_SUBCATEGORIA : INDENTACAO_CATEGORIA;
  const fracaoEmPercentual = sublimite.fracaoUtilizada * BASE_PERCENTUAL;
  const fracao = formatarPercentual(fracaoEmPercentual, CASAS_FRACAO_UTILIZADA);
  const estouro = sublimite.estourado ? " [ESTOURADO]" : "";
  const arquivada = sublimite.categoriaArquivada ? " (categoria arquivada)" : "";
  return `${indentacao}- ${sublimite.nome} [${sublimite.categoriaId}] | ${formatarPercentual(sublimite.percentual)} = ${formatarMoeda(sublimite.valor)} | gasto ${formatarMoeda(sublimite.gastoRealizado)} (${fracao})${estouro}${arquivada}`;
}

function formatarSublimites(sublimites: SublimitePlanoResponse[]): string[] {
  if (sublimites.length === 0) return [`${INDENTACAO_CATEGORIA}(nenhum sublimite)`];
  return sublimites.map(formatarLinhaSublimite);
}

function rotuloCategoria(categoriaId: string, indice: IndiceCategorias): string {
  return indice.get(categoriaId)?.rotulo ?? categoriaId;
}

function formatarGastosPorCategoria(gastos: GastoCategoriaResponse[], indice: IndiceCategorias): string[] {
  const linhas =
    gastos.length === 0
      ? [`${INDENTACAO_CATEGORIA}(nenhuma despesa)`]
      : gastos.map(
          (gasto) =>
            `${INDENTACAO_CATEGORIA}- ${rotuloCategoria(gasto.categoriaId, indice)} | gasto ${formatarMoeda(gasto.gastoRealizado)}`
        );
  return ["Gasto variavel por categoria (inclui despesas sem sublimite):", ...linhas];
}

function formatarPlano(plano: PlanoMensalResponse, indice: IndiceCategorias): string {
  const sufixoLeitura = plano.somenteLeitura ? " (somente leitura: mes passado)" : "";
  return [
    `Planejamento ${formatarPeriodo(plano.ano, plano.mes)}${sufixoLeitura}`,
    `Renda prevista: ${formatarMoeda(plano.rendaPrevista)}`,
    `Fixas: ${formatarMoeda(plano.fixas)}`,
    `Investimento planejado: ${formatarMoeda(plano.investimentoPlanejado)} (${formatarPercentual(plano.percentualInvestimento)} da renda)`,
    `Investimento calculado: ${formatarMoeda(plano.investimento)}`,
    ...formatarAvisoInvestimento(plano),
    ...formatarAlertaPlanoNaoFecha(plano),
    `Sobra: ${formatarMoeda(plano.sobra)}`,
    "Sublimites:",
    ...formatarSublimites(plano.sublimites),
    `Nao alocado: ${formatarMoeda(plano.naoAlocado)}`,
    `Gasto sem sublimite: ${formatarMoeda(plano.gastoSemSublimite)}`,
    ...formatarGastosPorCategoria(plano.gastosPorCategoria, indice),
  ].join("\n");
}

export function registerPlanejamentoMensalTools(server: McpServer) {
  server.tool(
    "obter_planejamento_mensal",
    "Mostra o planejamento de um mes: renda prevista, despesas fixas, investimento (planejado e calculado), sobra, sublimites por categoria (percentual, valor em R$, gasto do mes), o gasto variavel do mes por categoria de despesa (inclusive as sem sublimite) e o que nao foi alocado. Cada sublimite traz o [categoriaId] usado em salvar_planejamento_mensal. Mes corrente ou futuro sem plano e criado na primeira consulta. Ate 12 meses a frente do mes corrente; alem disso o back recusa.",
    {
      ano: campoAno,
      mes: campoMes,
    },
    async ({ ano, mes }) => {
      try {
        const plano = await buscarPlanoOuNull(ano, mes);
        if (!plano) return ok(mensagemMesSemPlano(ano, mes));
        return ok(formatarPlano(plano, await buscarIndiceCategorias()));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "salvar_planejamento_mensal",
    "Cria ou altera o planejamento de um mes. rendaPrevista e obrigatoria. O investimento vai em percentualInvestimento (0 a 100) OU valorInvestimento (R$), exatamente um. Cada sublimite vai em percentual (0 a 100) OU valor (R$), exatamente um; sublimite com percentual 0 ou valor 0 e removido. Cada categoria aparece no maximo uma vez em sublimites. Valor em R$ e convertido para percentual: categoria de primeiro nivel sobre a sobra do mes; subcategoria sobre o R$ do sublimite do pai enviado no mesmo envio. ATENCAO: o conjunto de sublimites enviado SUBSTITUI o atual inteiro. Chame obter_planejamento_mensal antes e reenvie todos os sublimites que devem continuar; lista vazia remove todos. Se o mes ainda nao tem plano, ele e criado com exatamente os valores enviados (sem copiar o mes anterior). Meses passados nao podem ser alterados. Meses alem de 12 a frente do mes corrente sao recusados. Percentuais incidem sobre a sobra; categoria arquivada e categoria de receita nao recebem sublimite.",
    {
      ano: campoAno,
      mes: campoMes,
      rendaPrevista: z.number().min(0).describe("Renda prevista do mes em R$"),
      percentualInvestimento: z
        .number()
        .min(0)
        .max(PERCENTUAL_MAXIMO)
        .optional()
        .describe("Percentual da renda reservado para investimento (0 a 100). Use este OU valorInvestimento."),
      valorInvestimento: z
        .number()
        .min(0)
        .optional()
        .describe("Investimento em R$ (convertido para % sobre a renda prevista). Use este OU percentualInvestimento."),
      sublimites: z
        .array(
          z.object({
            categoriaId: z.string().uuid(),
            percentual: z
              .number()
              .min(0)
              .max(PERCENTUAL_MAXIMO)
              .optional()
              .describe("Percentual da sobra destinado a categoria (0 a 100). Use este OU valor."),
            valor: z
              .number()
              .min(0)
              .optional()
              .describe("Valor em R$ destinado a categoria. Use este OU percentual."),
          })
        )
        .describe("Conjunto completo de sublimites do mes. Substitui o atual."),
    },
    async ({ ano, mes, rendaPrevista, percentualInvestimento, valorInvestimento, sublimites }) => {
      try {
        const investimento = lerMedida(percentualInvestimento, valorInvestimento, ROTULO_INVESTIMENTO);
        const entradas: SublimiteEntrada[] = sublimites.map((s) => ({
          categoriaId: s.categoriaId,
          medida: lerMedida(s.percentual, s.valor, `Sublimite ${s.categoriaId}`),
        }));
        garantirCategoriasUnicas(entradas);
        const percentualInv = percentualDoInvestimento(investimento, rendaPrevista);
        const percentuais = await resolverSublimitesSalvar(ano, mes, rendaPrevista, percentualInv, entradas);
        const plano = await api.put<PlanoMensalResponse>(
          caminhoPlano(ano, mes),
          montarCorpoPlano(rendaPrevista, percentualInv, percentuais)
        );
        return ok(`Plano salvo.\n${formatarPlano(plano, await buscarIndiceCategorias())}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "copiar_planejamento_mensal",
    "Copia o planejamento de um mes para outro (sobrescreve o plano de destino). Copia o percentual de investimento e os sublimites da origem, sem categorias arquivadas; subcategoria so e copiada se o pai tambem foi copiado. A renda prevista do destino e mantida, a menos que copiarRenda seja true. Mes de origem sem plano e erro. Horizonte e meses passados seguem as regras do back.",
    {
      anoOrigem: campoAno,
      mesOrigem: campoMes,
      anoDestino: campoAno,
      mesDestino: campoMes,
      copiarRenda: z
        .boolean()
        .default(false)
        .describe("Se true, copia a renda prevista da origem; se false (padrao), mantem a renda do destino"),
    },
    async ({ anoOrigem, mesOrigem, anoDestino, mesDestino, copiarRenda }) => {
      try {
        garantirMesesDistintos(anoOrigem, mesOrigem, anoDestino, mesDestino);
        const origem = await buscarPlanoOuNull(anoOrigem, mesOrigem);
        if (!origem) throw new Error(mensagemSemPlanoNaOrigem(anoOrigem, mesOrigem));
        const destino = await buscarPlanoOuNull(anoDestino, mesDestino);
        if (!destino) throw new Error(mensagemMesSemPlano(anoDestino, mesDestino));
        const plano = await api.put<PlanoMensalResponse>(
          caminhoPlano(anoDestino, mesDestino),
          montarCorpoCopia(origem, destino, copiarRenda)
        );
        const cabecalho = `Plano copiado de ${formatarPeriodo(anoOrigem, mesOrigem)} para ${formatarPeriodo(anoDestino, mesDestino)}.`;
        return ok(`${cabecalho}\n${formatarPlano(plano, await buscarIndiceCategorias())}`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
