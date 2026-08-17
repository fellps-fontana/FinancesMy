import { useState, type FormEvent } from "react"
import { useAtualizarSaldoConta } from "@/features/contas/hooks/useAtualizarSaldoConta"
import { useDesativarConta } from "@/features/contas/hooks/useDesativarConta"
import { validarSaldo, converterSaldoParaNumero } from "@/features/contas/lib/validarSaldo"
import { ContaCard } from "@/features/contas/components/ContaCard"
import { ApiError } from "@/shared/api/client"
import type { ContaResponse } from "@/features/contas/types"

type ContaItemProps = {
  conta: ContaResponse
}

// Container do item de lista: guarda o estado de UI (edicao de saldo_manual,
// confirmacao de desativacao) e aciona as mutations
// (useAtualizarSaldoConta/useDesativarConta). A apresentacao pura fica em
// ContaCard - clean-code.md "Organizacao (React)": componente de
// apresentacao separado da logica de estado. Mesma divisao ja usada por
// AtivoItem/AtivoCard e, antes da migracao para esta feature generica, por
// ContaInvestimentoItem/ContaInvestimentoCard.
export function ContaItem({ conta }: ContaItemProps) {
  const { mutate: atualizarSaldo, isPending: salvandoSaldo } = useAtualizarSaldoConta()
  const { mutate: desativarConta, isPending: desativando } = useDesativarConta()

  const [editandoSaldo, setEditandoSaldo] = useState(false)
  const [novoSaldo, setNovoSaldo] = useState("")
  const [erroSaldo, setErroSaldo] = useState<string | null>(null)

  const [confirmandoDesativar, setConfirmandoDesativar] = useState(false)
  const [erroDesativar, setErroDesativar] = useState<string | null>(null)

  function iniciarEdicaoSaldo() {
    setNovoSaldo(String(conta.saldoManual ?? 0))
    setErroSaldo(null)
    setEditandoSaldo(true)
  }

  function cancelarEdicaoSaldo() {
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
        onSuccess: () => {
          setEditandoSaldo(false)
          setErroSaldo(null)
        },
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

  function cancelarDesativar() {
    setConfirmandoDesativar(false)
    setErroDesativar(null)
  }

  function confirmarDesativar() {
    desativarConta(conta.id, {
      onError: (error) => {
        // Em sucesso a conta some da lista pela invalidacao de cache. Em
        // erro so registramos o contexto e mantemos a confirmacao aberta com
        // a mensagem, sem travar a UI - ver clean-code.md "Tratamento de erro".
        console.error("Falha ao desativar conta", error)
        setErroDesativar(
          error instanceof ApiError ? error.message : "Nao foi possivel desativar a conta. Tente novamente.",
        )
      },
    })
  }

  return (
    <ContaCard
      conta={conta}
      editandoSaldo={editandoSaldo}
      novoSaldo={novoSaldo}
      salvandoSaldo={salvandoSaldo}
      erroSaldo={erroSaldo}
      onIniciarEdicaoSaldo={iniciarEdicaoSaldo}
      onNovoSaldoChange={setNovoSaldo}
      onSubmitSaldo={handleSubmitSaldo}
      onCancelarEdicaoSaldo={cancelarEdicaoSaldo}
      confirmandoDesativar={confirmandoDesativar}
      desativando={desativando}
      erroDesativar={erroDesativar}
      onSolicitarDesativar={solicitarDesativar}
      onConfirmarDesativar={confirmarDesativar}
      onCancelarDesativar={cancelarDesativar}
    />
  )
}
