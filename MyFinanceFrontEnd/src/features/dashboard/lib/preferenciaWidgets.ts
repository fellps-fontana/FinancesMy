// Persistencia da escolha de widgets do Dashboard, 100% client-side
// (localStorage). Escopo explicito da task: nenhum endpoint de backend para
// isso -- e preferencia de UI, nao regra de negocio, mesmo espirito de
// shared/hooks/useTheme.ts (THEME_STORAGE_KEY), que ja usa o mesmo
// localStorage para uma escolha pessoal do usuario sobre a propria tela.
//
// Fica em lib/ (nao hooks/) porque as funcoes aqui sao so leitura/escrita de
// serializacao (parse/stringify + validacao de shape) -- sem useState, sem
// efeito de ciclo de vida React. O estado em si (o que esta ligado agora,
// quando re-renderizar) mora em DashboardPage.tsx via useState, que chama
// estas funcoes puras de IO para carregar o valor inicial e persistir cada
// mudanca (stack.md "Criterio objetivo lib/ vs hooks/ vs components/").

export type WidgetId =
  | "saldo-projetado"
  | "acoes-rapidas"
  | "grafico-entradas-saidas"
  | "limite-gasto"
  | "ultimos-lancamentos"
  | "investimentos"
  | "rendimentos"

export type PreferenciaWidgets = Record<WidgetId, boolean>

// Catalogo de widgets disponiveis no Dashboard -- id (chave de persistencia
// e de renderizacao condicional em DashboardPage.tsx) + label (texto exibido
// em SeletorWidgets.tsx). Fonte unica: DashboardPage e SeletorWidgets leem
// daqui, nunca duplicam a lista de ids em outro arquivo.
export const CATALOGO_WIDGETS: { id: WidgetId; label: string }[] = [
  { id: "saldo-projetado", label: "Saldo projetado do mes" },
  { id: "acoes-rapidas", label: "Acoes rapidas" },
  { id: "grafico-entradas-saidas", label: "Entradas e saidas" },
  { id: "limite-gasto", label: "Limite de gasto por categoria" },
  { id: "ultimos-lancamentos", label: "Ultimos lancamentos" },
  { id: "investimentos", label: "Distribuicao da carteira (investimentos)" },
  { id: "rendimentos", label: "Rendimentos por mes" },
]

// Todos ligados por padrao: os 5 widgets ja existentes sempre apareceram sem
// opcao de esconder, entao o default preserva o comportamento atual (nenhuma
// regressao visual pra quem nunca abriu o seletor); os 2 novos (investimentos
// e rendimentos) entram tambem ligados por padrao, pra ficarem descobriveis
// sem exigir que o usuario ja saiba que existem e va liga-los manualmente.
export const PREFERENCIA_PADRAO: PreferenciaWidgets = CATALOGO_WIDGETS.reduce(
  (acumulado, widget) => ({ ...acumulado, [widget.id]: true }),
  {} as PreferenciaWidgets,
)

const CHAVE_STORAGE = "dashboard:widgets"

function ehShapeValido(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === "object" && valor !== null
}

// Le a preferencia salva, mesclando com o default e ignorando qualquer chave
// desconhecida ou valor que nao seja booleano (ex: storage de uma versao
// antiga do catalogo, ou edicao manual no devtools) -- nunca deixa a UI
// quebrar por um localStorage corrompido/desatualizado.
export function lerPreferenciaWidgets(): PreferenciaWidgets {
  try {
    const bruto = window.localStorage.getItem(CHAVE_STORAGE)
    if (bruto === null) {
      return PREFERENCIA_PADRAO
    }

    const armazenado: unknown = JSON.parse(bruto)
    if (!ehShapeValido(armazenado)) {
      return PREFERENCIA_PADRAO
    }

    const resultado = { ...PREFERENCIA_PADRAO }
    for (const widget of CATALOGO_WIDGETS) {
      const valorSalvo = armazenado[widget.id]
      if (typeof valorSalvo === "boolean") {
        resultado[widget.id] = valorSalvo
      }
    }
    return resultado
  } catch (erro) {
    // Falha de parse/acesso nao pode quebrar o Dashboard silenciosamente
    // (clean-code.md "Tratamento de erro") -- loga com contexto e cai pro
    // default, mesmo padrao de erro ja usado nos hooks de investimentos.
    console.error("Falha ao ler preferencia de widgets do localStorage", erro)
    return PREFERENCIA_PADRAO
  }
}

export function salvarPreferenciaWidgets(preferencia: PreferenciaWidgets): void {
  try {
    window.localStorage.setItem(CHAVE_STORAGE, JSON.stringify(preferencia))
  } catch (erro) {
    console.error("Falha ao salvar preferencia de widgets no localStorage", erro)
  }
}
