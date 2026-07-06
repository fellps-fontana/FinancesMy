import { useState, type FormEvent } from "react"
import { useAtualizarSaldoConta } from "@/features/investimentos/hooks/useAtualizarSaldoConta"
import { useDesativarConta } from "@/features/investimentos/hooks/useDesativarConta"
import { ContaInvestimentoCard } from "@/features/investimentos/components/ContaInvestimentoCard"
import { validarSaldo, converterSaldoParaNumero } from "@/features/investimentos/lib/validarSaldo"
import { ApiError } from "@/shared/api/client"
import type { ContaResponse } from "@/features/investimentos/types"

type ContaInvestimentoItemProps = {
  conta: ContaResponse
}

// Container do item de lista: guarda o estado de UI (edicao de saldo,
// confirmacao de desativacao) e aciona as mutations ja existentes
// (useAtualizarSaldoConta/useDesativarConta). A apresentacao pura fica em
// ContaInvestimentoCard - ver clean-code.md "Organizacao (React)".
export function ContaInvestimentoItem({ conta }: ContaInvestimentoItemProps) {
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
          console.error("Falha ao atualizar saldo da conta de investimento", error)
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
        // a mensagem, sem travar a UI (o usuario pode tentar de novo ou
        // cancelar) - ver clean-code.md "Tratamento de erro".
        console.error("Falha ao desativar conta de investimento", error)
        setErroDesativar(
          error instanceof ApiError ? error.message : "Nao foi possivel desativar a conta. Tente novamente.",
        )
      },
    })
  }

  return (
    <ContaInvestimentoCard
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
