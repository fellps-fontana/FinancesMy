import { useState } from "react"
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { useCotacaoHistorico } from "@/features/investimentos/hooks/useCotacaoHistorico"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { Button } from "@/shared/ui/button"

type GraficoCotacaoAtivoProps = {
  ticker: string
}

type RangeCotacao = "1mo" | "3mo" | "6mo"

const OPCOES_RANGE: { valor: RangeCotacao; rotulo: string }[] = [
  { valor: "1mo", rotulo: "1m" },
  { valor: "3mo", rotulo: "3m" },
  { valor: "6mo", rotulo: "6m" },
]

const formatadorDataEixo = new Intl.DateTimeFormat("pt-BR", {
  day: "2-digit",
  month: "2-digit",
})

// A API externa (Brapi, ver stack.md) devolve a data em ISO ("AAAA-MM-DD" ou
// com horario). new Date(string-iso-sem-horario) e interpretado como UTC e
// pode exibir o dia anterior no fuso local - por isso os componentes
// (ano/mes/dia) sao extraidos antes de montar o Date local, em vez de deixar
// o parser reinterpretar o fuso.
function formatarDataPonto(data: string): string {
  const [ano, mes, dia] = data.slice(0, 10).split("-").map(Number)
  if (!ano || !mes || !dia) {
    return data
  }
  return formatadorDataEixo.format(new Date(ano, mes - 1, dia))
}

// Grafico de historico de cotacao (regra-de-negocio.md item 8 e "Escopo: v1
// vs v2"): busca SOB DEMANDA via useCotacaoHistorico (enabled: Boolean(ticker)
// no hook), sem sync/polling. Este componente so e montado quando o usuario
// abre o grafico do ativo (ver toggle em ListaAtivos.tsx) - fechar o toggle
// desmonta o componente e a query nao roda mais em background.
// Nao compara com preco_medio nem calcula rentabilidade/variacao - isso e v2
// (ver "Escopo: v1 vs v2"). A linha usa --color-primary (roxo de acao/
// destaque da identidade visual): a serie e neutra, so cotacao no tempo, sem
// significado de ganho/perda a comunicar aqui.
export function GraficoCotacaoAtivo({ ticker }: GraficoCotacaoAtivoProps) {
  const [range, setRange] = useState<RangeCotacao>("1mo")
  const { data, isLoading, error } = useCotacaoHistorico(ticker, range)

  // Log com contexto (ticker/range) antes de qualquer mensagem generica ao
  // usuario - a API externa pode falhar com 502/504/404 e o detalhe tecnico
  // do erro nao deve vazar para a tela (clean-code.md "Tratamento de erro").
  if (error) {
    console.error(`Falha ao carregar cotacao historica - ticker=${ticker}, range=${range}`, error)
  }

  return (
    <div className="flex flex-col gap-2 rounded-xl border border-border bg-card px-3 py-3">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[12px] text-muted-foreground">Cotacao de {ticker}</span>
        <div className="flex gap-1">
          {OPCOES_RANGE.map((opcao) => (
            <Button
              key={opcao.valor}
              type="button"
              variant={range === opcao.valor ? "secondary" : "ghost"}
              size="xs"
              onClick={() => setRange(opcao.valor)}
              aria-pressed={range === opcao.valor}
            >
              {opcao.rotulo}
            </Button>
          ))}
        </div>
      </div>

      {isLoading ? (
        <p className="text-[13px] text-muted-foreground">Carregando cotacao...</p>
      ) : error ? (
        <p className="text-[13px] text-muted-foreground">
          Nao foi possivel carregar a cotacao agora. Tente novamente mais tarde.
        </p>
      ) : !data || data.pontos.length === 0 ? (
        <p className="text-[13px] text-muted-foreground">
          Sem cotacao disponivel para este periodo.
        </p>
      ) : (
        <div className="h-44 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={data.pontos} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
              <CartesianGrid stroke="var(--color-border)" strokeDasharray="3 3" vertical={false} />
              <XAxis
                dataKey="data"
                tickFormatter={formatarDataPonto}
                tick={{ fill: "var(--color-muted-foreground)", fontSize: 11 }}
                axisLine={{ stroke: "var(--color-border)" }}
                tickLine={false}
                minTickGap={24}
              />
              <YAxis
                tickFormatter={(valor: number) => formatarMoeda(valor)}
                tick={{ fill: "var(--color-muted-foreground)", fontSize: 11 }}
                axisLine={false}
                tickLine={false}
                width={76}
              />
              <Tooltip
                formatter={(valor) => [formatarMoeda(Number(valor)), "Preco"]}
                labelFormatter={(rotulo) => formatarDataPonto(String(rotulo))}
                contentStyle={{
                  backgroundColor: "var(--color-popover)",
                  borderColor: "var(--color-border)",
                  borderRadius: "8px",
                  fontSize: 12,
                }}
                labelStyle={{ color: "var(--color-muted-foreground)" }}
                itemStyle={{ color: "var(--color-foreground)" }}
              />
              <Line
                type="monotone"
                dataKey="preco"
                stroke="var(--color-primary)"
                strokeWidth={2}
                dot={false}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
