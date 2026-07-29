import { Card, CardContent } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { useUltimosLancamentos } from "@/features/dashboard/hooks/useUltimosLancamentos"
import type {
  LancamentoResponse,
  StatusLancamento,
  TipoLancamento,
} from "@/features/lancamentos/types"

// Mesma tabela cor/sinal de LancamentoItem.tsx (regra-de-negocio.md item 2,
// CRITICA: "usar SEMPRE o campo tipo... Nunca somar valor cru") - duplicada
// aqui, em vez de importar LancamentoItem inteiro, porque LancamentoItem e
// um item de LISTA COMPLETA com mutations (marcar pago/editar/remover, ver
// LancamentoItem.tsx) - carga pesada demais pra uma previa do dashboard
// so-leitura. O que se reusa e o PADRAO VISUAL (tokens de cor/badge), nao o
// componente com efeito colateral acoplado.
const CONFIG_POR_TIPO: Record<TipoLancamento, { corValor: string; sinal: string }> = {
  DEBIT: { corValor: "text-negativo", sinal: "-" },
  CREDIT: { corValor: "text-positivo", sinal: "+" },
}

// Status (regra-de-negocio.md item 5): mesmas 3 cores de LancamentoItem.tsx
// ("pago -> positivo; pendente -> alerta; sugerido -> accent/roxo real via
// token --primary", identidade-visual.md).
const CONFIG_POR_STATUS: Record<StatusLancamento, { label: string; className: string }> = {
  PENDENTE: { label: "Pendente", className: "bg-alerta/15 text-alerta" },
  SUGERIDO: { label: "Sugerido", className: "bg-primary/15 text-primary" },
  PAGO: { label: "Pago", className: "bg-positivo/15 text-positivo" },
}

const QUANTIDADE_EXIBIDA = 5

type UltimosLancamentosProps = {
  className?: string
}

/**
 * Widget "Ultimos lancamentos" (mockup "02 Dashboard.dc.html", secao de
 * mesmo nome) - previa SOMENTE LEITURA dos lancamentos mais recentes do
 * fluxo de caixa (regra-de-negocio.md item 12), agregados entre as contas
 * BANCO do usuario (ver useUltimosLancamentos.ts). Sem acoes de editar/
 * marcar como pago/remover aqui - isso continua exclusivo da tela
 * /lancamentos (LancamentoItem.tsx); este widget e so um atalho visual do
 * que aconteceu por ultimo.
 *
 * Mesmo padrao estrutural de CardSaldoProjetado/LimiteGastoIndicador: Card +
 * label + estado de loading/erro/vazio + lista. Container puro - nenhum
 * calculo de dominio mora aqui, so leitura de LancamentoResponse ja pronto e
 * formatacao (formatarMoeda/formatarData).
 *
 * Garantia de contraste (achado previo repassado por Kira: bug de hover
 * ilegivel relatado, mas o widget simplesmente nao existia ainda no
 * Dashboard): este componente NAO usa nenhuma classe `hover:` - os itens da
 * lista nao sao interativos/clicaveis nesta leva, entao nao ha estado de
 * hover pra repetir o bug relatado em nenhum lugar deste arquivo.
 */
export function UltimosLancamentos({ className }: UltimosLancamentosProps) {
  const { data, isLoading, isError } = useUltimosLancamentos(QUANTIDADE_EXIBIDA)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Ultimos lancamentos</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando lancamentos...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar os ultimos lancamentos.</p>
        )}

        {!isLoading && !isError && (data === undefined || data.length === 0) && (
          <p className="text-sm text-text-muted">Nenhum lancamento registrado ainda.</p>
        )}

        {!isLoading && !isError && data !== undefined && data.length > 0 && (
          <ul className="flex flex-col gap-3">
            {data.map((lancamento) => (
              <LinhaUltimoLancamento key={lancamento.id} lancamento={lancamento} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

type LinhaUltimoLancamentoProps = {
  lancamento: LancamentoResponse
}

function LinhaUltimoLancamento({ lancamento }: LinhaUltimoLancamentoProps) {
  const tipoConfig = CONFIG_POR_TIPO[lancamento.tipo]
  const statusConfig = CONFIG_POR_STATUS[lancamento.status]

  return (
    <li className="flex items-center justify-between gap-3">
      <div className="flex min-w-0 flex-col gap-0.5">
        <span className="truncate text-sm text-text-body">
          {lancamento.descricao ?? "Sem descricao"}
        </span>
        <span className="text-[12px] text-text-faint">{formatarData(lancamento.data)}</span>
      </div>

      <div className="flex shrink-0 flex-col items-end gap-1">
        <span className={cn("text-sm font-medium", tipoConfig.corValor)}>
          {tipoConfig.sinal} {formatarMoeda(lancamento.valor)}
        </span>
        <span
          className={cn(
            "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[11px] font-medium",
            statusConfig.className,
          )}
        >
          {statusConfig.label}
        </span>
      </div>
    </li>
  )
}
