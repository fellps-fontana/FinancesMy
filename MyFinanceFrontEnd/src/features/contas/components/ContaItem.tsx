import { useState, type FormEvent } from "react"
import { Power } from "lucide-react"
import { useAtualizarSaldoConta } from "@/features/contas/hooks/useAtualizarSaldoConta"
import { useDesativarConta } from "@/features/contas/hooks/useDesativarConta"
import { validarSaldo, converterSaldoParaNumero } from "@/features/contas/lib/validarSaldo"
import { ContaCard } from "@/features/contas/components/ContaCard"
import { FormEditarConta } from "@/features/contas/components/FormEditarConta"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import type { ContaResponse } from "@/features/contas/types"

type ContaItemProps = {
  conta: ContaResponse
}

// Container do item: guarda o estado de UI (modal de saldo, confirmacao de
// desativacao, erro) e aciona as mutations. Erros 409 (conta com ativo ativo)
// e 422 (saldo invalido) chegam tipados pelos hooks, com a mensagem do
// backend em `message`. Apresentacao pura em ContaCard.
export function ContaItem({ conta }: ContaItemProps) {
  const { mutate: atualizarSaldo, isPending: salvandoSaldo } = useAtualizarSaldoConta()
  const { mutate: desativarConta, isPending: desativando } = useDesativarConta()

  const [editandoConta, setEditandoConta] = useState(false)
  const [editandoSaldo, setEditandoSaldo] = useState(false)
  const [novoSaldo, setNovoSaldo] = useState("")
  const [erroSaldo, setErroSaldo] = useState<string | null>(null)

  const [confirmandoDesativar, setConfirmandoDesativar] = useState(false)
  const [erroDesativar, setErroDesativar] = useState<string | null>(null)

  function iniciarEdicaoSaldo() {
    setNovoSaldo(String(conta.saldoManual ?? conta.saldo))
    setErroSaldo(null)
    setEditandoSaldo(true)
  }

  function fecharEdicaoSaldo() {
    setEditandoSaldo(false)
    setErroSaldo(null)
  }

  function handleSubmitSaldo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarSaldo(novoSaldo)
    if (erroValidacao) {
      setErroSaldo(erroValidacao)
      return
    }

    atualizarSaldo(
      { id: conta.id, request: { novoSaldo: converterSaldoParaNumero(novoSaldo) } },
      {
        onSuccess: fecharEdicaoSaldo,
        onError: (error) => {
          console.error("Falha ao atualizar saldo da conta", error)
          setErroSaldo(
            error instanceof ApiError ? error.message : "Nao foi possivel salvar o saldo. Tente novamente.",
          )
        },
      },
    )
  }

  function solicitarDesativar() {
    setErroDesativar(null)
    setConfirmandoDesativar(true)
  }

  function confirmarDesativar() {
    desativarConta(conta.id, {
      onSuccess: () => setConfirmandoDesativar(false),
      onError: (error) => {
        // 409: conta com ativo ativo. Fecha a confirmacao e deixa a mensagem
        // do backend visivel no card.
        console.error("Falha ao desativar conta", error)
        setConfirmandoDesativar(false)
        setErroDesativar(
          error instanceof ApiError ? error.message : "Nao foi possivel desativar a conta. Tente novamente.",
        )
      },
    })
  }

  return (
    <div className="flex flex-col gap-2">
      <ContaCard
        conta={conta}
        onEditar={() => setEditandoConta(true)}
        onEditarSaldo={iniciarEdicaoSaldo}
        onDesativar={solicitarDesativar}
      />

      {editandoConta && <FormEditarConta conta={conta} onFechar={() => setEditandoConta(false)} />}

      {erroDesativar && (
        <Alert variant="destructive">
          <AlertDescription>{erroDesativar}</AlertDescription>
        </Alert>
      )}

      <FormModal open={editandoSaldo} onClose={fecharEdicaoSaldo} title={`Atualizar saldo - ${conta.nome}`}>
        <form onSubmit={handleSubmitSaldo} className="flex flex-col gap-4">
          {erroSaldo && (
            <Alert variant="destructive">
              <AlertDescription>{erroSaldo}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`saldo-${conta.id}`}>Novo saldo</Label>
            <Input
              id={`saldo-${conta.id}`}
              type="number"
              step="0.01"
              inputMode="decimal"
              autoFocus
              required
              value={novoSaldo}
              onChange={(event) => setNovoSaldo(event.target.value)}
              className={CLASSE_CAMPO}
            />
          </div>

          <div className="mt-1 flex justify-end gap-2.5">
            <Button
              type="button"
              variant="outline"
              onClick={fecharEdicaoSaldo}
              disabled={salvandoSaldo}
              className={CLASSE_BOTAO_CANCELAR}
            >
              Cancelar
            </Button>
            <Button type="submit" disabled={salvandoSaldo} className={CLASSE_BOTAO_PRIMARIO}>
              {salvandoSaldo ? "Salvando..." : "Salvar saldo"}
            </Button>
          </div>
        </form>
      </FormModal>

      <ConfirmDialog
        open={confirmandoDesativar}
        title={`Desativar ${conta.nome}?`}
        description="A conta sai das listas. Contas com ativo ativo nao podem ser desativadas."
        icon={<Power className="size-[18px]" />}
        destructive
        confirmLabel="Desativar"
        loading={desativando}
        onCancel={() => setConfirmandoDesativar(false)}
        onConfirm={confirmarDesativar}
      />
    </div>
  )
}
