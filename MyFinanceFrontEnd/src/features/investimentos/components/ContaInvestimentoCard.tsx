import type { FormEvent } from "react"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
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
  onSolicitarDesativar: () => void
  onConfirmarDesativar: () => void
  onCancelarDesativar: () => void
}

// Componente de apresentacao (burro): recebe a conta e o estado de UI ja
// resolvido pelo container (ContaInvestimentoItem) e exibe. Nenhuma mutation
// ou fetch mora aqui - so callbacks repassados para quem chama. Conta de
// investimento simples (cofrinho/XP) e saldo 100% manual - regra-de-
// negocio.md item 8 ("Conta de investimento - saldo simples") e item 10.
// Sem modo "carteira de ativos": Ativo e um modulo standalone, sem vinculo
// com Conta (ver ListaAtivosPage.tsx), entao saldoManual nunca vem nulo aqui.
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
  onSolicitarDesativar,
  onConfirmarDesativar,
  onCancelarDesativar,
}: ContaInvestimentoCardProps) {
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

        {editandoSaldo ? (
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
