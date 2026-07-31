import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useHistoricoAportes } from "@/features/investimentos/hooks/useHistoricoAportes"
import type { AtivoAporteResponse } from "@/features/investimentos/types"

// Ponto do grafico: quantidade acumulada e preco medio, os dois calculados
// SO a partir dos proprios aportes do usuario (regra-de-negocio.md item 8.1)
// - nunca cotacao de mercado (item 8, proibido em todas as fases da v1).
type PontoHistorico = {
  data: string
  quantidadeAcumulada: number
  precoMedio: number
}

// Preco medio por media ponderada (regra-de-negocio.md item 8.1):
// preco_medio = valor_investido_acumulado / quantidade_acumulada. Aportes
// chegam do backend sem garantia de ordem -> ordena por data antes de
// acumular, senao a serie do grafico anda pra tras.
function calcularSerieHistorico(aportes: AtivoAporteResponse[]): PontoHistorico[] {
  const ordenados = [...aportes].sort((a, b) => a.data.localeCompare(b.data))

  let quantidadeAcumulada = 0
  let valorInvestidoAcumulado = 0

  return ordenados.map((aporte) => {
    quantidadeAcumulada += aporte.quantidade
    valorInvestidoAcumulado += aporte.valorTotal

    return {
      data: aporte.data,
      quantidadeAcumulada,
      precoMedio: quantidadeAcumulada === 0 ? 0 : valorInvestidoAcumulado / quantidadeAcumulada,
    }
  })
}

type GraficoHistoricoAportesProps = {
  ativoId: string
  className?: string
}

// Grafico do historico de aportes de UM ativo (regra-de-negocio.md item 8.1:
// "base do grafico de aportes por ativo"). Apresentacao pura: busca via
// useHistoricoAportes e so exibe quantidade acumulada + preco medio
// derivados dos aportes reais - sem serie de cotacao de mercado.
export function GraficoHistoricoAportes({ ativoId, className }: GraficoHistoricoAportesProps) {
  const { data: aportes, isLoading, isError } = useHistoricoAportes(ativoId)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Historico de aportes</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando historico...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar o historico de aportes.</p>
        )}

        {!isLoading && !isError && (aportes === undefined || aportes.length === 0) && (
          <p className="text-sm text-text-muted">Nenhum aporte registrado ainda para este ativo.</p>
        )}

        {!isLoading && !isError && aportes !== undefined && aportes.length > 0 && (
          <div className="h-[240px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart
                data={calcularSerieHistorico(aportes)}
                margin={{ top: 8, right: 8, left: 8, bottom: 0 }}
              >
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" vertical={false} />
                <XAxis
                  dataKey="data"
                  tickFormatter={formatarData}
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "var(--color-text-muted)", fontSize: 12 }}
                />
                <YAxis
                  yAxisId="quantidade"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "var(--color-text-muted)", fontSize: 12 }}
                  width={48}
                />
                <YAxis
                  yAxisId="precoMedio"
                  orientation="right"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "var(--color-text-muted)", fontSize: 12 }}
                  tickFormatter={formatarMoeda}
                  width={72}
                />
                <Tooltip
                  cursor={{ stroke: "var(--color-border)" }}
                  contentStyle={{
                    backgroundColor: "var(--color-card)",
                    border: "0.5px solid var(--color-border)",
                    borderRadius: 8,
                    color: "var(--color-text-body)",
                    fontSize: 13,
                  }}
                  labelStyle={{ color: "var(--color-text-muted)" }}
                  labelFormatter={(valor) => formatarData(String(valor))}
                  formatter={(valor, nome) =>
                    nome === "precoMedio"
                      ? [formatarMoeda(Number(valor)), "Preco medio"]
                      : [Number(valor), "Quantidade acumulada"]
                  }
                />
                <Line
                  yAxisId="quantidade"
                  type="monotone"
                  dataKey="quantidadeAcumulada"
                  stroke="var(--color-accent)"
                  strokeWidth={2}
                  dot={{ r: 3, fill: "var(--color-accent)" }}
                  activeDot={{ r: 4 }}
                />
                <Line
                  yAxisId="precoMedio"
                  type="monotone"
                  dataKey="precoMedio"
                  stroke="var(--color-accent-soft)"
                  strokeWidth={2}
                  dot={{ r: 3, fill: "var(--color-accent-soft)" }}
                  activeDot={{ r: 4 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
