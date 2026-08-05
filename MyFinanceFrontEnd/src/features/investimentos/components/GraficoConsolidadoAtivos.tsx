import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarPercentual } from "@/features/investimentos/lib/formatarPercentual"
import { obterResumoPorTipo } from "@/features/investimentos/lib/obterResumoPorTipo"
import type { AtivosResumoResponse, TipoAtivoStorage } from "@/features/investimentos/types"

type GraficoConsolidadoAtivosProps = {
  resumo: AtivosResumoResponse | undefined
  carregando: boolean
}

const LABEL_POR_TIPO: Record<TipoAtivoStorage, string> = {
  RENDA_FIXA: "Renda fixa",
  RENDA_VARIAVEL: "Renda variavel",
}

// Roxo em dois tons (identidade-visual.md: "roxo = acao/categoria"). Aqui a
// distincao e de CATEGORIA (renda fixa x renda variavel), nao de estado
// (pago/pendente) nem de sinal (entrada/saida) -- por isso NAO usa
// positivo/negativo/alerta, que carregam outro significado de dominio (ver
// identidade-visual.md "Cores - acento e semantica"). --color-primary e o
// roxo de acao/destaque; --color-accent-soft e o tom claro do mesmo grupo --
// juntos distinguem as duas fatias sem sair dos tokens definidos.
const COR_POR_TIPO: Record<TipoAtivoStorage, string> = {
  RENDA_FIXA: "var(--color-primary)",
  RENDA_VARIAVEL: "var(--color-accent-soft)",
}

const TIPOS: TipoAtivoStorage[] = ["RENDA_FIXA", "RENDA_VARIAVEL"]

type FatiaGrafico = {
  tipo: TipoAtivoStorage
  label: string
  valorAtual: number
  percentualDaCarteira: number
}

// So le o que o backend ja calculou em resumo.porTipo -- nenhum percentual
// ou total e recalculado aqui (regra-de-negocio.md item 8; percentualDaCarteira
// ja vem pronto de Services/AtivoService.cs ObterResumo). obterResumoPorTipo
// devolve zerado quando a carteira nao tem nenhum ativo daquele tipo, mesma
// regra ja usada por ResumoAtivosCards.
function montarFatias(resumo: AtivosResumoResponse | undefined): FatiaGrafico[] {
  return TIPOS.map((tipo) => {
    const item = obterResumoPorTipo(resumo, tipo)
    return {
      tipo,
      label: LABEL_POR_TIPO[tipo],
      valorAtual: item.valorAtual,
      percentualDaCarteira: item.percentualDaCarteira,
    }
  })
}

// Componente de apresentacao (burro): grafico consolidado RENDA_FIXA x
// RENDA_VARIAVEL da tela "Investimentos" (regra-de-negocio.md item 8,
// "Resumo por tipo"), complementar aos cards numericos de
// ResumoAtivosCards.tsx -- mesma fonte de dados (useResumoAtivos na pagina),
// so a forma de exibicao muda. Sem calculo de dominio: valorAtual e
// percentualDaCarteira sao repassados como o backend entrega.
export function GraficoConsolidadoAtivos({ resumo, carregando }: GraficoConsolidadoAtivosProps) {
  const fatias = montarFatias(resumo)
  const temDados = fatias.some((fatia) => fatia.valorAtual > 0)

  return (
    <Card>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Distribuicao da carteira</span>

        {carregando && <p className="text-sm text-text-muted">Carregando grafico...</p>}

        {!carregando && !temDados && (
          <p className="text-sm text-text-muted">
            Nenhum ativo cadastrado ainda para compor o grafico.
          </p>
        )}

        {!carregando && temDados && (
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
            <div className="h-[180px] w-full sm:w-1/2">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={fatias}
                    dataKey="valorAtual"
                    nameKey="label"
                    innerRadius={48}
                    outerRadius={72}
                    paddingAngle={2}
                    stroke="none"
                  >
                    {fatias.map((fatia) => (
                      <Cell key={fatia.tipo} fill={COR_POR_TIPO[fatia.tipo]} />
                    ))}
                  </Pie>
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "var(--color-card)",
                      border: "0.5px solid var(--color-border)",
                      borderRadius: 8,
                      color: "var(--color-text-body)",
                      fontSize: 13,
                    }}
                    labelStyle={{ color: "var(--color-text-muted)" }}
                    formatter={(valor) => formatarMoeda(Number(valor))}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>

            <ul className="flex flex-1 flex-col gap-2">
              {fatias.map((fatia) => (
                <li key={fatia.tipo} className="flex items-center justify-between gap-2 text-sm">
                  <span className="flex items-center gap-2 text-text-body">
                    <span
                      className="size-2.5 shrink-0 rounded-full"
                      style={{ backgroundColor: COR_POR_TIPO[fatia.tipo] }}
                      aria-hidden="true"
                    />
                    {fatia.label}
                  </span>
                  <span className="flex flex-col items-end">
                    <span className="font-medium text-text-primary">
                      {formatarMoeda(fatia.valorAtual)}
                    </span>
                    <span className="text-[12px] text-text-faint">
                      {formatarPercentual(fatia.percentualDaCarteira)}
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
