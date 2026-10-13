import { ArrowDown, ArrowUp } from "lucide-react"
import { Card, CardContent } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useProjecaoMes } from "@/features/dashboard/hooks/useProjecaoMes"
import type { ProjecaoMesResponse } from "@/features/dashboard/types"

// Cor semantica do saldo (identidade-visual.md: "cor com significado").
// saldo >= 0 -> positivo (mesmo verde de entrada/recebimento), saldo < 0 ->
// negativo (mesmo coral de saida/gasto). So decide o TOKEN a partir do sinal
// que ja vem pronto do backend (regra-de-negocio.md item 9) - nenhuma soma
// ou calculo de projecao mora aqui, so a escolha de cor, igual
// LimiteGastoIndicador.tsx faz com `obterEstado`.
function obterCorSaldo(saldoProjetado: number): "positivo" | "negativo" {
  return saldoProjetado >= 0 ? "positivo" : "negativo"
}

type Termo = {
  label: string
  valor: number
}

function obterTermos(data: ProjecaoMesResponse): Termo[] {
  return [
    { label: "Recebido no mes", valor: data.totalRecebidoNoMes },
    { label: "A receber (esperado)", valor: data.totalAReceberEsperadoNoMes },
    { label: "Pago no mes", valor: data.totalPagoNoMes },
    { label: "A pagar (esperado)", valor: data.totalAPagarNoMes },
  ]
}

type CardSaldoProjetadoProps = {
  ano: number
  mes: number
  className?: string
}

// Componente standalone (regra-de-negocio.md item 9: "Projecao do mes"),
// mesmo padrao de LimiteGastoIndicador.tsx: recebe `ano`/`mes` via props e
// chama `useProjecaoMes` internamente, em vez de receber a resposta ja
// pronta - quem for embutir este card na pagina de Dashboard so passa o
// periodo. Apresentacao pura: os 5 valores (saldoProjetado e os 4 termos) ja
// vem prontos do backend (ProjecaoMesResponse) - nenhuma soma/subtracao de
// dominio mora aqui, so formatacao (formatarMoeda) e escolha de cor
// (obterCorSaldo).
export function CardSaldoProjetado({ ano, mes, className }: CardSaldoProjetadoProps) {
  const { data, isLoading, isError } = useProjecaoMes(ano, mes)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Saldo projetado do mes</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando projecao...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar a projecao do mes.</p>
        )}

        {!isLoading && !isError && data === undefined && (
          <p className="text-sm text-text-muted">Nenhuma projecao disponivel para o periodo.</p>
        )}

        {!isLoading && !isError && data !== undefined && (
          <>
            <span
              className={cn(
                "text-[28px] font-medium",
                obterCorSaldo(data.saldoProjetado) === "positivo" ? "text-positivo" : "text-negativo",
              )}
            >
              {formatarMoeda(data.saldoProjetado)}
            </span>

            <dl className="grid grid-cols-2 gap-x-4 gap-y-2">
              {obterTermos(data).map((termo) => (
                <div key={termo.label} className="flex flex-col gap-0.5">
                  <dt className="text-[12px] text-text-muted">{termo.label}</dt>
                  <dd className="text-sm text-text-body">{formatarMoeda(termo.valor)}</dd>
                </div>
              ))}
            </dl>
          </>
        )}
      </CardContent>
    </Card>
  )
}

type CardMovimentoMesProps = {
  tipo: "entradas" | "saidas"
  ano: number
  mes: number
  className?: string
}

// Configuracao visual por lado (identidade-visual.md: verde = entrada, coral
// = saida). `escolherValor` so SELECIONA qual dos totais ja prontos do
// backend exibir (regra-de-negocio.md item 9, `totalRecebidoNoMes` e
// `totalPagoNoMes`) - nenhuma soma ou projecao mora aqui.
const CONFIG_MOVIMENTO = {
  entradas: {
    label: "Entradas",
    Icone: ArrowUp,
    corTexto: "text-positivo",
    escolherValor: (data: ProjecaoMesResponse) => data.totalRecebidoNoMes,
  },
  saidas: {
    label: "Saidas",
    Icone: ArrowDown,
    corTexto: "text-negativo",
    escolherValor: (data: ProjecaoMesResponse) => data.totalPagoNoMes,
  },
} as const

// Cards "Entradas" / "Saidas" do topo do dashboard (desktop) e "Resumo do
// mes" (mobile), mockups 02 Dashboard. Mesma query de CardSaldoProjetado
// (useProjecaoMes compartilha cache pela query key), entao nao ha chamada
// extra ao backend. Rotulo colorido e o unico uso de cor semantica aqui, em
// cima de fundo de card - o valor segue em text-primary.
export function CardMovimentoMes({ tipo, ano, mes, className }: CardMovimentoMesProps) {
  const { data, isLoading, isError } = useProjecaoMes(ano, mes)
  const { label, Icone, corTexto, escolherValor } = CONFIG_MOVIMENTO[tipo]

  return (
    <Card
      className={cn(
        "gap-0 rounded-xl py-3.5 [--card-spacing:--spacing(3.5)] md:rounded-2xl md:py-[22px] md:[--card-spacing:22px]",
        className,
      )}
    >
      <CardContent className="flex flex-col gap-1.5 md:gap-2">
        <span className={cn("flex items-center gap-1.5 text-[12px] md:text-[13px]", corTexto)}>
          <Icone className="size-3.5" strokeWidth={2} aria-hidden="true" />
          {label}
        </span>

        {isLoading && <span className="text-sm text-text-muted">Carregando...</span>}
        {isError && <span className="text-sm text-negativo">Indisponivel</span>}
        {!isLoading && !isError && data !== undefined && (
          <span className="text-[19px] font-medium text-text-primary">
            {formatarMoeda(escolherValor(data))}
          </span>
        )}
      </CardContent>
    </Card>
  )
}
