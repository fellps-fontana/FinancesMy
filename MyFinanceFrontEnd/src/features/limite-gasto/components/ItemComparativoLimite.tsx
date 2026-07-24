import { Card, CardContent } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

const formatadorPercentual = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  maximumFractionDigits: 0,
})

type ItemComparativoLimiteProps = {
  item: GastoVsLimiteResponse
}

// Componente de apresentacao puro: `gastoRealizado`, `valorLimite`,
// `percentualUtilizado` e `estourado` ja vem calculados do backend
// (regra-de-negocio.md item 14 - LimiteGastoCalculator/TASK-053/054). Este
// componente so exibe e decide a cor semantica, nunca recalcula a razao
// gasto/limite (ver clean-code.md "Organizacao (React)").
export function ItemComparativoLimite({ item }: ItemComparativoLimiteProps) {
  // Cor com significado (identidade-visual.md "Principios"): estourado e a
  // unica sinalizacao de alerta que a regra define para este comparativo (item
  // 14 - "SOMENTE alerta visual"), entao usa negativo; dentro do limite usa
  // positivo. Sem estado intermediario "perto do limite" porque a regra nao
  // define esse threshold - inventar um aqui seria calculo de dominio fora do
  // backend.
  const corBarra = item.estourado ? "bg-negativo" : "bg-positivo"
  const corPercentual = item.estourado ? "text-negativo" : "text-positivo"
  // A razao pode passar de 100% quando estourado (item 14): a largura da
  // barra e limitada visualmente a 100%, mas o percentual exibido ao lado
  // continua mostrando o valor real, sem esconder o quanto passou do limite.
  const larguraBarra = Math.min(item.percentualUtilizado * 100, 100)

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-2">
          <span className="text-sm text-text-body">{item.categoriaNome}</span>
          {item.estourado && (
            <span className="inline-flex items-center rounded-[5px] bg-negativo/15 px-2 py-0.5 text-[12px] font-medium text-negativo">
              Estourado
            </span>
          )}
        </div>

        <div className="h-1.5 w-full overflow-hidden rounded-full bg-accent">
          <div
            className={cn("h-full rounded-full", corBarra)}
            style={{ width: `${larguraBarra}%` }}
          />
        </div>

        <div className="flex items-center justify-between text-[12px] text-text-faint">
          <span>
            {formatarMoeda(item.gastoRealizado)} de {formatarMoeda(item.valorLimite)}
          </span>
          <span className={cn("font-medium", corPercentual)}>
            {formatadorPercentual.format(item.percentualUtilizado)}
          </span>
        </div>
      </CardContent>
    </Card>
  )
}
