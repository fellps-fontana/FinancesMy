import { Landmark, MoreHorizontal, TrendingUp } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { Button } from "@/shared/ui/button"
import { IconTile } from "@/shared/ui/icon-tile"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarVariacaoPercentual } from "@/features/investimentos/lib/formatarVariacaoPercentual"
import { ROTULO_TIPO_ATIVO } from "@/features/investimentos/lib/rotularTipoAtivo"
import type { AtivoResponse, TipoAtivo } from "@/features/investimentos/types"

// Grade da tabela desktop (mockup s_investimentos: Ativo / Tipo / Conta /
// Valor / Evolucao + coluna de acao). Exportada para o cabecalho.
export const GRADE_COLUNAS_ATIVO =
  "md:grid-cols-[minmax(0,1.7fr)_minmax(0,1.1fr)_minmax(0,1.3fr)_minmax(0,1fr)_minmax(0,0.7fr)_44px]"

// Icone por tipo: Landmark (renda atrelada a emissor) e TrendingUp (variacao
// de mercado) - significado de dominio, nao enfeite.
const ICONE_POR_TIPO: Record<TipoAtivo, typeof Landmark> = {
  RendaFixa: Landmark,
  RendaVariavel: TrendingUp,
}

const ROTULO_SEM_CONTA = "Sem conta"

type AtivoLinhaProps = {
  ativo: AtivoResponse
  onVincularConta: (ativo: AtivoResponse) => void
  onAbrirAcoes: (ativo: AtivoResponse) => void
}

// Apresentacao pura de um ativo (valores e evolucao vem do backend). Desktop:
// linha da tabela com "Vincular conta" na coluna Conta quando `semConta` e
// botao de acoes; mobile: card inteiro abre a folha de acoes.
export function AtivoLinha({ ativo, onVincularConta, onAbrirAcoes }: AtivoLinhaProps) {
  const Icone = ICONE_POR_TIPO[ativo.tipo]
  const evolucaoCor = ativo.evolucaoPercentual >= 0 ? "text-positivo" : "text-negativo"
  const nomeConta = ativo.semConta ? ROTULO_SEM_CONTA : (ativo.contaNome ?? ROTULO_SEM_CONTA)

  return (
    <li
      className={cn(
        "relative rounded-[14px] border-[0.5px] border-border bg-card p-3.5",
        "md:grid md:items-center md:gap-0 md:rounded-none md:border-0 md:border-b-[0.5px] md:bg-transparent md:px-[22px] md:py-3.5 md:last:border-b-0",
        GRADE_COLUNAS_ATIVO,
      )}
    >
      <button
        type="button"
        aria-label={`Acoes de ${ativo.nome}`}
        onClick={() => onAbrirAcoes(ativo)}
        className="absolute inset-0 rounded-[14px] outline-none focus-visible:ring-3 focus-visible:ring-ring/50 md:hidden"
      />

      <div className="flex min-w-0 items-center gap-3">
        <IconTile>
          <Icone strokeWidth={1.6} className="size-4" aria-hidden="true" />
        </IconTile>
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm text-text-body">{ativo.nome}</div>
          <div className="truncate text-xs text-text-faint md:hidden">
            {ROTULO_TIPO_ATIVO[ativo.tipo]} · {nomeConta}
          </div>
        </div>
        <div className="shrink-0 text-right md:hidden">
          <div className="text-sm whitespace-nowrap text-text-primary">{formatarMoeda(ativo.valorAtual)}</div>
          <div className={cn("mt-0.5 text-xs", evolucaoCor)}>
            {formatarVariacaoPercentual(ativo.evolucaoPercentual)}
          </div>
        </div>
      </div>

      <span className="hidden truncate text-[13px] text-text-muted md:block">
        {ROTULO_TIPO_ATIVO[ativo.tipo]}
      </span>

      <div className="relative z-10 hidden min-w-0 md:block">
        {ativo.semConta ? (
          <button
            type="button"
            onClick={() => onVincularConta(ativo)}
            className="rounded-md text-left text-[13px] text-primary outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
          >
            <span className="text-text-faint">{ROTULO_SEM_CONTA}</span>
            {" · "}
            Vincular conta
          </button>
        ) : (
          <span className="block truncate text-[13px] text-text-muted">{nomeConta}</span>
        )}
      </div>

      <span className="hidden text-right text-sm whitespace-nowrap text-text-primary md:block">
        {formatarMoeda(ativo.valorAtual)}
      </span>

      <span className={cn("hidden text-right text-[13px] md:block", evolucaoCor)}>
        {formatarVariacaoPercentual(ativo.evolucaoPercentual)}
      </span>

      <div className="relative z-10 hidden justify-end md:flex">
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={`Acoes de ${ativo.nome}`}
          title="Acoes"
          onClick={() => onAbrirAcoes(ativo)}
        >
          <MoreHorizontal className="size-4 text-text-muted" aria-hidden="true" />
        </Button>
      </div>
    </li>
  )
}
