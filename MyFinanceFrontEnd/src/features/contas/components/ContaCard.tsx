import type { FormEvent } from "react"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { obterSubtituloConta } from "@/features/contas/lib/obterSubtituloConta"
import { ContaIcone } from "@/features/contas/components/ContaIcone"
import { ContaBadgeOrigem } from "@/features/contas/components/ContaBadgeOrigem"
import type { ContaResponse } from "@/features/contas/types"

type ContaCardProps = {
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

// Componente de apresentacao (burro): estado de edicao de saldo_manual e de
// confirmacao de desativacao ja resolvido pelo container (ContaItem.tsx) -
// so callbacks repassados, nenhum fetch/mutation mora aqui (clean-code.md
// "Organizacao (React)"). Layout segue o item de lista do mockup 03 Contas
// (icone, nome, subtitulo, saldo, badge de origem) e acrescenta as acoes de
// editar saldo/desativar reintroduzidas pela revisao do bloco Contas
// (regra-de-negocio.md item 10: saldo_manual e controle continuo do
// usuario, nao so no cadastro inicial).
export function ContaCard({
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
}: ContaCardProps) {
  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center gap-3">
          <ContaIcone conta={conta} />

          <div className="flex-1 min-w-0">
            <div className="truncate text-[14px] text-text-body">{conta.nome}</div>
            <div className="mt-0.5 text-[12px] text-text-faint">{obterSubtituloConta(conta)}</div>
          </div>

          {!editandoSaldo && (
            <div className="text-right">
              <div className="text-[14px] text-text-primary">{formatarMoeda(conta.saldo)}</div>
              <div className="mt-0.5 flex justify-end">
                <ContaBadgeOrigem origem={conta.origem} />
              </div>
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
              <span className="text-[13px] text-text-muted">Desativar esta conta?</span>
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
