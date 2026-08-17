import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { agruparRendimentosPorMes } from "@/features/investimentos/lib/agruparRendimentosPorMes"
import { useRendimentosResumo } from "@/features/investimentos/hooks/useRendimentosResumo"

const formatadorMesCurto = new Intl.DateTimeFormat("pt-BR", { month: "short", year: "2-digit" })

// Rotulo de eixo curto ("jul/26") a partir de "AAAA-MM". Parse manual dos
// componentes (mesmo motivo de features/cartao/lib/formatarData.ts: evita o
// deslocamento de fuso que `new Date(iso)` aplicaria) -- formatacao pura de
// apresentacao, nao calculo de dominio, por isso fica local ao componente em
// vez de lib/ (nao ha outro consumidor deste rotulo curto no projeto).
function formatarMesCurto(mesIso: string): string {
  const [ano, mes] = mesIso.split("-").map(Number)
  return formatadorMesCurto.format(new Date(ano, mes - 1, 1))
}

// Cor semantica por tipo de Rendimento (regra-de-negocio.md item 8.4;
// identidade-visual.md "cor com significado"). DIVIDENDO e um recebimento
// real declarado pelo usuario -> mesmo token "positivo" usado em qualquer
// entrada de dinheiro (identidade-visual.md "positivo -> entrada/
// recebimento/pago"). VALORIZACAO NAO e fluxo de caixa (item 8.4: "nao
// entra em nenhum calculo de saldo") -- e um dado calculado, categoria
// distinta do dividendo, por isso usa o roxo de acento (mesmo espirito de
// GraficoConsolidadoAtivos, que usa accent/accent-soft para distinguir
// CATEGORIA em vez de estado). O proprio valor pode ser negativo
// (desvalorizacao, item 8.4) -- o Recharts ja empilha a barra abaixo do eixo
// nesse caso, sem precisar de cor diferente para "perda".
const COR_DIVIDENDO = "var(--color-positivo)"
const COR_VALORIZACAO = "var(--color-accent)"

// Recharts entrega `nome`/`valor` dos formatters de Tooltip/Legend como
// string | number (tipo da propria lib, nao controlado por este projeto) --
// comparacao direta em vez de cast evita tipo dinamico solto (clean-code.md
// "Tipagem forte"), mesmo padrao ja usado em GraficoHistoricoAportes.tsx
// (`nome === "precoMedio"`).
function rotularSerie(chave: string | number | undefined): string {
  return chave === "dividendo" ? "Dividendo" : "Valorizacao"
}

// Grafico agregado (todos os ativos) de Rendimento por mes e por tipo
// (regra-de-negocio.md item 8.4). Apresentacao pura: busca via
// useRendimentosResumo (mesmo padrao de auto-fetch de GraficoEntradasSaidas
// e GraficoHistoricoAportes) e so exibe o agrupamento que
// agruparRendimentosPorMes ja calculou -- nenhuma soma/bucketizacao roda
// aqui.
export function GraficoRendimentosPorTipo() {
  const { data: resumo, isLoading, isError } = useRendimentosResumo()

  const pontos = resumo === undefined ? [] : agruparRendimentosPorMes(resumo.historico)

  return (
    <Card>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Rendimentos por mes</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando grafico...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar os rendimentos.</p>
        )}

        {!isLoading && !isError && pontos.length === 0 && (
          <p className="text-sm text-text-muted">
            Nenhum rendimento registrado ainda. Dividendos e valorizacao aparecem aqui conforme
            forem lancados.
          </p>
        )}

        {!isLoading && !isError && pontos.length > 0 && (
          <div className="h-[240px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={pontos} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" vertical={false} />
                <XAxis
                  dataKey="mes"
                  tickFormatter={formatarMesCurto}
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "var(--color-text-muted)", fontSize: 12 }}
                />
                <YAxis
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: "var(--color-text-muted)", fontSize: 12 }}
                  tickFormatter={formatarMoeda}
                  width={72}
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
                  labelFormatter={(valor) => formatarMesCurto(String(valor))}
                  formatter={(valor, nome) => [formatarMoeda(Number(valor)), rotularSerie(nome)]}
                />
                <Legend
                  formatter={(valor) => rotularSerie(valor)}
                  wrapperStyle={{ fontSize: 12, color: "var(--color-text-muted)" }}
                />
                <Bar dataKey="dividendo" name="dividendo" stackId="rendimentos" fill={COR_DIVIDENDO} radius={[4, 4, 0, 0]} />
                <Bar dataKey="valorizacao" name="valorizacao" stackId="rendimentos" fill={COR_VALORIZACAO} radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
