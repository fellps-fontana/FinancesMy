import { Card, CardContent } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useGastoVsLimiteTodasCategorias } from "@/features/limite-gasto/hooks/useGastoVsLimiteTodasCategorias"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

// Formata a mesma fracao 0..1+ que o backend devolve em `percentualUtilizado`
// (ver LimiteGastoCalculator.cs: 0.5m = 50%, 1.5m = 150% quando estourado) -
// mesmo padrao ja usado em RelatorioCategoriaPage.tsx para `item.percentual`,
// que tambem chega como fracao pronta do backend, sem escala *100 embutida
// (diferente de formatarPercentual.ts de investimentos, feito pra um valor
// que ja vem multiplicado por 100 - escala incompativel aqui).
const formatadorPercentual = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  maximumFractionDigits: 0,
})

// Threshold de "perto do limite" (regra-de-negocio.md item 14 nao define
// esse corte - so define "estourar" como gasto > limite). Decisao de UX
// isolada aqui, igual ao types.ts do modulo ja avisa ("nenhum threshold e
// decidido no hook, so pelos consumidores"). TASK-060 (AvisoLimiteGasto) usa
// o mesmo valor 80% numa funcao propria (lib/limiarAlertaLimite.ts) porque
// vive noutra feature (lancamentos) - cada consumidor decide o proprio corte
// visual sem compartilhar constante entre features, para nao criar
// acoplamento por um numero que e so estetico, nao contrato de API.
const LIMIAR_PERTO_DO_LIMITE = 0.8

type EstadoLimite = "estourado" | "perto" | "ok"

function obterEstado(item: GastoVsLimiteResponse): EstadoLimite {
  if (item.estourado) return "estourado"
  if (item.percentualUtilizado >= LIMIAR_PERTO_DO_LIMITE) return "perto"
  return "ok"
}

// Cor semantica por estado (identidade-visual.md: "cor com significado").
// estourado -> negativo (mesmo vermelho-coral de saida/gasto, aqui reforcando
// "orcamento furado"). perto -> alerta (mesmo ambar de "pendente/atencao").
// ok -> positivo: nao ha token neutro de "sucesso" separado no documento, e
// "dentro do limite" e o analogo, no dominio de orcamento, do estado
// resolvido/tranquilo que positivo ja representa nos badges de status (ex:
// "pago" em ContaReceberItem) - reaproveitar em vez de inventar cor nova.
const CONFIG_POR_ESTADO: Record<EstadoLimite, { barra: string; texto: string; label: string }> = {
  estourado: { barra: "bg-negativo", texto: "text-negativo", label: "Limite estourado" },
  perto: { barra: "bg-alerta", texto: "text-alerta", label: "Perto do limite" },
  ok: { barra: "bg-positivo", texto: "text-positivo", label: "Dentro do limite" },
}

type LimiteGastoIndicadorProps = {
  ano: number
  mes: number
  className?: string
}

// Componente standalone (regra-de-negocio.md item 14: "Dashboard/resumo
// geral: indicador de progresso gasto/limite por categoria"). Escolha de
// design: recebe `ano`/`mes` como props e chama
// `useGastoVsLimiteTodasCategorias` internamente, em vez de receber a lista
// ja pronta via prop - mais simples pra quem for embutir este componente
// quando a pagina de Dashboard existir (TASK-059 nao constroi essa pagina):
// o consumidor so passa o periodo, sem precisar orquestrar o hook por fora.
// Continua sendo apresentacao pura no sentido que importa aqui - nenhum
// calculo de gasto/limite/percentual/estouro mora neste arquivo, tudo isso
// (`percentualUtilizado`, `estourado`) vem pronto do backend
// (LimiteGastoCalculator.cs); o unico "calculo" local e de UI (limitar a
// largura visual da barra a 100%, pra uma categoria 150% estourada nao
// vazar do card).
export function LimiteGastoIndicador({ ano, mes, className }: LimiteGastoIndicadorProps) {
  const { data, isLoading, isError } = useGastoVsLimiteTodasCategorias(ano, mes)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Limite de gasto por categoria</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando limites...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar os limites de gasto.</p>
        )}

        {!isLoading && !isError && (data === undefined || data.length === 0) && (
          <p className="text-sm text-text-muted">Nenhuma categoria com limite de gasto definido.</p>
        )}

        {!isLoading && !isError && data !== undefined && data.length > 0 && (
          <ul className="flex flex-col gap-3">
            {data.map((item) => (
              <LinhaLimiteGasto key={item.categoriaId} item={item} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

type LinhaLimiteGastoProps = {
  item: GastoVsLimiteResponse
}

function LinhaLimiteGasto({ item }: LinhaLimiteGastoProps) {
  const estado = obterEstado(item)
  const config = CONFIG_POR_ESTADO[estado]
  const larguraBarra = Math.min(item.percentualUtilizado, 1) * 100

  return (
    <li className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between gap-2">
        <span className="text-sm text-text-body">{item.categoriaNome}</span>
        <span className={cn("text-[12px] font-medium", config.texto)}>{config.label}</span>
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
        <span>{formatadorPercentual.format(item.percentualUtilizado)}</span>
      </div>
    </li>
  )
}
