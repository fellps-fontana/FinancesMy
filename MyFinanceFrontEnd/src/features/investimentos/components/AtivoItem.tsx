import { useState, type FormEvent } from "react"
import { useAtualizarValorAtualAtivo } from "@/features/investimentos/hooks/useAtualizarValorAtualAtivo"
import { useDesativarAtivo } from "@/features/investimentos/hooks/useDesativarAtivo"
import { useRegistrarDividendo } from "@/features/investimentos/hooks/useRegistrarDividendo"
import { AtivoCard } from "@/features/investimentos/components/AtivoCard"
import { FormRegistrarDividendo } from "@/features/investimentos/components/FormRegistrarDividendo"
import { validarValorAtual } from "@/features/investimentos/lib/validarValorAtual"
import { validarDividendo } from "@/features/investimentos/lib/validarDividendo"
import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"
import { dataDeHoje } from "@/features/investimentos/lib/dataDeHoje"
import { ApiError } from "@/shared/api/client"
import type { AtivoResponse } from "@/features/investimentos/types"

type AtivoItemProps = {
  ativo: AtivoResponse
}

// Container do item de lista: guarda o estado de UI (edicao de valor atual,
// confirmacao de desativacao, formulario de registro de dividendo) e aciona
// as mutations (useAtualizarValorAtualAtivo/useDesativarAtivo/
// useRegistrarDividendo). A apresentacao pura fica em AtivoCard/
// FormRegistrarDividendo - mesma divisao de responsabilidade de
// ContaInvestimentoItem/ContaInvestimentoCard (clean-code.md
// "Organizacao (React)"). Dividendo e regra-de-negocio.md item 8.4: cadastro
// MANUAL do usuario (valor + data), tipo/origem decididos pelo backend.
export function AtivoItem({ ativo }: AtivoItemProps) {
  const { mutate: atualizarValor, isPending: salvandoValor } = useAtualizarValorAtualAtivo()
  const { mutate: desativarAtivo, isPending: desativando } = useDesativarAtivo()
  const { mutate: registrarDividendo, isPending: registrandoDividendo } = useRegistrarDividendo()

  const [editandoValor, setEditandoValor] = useState(false)
  const [novoValorAtual, setNovoValorAtual] = useState("")
  const [erroValor, setErroValor] = useState<string | null>(null)

  const [confirmandoDesativar, setConfirmandoDesativar] = useState(false)
  const [erroDesativar, setErroDesativar] = useState<string | null>(null)

  const [formDividendoAberto, setFormDividendoAberto] = useState(false)
  const [valorDividendo, setValorDividendo] = useState("")
  const [dataDividendo, setDataDividendo] = useState("")
  const [erroDividendo, setErroDividendo] = useState<string | null>(null)

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

  function abrirFormDividendo() {
    setValorDividendo("")
    setDataDividendo(dataDeHoje())
    setErroDividendo(null)
    setFormDividendoAberto(true)
  }

  function fecharFormDividendo() {
    setFormDividendoAberto(false)
    setErroDividendo(null)
  }

  function handleSubmitDividendo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarDividendo(valorDividendo, dataDividendo)
    if (erroValidacao) {
      setErroDividendo(erroValidacao)
      return
    }

    registrarDividendo(
      {
        ativoId: ativo.id,
        request: { valor: converterValorParaNumero(valorDividendo), data: dataDividendo },
      },
      {
        onSuccess: fecharFormDividendo,
        onError: (error) => {
          console.error("Falha ao registrar dividendo", error)
          setErroDividendo(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar o dividendo. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <>
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
        onSolicitarRegistrarDividendo={abrirFormDividendo}
      />

      {formDividendoAberto && (
        <FormRegistrarDividendo
          ativoNome={ativo.nome}
          valor={valorDividendo}
          data={dataDividendo}
          isSubmitting={registrandoDividendo}
          errorMessage={erroDividendo}
          onValorChange={setValorDividendo}
          onDataChange={setDataDividendo}
          onSubmit={handleSubmitDividendo}
          onFechar={fecharFormDividendo}
        />
      )}
    </>
  )
}
