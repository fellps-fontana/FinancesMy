import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

const formatadorPercentual = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  maximumFractionDigits: 0,
})

// Threshold de "perto do limite" (regra-de-negocio.md item 14 nao define
// esse corte - so define "estourar" como gasto > limite). Mesmo valor e
// mesmo raciocinio ja usados em dashboard/components/LimiteGastoIndicador.tsx
// e lancamentos/lib/limiarAlertaLimite.ts: cada consumidor decide o proprio
// corte visual sem compartilhar constante entre features, para nao acoplar
// por um numero que e so estetico, nao contrato de API.
const LIMIAR_PERTO_DO_LIMITE = 0.8

type EstadoLimite = "estourado" | "perto" | "ok"

function obterEstado(item: GastoVsLimiteResponse): EstadoLimite {
  if (item.estourado) return "estourado"
  if (item.percentualUtilizado >= LIMIAR_PERTO_DO_LIMITE) return "perto"
  return "ok"
}

type ConfigEstado = {
  dot: string
  barra: string
  texto: string
  badge: string
  label: string | null
}

// Cor com significado (identidade-visual.md "Principios"): estourado ->
// negativo (mesmo vermelho-coral de saida/gasto, reforcando "orcamento
// furado"); perto -> alerta (mesmo ambar de "pendente/atencao"); ok ->
// positivo (dentro do orcamento, analogo ao estado resolvido/tranquilo que
// positivo ja representa nos badges de status do dominio, ex:
// ContaReceberItem). Mesmo mapeamento de LimiteGastoIndicador.tsx (dashboard),
// para o widget e a tela cheia falarem a mesma linguagem visual.
const CONFIG_POR_ESTADO: Record<EstadoLimite, ConfigEstado> = {
  estourado: {
    dot: "bg-negativo",
    barra: "bg-negativo",
    texto: "text-negativo",
    badge: "bg-negativo/15 text-negativo",
    label: "Estourado",
  },
  perto: {
    dot: "bg-alerta",
    barra: "bg-alerta",
    texto: "text-alerta",
    badge: "bg-alerta/15 text-alerta",
    label: "Perto do limite",
  },
  ok: {
    dot: "bg-positivo",
    barra: "bg-positivo",
    texto: "text-positivo",
    badge: "",
    label: null,
  },
}

type ItemComparativoLimiteProps = {
  item: GastoVsLimiteResponse
  /**
   * Verdadeiro quando esta categoria e o alvo do deep-link `?categoriaId=`
   * (ver ComparativoLimiteGastoPage.tsx). So aplica destaque visual - nao
   * muda nenhum valor exibido.
   */
  destacado?: boolean
}

// Componente de apresentacao puro: `gastoRealizado`, `valorLimite`,
// `percentualUtilizado` e `estourado` ja vem calculados do backend
// (regra-de-negocio.md item 14 - LimiteGastoCalculator/TASK-053/054). Este
// componente so exibe e decide a cor/label semantica, nunca recalcula a razao
// gasto/limite (ver clean-code.md "Organizacao (React)").
export function ItemComparativoLimite({ item, destacado = false }: ItemComparativoLimiteProps) {
  const estado = obterEstado(item)
  const config = CONFIG_POR_ESTADO[estado]
  // A razao pode passar de 100% quando estourado (item 14): a largura da
  // barra e limitada visualmente a 100%, mas o percentual exibido ao lado
  // continua mostrando o valor real, sem esconder o quanto passou do limite.
  const larguraBarra = Math.min(item.percentualUtilizado * 100, 100)

  return (
    <li
      className={cn(
        "-mx-3 flex flex-col gap-2 rounded-[12px] px-3 py-2.5",
        destacado && "bg-accent-deep/30 ring-1 ring-accent",
      )}
    >
      <div className="flex items-center gap-2.5">
        <span className={cn("h-2.5 w-2.5 shrink-0 rounded-[3px]", config.dot)} aria-hidden="true" />
        <span className="flex-1 truncate text-sm text-text-body">{item.categoriaNome}</span>
        {config.label && (
          <span
            className={cn(
              "inline-flex shrink-0 items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
              config.badge,
            )}
          >
            {config.label}
          </span>
        )}
      </div>

      <div className="h-1.5 w-full overflow-hidden rounded-full bg-accent">
        <div
          className={cn("h-full rounded-full", config.barra)}
          style={{ width: `${larguraBarra}%` }}
        />
      </div>

      <div className="flex items-center justify-between text-[12px] text-text-faint">
        <span>
          {formatarMoeda(item.gastoRealizado)} de {formatarMoeda(item.valorLimite)}
        </span>
        <span className={cn("font-medium", config.texto)}>
          {formatadorPercentual.format(item.percentualUtilizado)}
        </span>
      </div>
    </li>
  )
}
