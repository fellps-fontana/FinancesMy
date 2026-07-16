import type { FormEvent } from "react"
import { Landmark, TrendingUp } from "lucide-react"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarVariacaoPercentual } from "@/features/investimentos/lib/formatarVariacaoPercentual"
import { cn } from "@/shared/lib/utils"
import type { AtivoResponse, TipoAtivo } from "@/features/investimentos/types"

const LABEL_POR_TIPO: Record<TipoAtivo, string> = {
  RendaFixa: "Renda fixa",
  RendaVariavel: "Renda variavel",
}

// Icone por tipo (identidade-visual.md: "Icone de status/categoria: quadrado
// arredondado 34px com icone dentro"). RendaFixa usa um icone de instituicao
// (Landmark - renda atrelada a um emissor/banco); RendaVariavel usa um icone
// de variacao de mercado (TrendingUp) - significado de dominio, nao enfeite.
const ICONE_POR_TIPO: Record<TipoAtivo, typeof Landmark> = {
  RendaFixa: Landmark,
  RendaVariavel: TrendingUp,
}

type AtivoCardProps = {
  ativo: AtivoResponse
  editandoValor: boolean
  novoValorAtual: string
  salvandoValor: boolean
  erroValor: string | null
  onIniciarEdicaoValor: () => void
  onNovoValorAtualChange: (value: string) => void
  onSubmitValor: (event: FormEvent<HTMLFormElement>) => void
  onCancelarEdicaoValor: () => void
  confirmandoDesativar: boolean
  desativando: boolean
  erroDesativar: string | null
  onSolicitarDesativar: () => void
  onConfirmarDesativar: () => void
  onCancelarDesativar: () => void
}

// Componente de apresentacao (burro): uma linha da lista de ativos do mockup
// "11 Investimentos" (icone por tipo, nome, valor atual, evolucao colorida
// verde/vermelho conforme sinal). Estado de edicao/desativacao ja resolvido
// pelo container (AtivoItem) - so callbacks repassados, mesma divisao de
// responsabilidade de ContaInvestimentoCard/ContaInvestimentoItem
// (clean-code.md "Organizacao (React)"). Sem sparkline (regra-de-negocio.md
// item 8, "Pendencias a definir": nao ha historico de valor_atual na v1).
export function AtivoCard({
  ativo,
  editandoValor,
  novoValorAtual,
  salvandoValor,
  erroValor,
  onIniciarEdicaoValor,
  onNovoValorAtualChange,
  onSubmitValor,
  onCancelarEdicaoValor,
  confirmandoDesativar,
  desativando,
  erroDesativar,
  onSolicitarDesativar,
  onConfirmarDesativar,
  onCancelarDesativar,
}: AtivoCardProps) {
  const Icone = ICONE_POR_TIPO[ativo.tipo]
  const evolucaoPositiva = ativo.evolucaoPercentual >= 0

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="flex size-[34px] shrink-0 items-center justify-center rounded-lg bg-accent-deep">
              <Icone className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
            </div>
            <div className="flex flex-col">
              <span className="text-sm font-medium text-text-primary">{ativo.nome}</span>
              <span className="text-[12px] text-text-muted">{LABEL_POR_TIPO[ativo.tipo]}</span>
            </div>
          </div>

          {!editandoValor && (
            <div className="flex flex-col items-end">
              <span className="text-sm font-medium text-text-primary">
                {formatarMoeda(ativo.valorAtual)}
              </span>
              <span
                className={cn(
                  "text-[12px] font-medium",
                  evolucaoPositiva ? "text-positivo" : "text-negativo",
                )}
              >
                {formatarVariacaoPercentual(ativo.evolucaoPercentual)}
              </span>
            </div>
          )}
        </div>

        {confirmandoDesativar ? (
          <div className="flex flex-col gap-2">
            {erroDesativar && (
              <Alert variant="destructive">
                <AlertDescription>{erroDesativar}</AlertDescription>
              </Alert>
            )}
            <div className="flex items-center justify-between gap-2">
              <span className="text-[13px] text-text-muted">Desativar este ativo?</span>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={onCancelarDesativar}
                  disabled={desativando}
                >
                  Nao
                </Button>
                <Button
                  type="button"
                  variant="destructive"
                  size="sm"
                  onClick={onConfirmarDesativar}
                  disabled={desativando}
                >
                  {desativando ? "Desativando..." : "Sim, desativar"}
                </Button>
              </div>
            </div>
          </div>
        ) : editandoValor ? (
          <form onSubmit={onSubmitValor} className="flex flex-col gap-2">
            {erroValor && (
              <Alert variant="destructive">
                <AlertDescription>{erroValor}</AlertDescription>
              </Alert>
            )}
            <Input
              type="number"
              step="0.01"
              min="0.01"
              inputMode="decimal"
              autoFocus
              required
              value={novoValorAtual}
              onChange={(event) => onNovoValorAtualChange(event.target.value)}
            />
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={onCancelarEdicaoValor}
                disabled={salvandoValor}
              >
                Cancelar
              </Button>
              <Button type="submit" size="sm" disabled={salvandoValor}>
                {salvandoValor ? "Salvando..." : "Salvar valor"}
              </Button>
            </div>
          </form>
        ) : (
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="sm" onClick={onIniciarEdicaoValor}>
              Editar valor atual
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={onSolicitarDesativar}>
              Desativar
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
