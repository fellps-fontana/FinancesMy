import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis } from "recharts"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useProjecaoMes } from "@/features/dashboard/hooks/useProjecaoMes"
import type { ProjecaoMesResponse } from "@/features/dashboard/types"

// Agrupamento de EXIBICAO, nao regra de negocio nova (regra-de-negocio.md
// item 9). O backend entrega 4 termos separados (recebido/a receber,
// pago/a pagar) porque cada um tem leitura propria no card detalhado
// (CardSaldoProjetado). Aqui, pro grafico comparativo, os 4 termos sao
// agrupados em 2 barras -- soma trivial de 2 numeros ja prontos, nunca
// recalculando saldoProjetado.
type BarraGrafico = {
  categoria: string
  valor: number
  tipo: "positivo" | "negativo"
}

function obterBarras(data: ProjecaoMesResponse): BarraGrafico[] {
  return [
    {
      categoria: "Entradas",
      valor: data.totalRecebidoNoMes + data.totalAReceberEsperadoNoMes,
      tipo: "positivo",
    },
    {
      categoria: "Saidas",
      valor: data.totalPagoNoMes + data.totalAPagarNoMes,
      tipo: "negativo",
    },
  ]
}

// Cor semantica por barra (identidade-visual.md: "cor com significado").
// Mesmos tokens de CardSaldoProjetado.tsx, aqui como var() de CSS pois o
// Recharts pinta via SVG `fill`, nao aceita className do Tailwind.
const CORES: Record<BarraGrafico["tipo"], string> = {
  positivo: "var(--color-positivo)",
  negativo: "var(--color-negativo)",
}

type GraficoEntradasSaidasProps = {
  ano: number
  mes: number
  className?: string
}

// Componente standalone (regra-de-negocio.md item 9: "Projecao do mes"),
// mesmo padrao de CardSaldoProjetado.tsx: recebe `ano`/`mes` via props e
// chama `useProjecaoMes` internamente. Apresentacao pura: os 4 termos ja vem
// prontos do backend, o componente so agrupa em par (exibicao) e formata.
export function GraficoEntradasSaidas({ ano, mes, className }: GraficoEntradasSaidasProps) {
  const { data, isLoading, isError } = useProjecaoMes(ano, mes)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Entradas x saidas do mes</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando grafico...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar o grafico do mes.</p>
        )}

        {!isLoading && !isError && data === undefined && (
          <p className="text-sm text-text-muted">Nenhum dado disponivel para o periodo.</p>
        )}

        {!isLoading && !isError && data !== undefined && (
          <div className="h-[220px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={obterBarras(data)} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
                <XAxis
                  dataKey="categoria"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "var(--color-text-muted)", fontSize: 12 }}
                />
                <Tooltip
                  cursor={{ fill: "var(--color-muted)" }}
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
                <Bar dataKey="valor" radius={[8, 8, 0, 0]} maxBarSize={64}>
                  {obterBarras(data).map((barra) => (
                    <Cell key={barra.categoria} fill={CORES[barra.tipo]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
