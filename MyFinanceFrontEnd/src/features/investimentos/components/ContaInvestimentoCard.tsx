import type { FormEvent } from "react"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { ListaAtivos } from "@/features/investimentos/components/ListaAtivos"
import type { ContaResponse } from "@/features/investimentos/types"

type ContaInvestimentoCardProps = {
  conta: ContaResponse
  editandoSaldo: boolean
  novoSaldo: string
  salvandoSaldo: boolean
  erroSaldo: string | null
  onIniciarEdicaoSaldo: () => void
  onNovoSaldoChange: (value: string) => void
  onSubmitSaldo: (event: FormEvent<HTMLFormElement>) => void
  onCancelarEdicaoSaldo: () => void
  confirmandoDesativar: boolean
  desativando: boolean
  erroDesativar: string | null
  desabilitarDesativar: boolean
  motivoDesativarBloqueado: string | null
  onSolicitarDesativar: () => void
  onConfirmarDesativar: () => void
  onCancelarDesativar: () => void
}

// Componente de apresentacao (burro): recebe a conta e o estado de UI ja
// resolvido pelo container (ContaInvestimentoItem) e exibe. Nenhuma mutation
// ou fetch mora aqui - so callbacks repassados para quem chama.
export function ContaInvestimentoCard({
  conta,
  editandoSaldo,
  novoSaldo,
  salvandoSaldo,
  erroSaldo,
  onIniciarEdicaoSaldo,
  onNovoSaldoChange,
  onSubmitSaldo,
  onCancelarEdicaoSaldo,
  confirmandoDesativar,
  desativando,
  erroDesativar,
  desabilitarDesativar,
  motivoDesativarBloqueado,
  onSolicitarDesativar,
  onConfirmarDesativar,
  onCancelarDesativar,
}: ContaInvestimentoCardProps) {
  // saldoManual === null marca a conta em modo carteira de ativos (regra-de-
  // negocio.md item 8/10): o saldo dela passa a ser calculado a partir dos
  // ativos, nao mais editado manualmente. O saldo exibido acima vem sempre de
  // conta.saldo, que o backend ja popula com o valor certo nos dois formatos.
  const contaComCarteiraDeAtivos = conta.saldoManual === null

  return (
    <Card>
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-card-foreground">{conta.nome}</span>
          {!editandoSaldo && (
            <span className="text-sm font-medium text-card-foreground">
              {formatarMoeda(conta.saldo)}
            </span>
          )}
        </div>

        {contaComCarteiraDeAtivos ? (
          confirmandoDesativar ? (
            <ConfirmacaoDesativar
              erroDesativar={erroDesativar}
              desativando={desativando}
              onConfirmarDesativar={onConfirmarDesativar}
              onCancelarDesativar={onCancelarDesativar}
            />
          ) : (
            <>
              <ListaAtivos contaId={conta.id} />
              <div className="flex flex-col items-end gap-1">
                {motivoDesativarBloqueado && (
                  <span className="text-[12px] text-muted-foreground">{motivoDesativarBloqueado}</span>
                )}
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={onSolicitarDesativar}
                  disabled={desabilitarDesativar}
                >
                  Desativar
                </Button>
              </div>
            </>
          )
        ) : editandoSaldo ? (
          <form onSubmit={onSubmitSaldo} className="flex flex-col gap-2">
            {erroSaldo && (
              <Alert variant="destructive">
                <AlertDescription>{erroSaldo}</AlertDescription>
              </Alert>
            )}
            <Input
              type="number"
              step="0.01"
              min="0"
              inputMode="decimal"
              autoFocus
              required
              value={novoSaldo}
              onChange={(event) => onNovoSaldoChange(event.target.value)}
            />
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={onCancelarEdicaoSaldo}
                disabled={salvandoSaldo}
              >
                Cancelar
              </Button>
              <Button type="submit" size="sm" disabled={salvandoSaldo}>
                {salvandoSaldo ? "Salvando..." : "Salvar saldo"}
              </Button>
            </div>
          </form>
        ) : confirmandoDesativar ? (
          <ConfirmacaoDesativar
            erroDesativar={erroDesativar}
            desativando={desativando}
            onConfirmarDesativar={onConfirmarDesativar}
            onCancelarDesativar={onCancelarDesativar}
          />
        ) : (
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="sm" onClick={onIniciarEdicaoSaldo}>
              Editar saldo
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

type ConfirmacaoDesativarProps = {
  erroDesativar: string | null
  desativando: boolean
  onConfirmarDesativar: () => void
  onCancelarDesativar: () => void
}

// Bloco de confirmacao de desativar extraido para reuso: aparece tanto na
// conta simples quanto na conta com carteira de ativos, ja que desativar
// independe do formato de saldo (regra-de-negocio.md item 8).
function ConfirmacaoDesativar({
  erroDesativar,
  desativando,
  onConfirmarDesativar,
  onCancelarDesativar,
}: ConfirmacaoDesativarProps) {
  return (
    <div className="flex flex-col gap-2">
      {erroDesativar && (
        <Alert variant="destructive">
          <AlertDescription>{erroDesativar}</AlertDescription>
        </Alert>
      )}
      <div className="flex items-center justify-between gap-2">
        <span className="text-[13px] text-muted-foreground">Desativar esta conta?</span>
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
  )
}
