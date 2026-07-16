import { useState, type FormEvent } from "react"
import { useAtualizarValorAtualAtivo } from "@/features/investimentos/hooks/useAtualizarValorAtualAtivo"
import { useDesativarAtivo } from "@/features/investimentos/hooks/useDesativarAtivo"
import { AtivoCard } from "@/features/investimentos/components/AtivoCard"
import { validarValorAtual } from "@/features/investimentos/lib/validarValorAtual"
import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"
import { ApiError } from "@/shared/api/client"
import type { AtivoResponse } from "@/features/investimentos/types"

type AtivoItemProps = {
  ativo: AtivoResponse
}

// Container do item de lista: guarda o estado de UI (edicao de valor atual,
// confirmacao de desativacao) e aciona as mutations
// (useAtualizarValorAtualAtivo/useDesativarAtivo). A apresentacao pura fica
// em AtivoCard - mesma divisao de responsabilidade de
// ContaInvestimentoItem/ContaInvestimentoCard (clean-code.md
// "Organizacao (React)").
export function AtivoItem({ ativo }: AtivoItemProps) {
  const { mutate: atualizarValor, isPending: salvandoValor } = useAtualizarValorAtualAtivo()
  const { mutate: desativarAtivo, isPending: desativando } = useDesativarAtivo()

  const [editandoValor, setEditandoValor] = useState(false)
  const [novoValorAtual, setNovoValorAtual] = useState("")
  const [erroValor, setErroValor] = useState<string | null>(null)

  const [confirmandoDesativar, setConfirmandoDesativar] = useState(false)
  const [erroDesativar, setErroDesativar] = useState<string | null>(null)

  function iniciarEdicaoValor() {
    setNovoValorAtual(String(ativo.valorAtual))
    setErroValor(null)
    setEditandoValor(true)
  }

  function cancelarEdicaoValor() {
    setEditandoValor(false)
    setErroValor(null)
  }

  function handleSubmitValor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarValorAtual(novoValorAtual)
    if (erroValidacao) {
      setErroValor(erroValidacao)
      return
    }

    atualizarValor(
      { id: ativo.id, request: { novoValorAtual: converterValorParaNumero(novoValorAtual) } },
      {
        onSuccess: () => {
          setEditandoValor(false)
          setErroValor(null)
        },
        onError: (error) => {
          console.error("Falha ao atualizar valor atual do ativo", error)
          setErroValor(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel salvar o valor. Tente novamente.",
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
    desativarAtivo(ativo.id, {
      onError: (error) => {
        // Em sucesso o ativo some da lista pela invalidacao de cache. Em
        // erro so registramos o contexto e mantemos a confirmacao aberta com
        // a mensagem - ver clean-code.md "Tratamento de erro".
        console.error("Falha ao desativar ativo", error)
        setErroDesativar(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel desativar o ativo. Tente novamente.",
        )
      },
    })
  }

  return (
    <AtivoCard
      ativo={ativo}
      editandoValor={editandoValor}
      novoValorAtual={novoValorAtual}
      salvandoValor={salvandoValor}
      erroValor={erroValor}
      onIniciarEdicaoValor={iniciarEdicaoValor}
      onNovoValorAtualChange={setNovoValorAtual}
      onSubmitValor={handleSubmitValor}
      onCancelarEdicaoValor={cancelarEdicaoValor}
      confirmandoDesativar={confirmandoDesativar}
      desativando={desativando}
      erroDesativar={erroDesativar}
      onSolicitarDesativar={solicitarDesativar}
      onConfirmarDesativar={confirmarDesativar}
      onCancelarDesativar={cancelarDesativar}
    />
  )
}
