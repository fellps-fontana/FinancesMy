import { useState } from "react"
import type { FormEvent } from "react"
import { useFaturas } from "@/features/cartao/hooks/useFaturas"
import { usePagarFatura } from "@/features/cartao/hooks/usePagarFatura"
import { FaturaItem } from "@/features/cartao/components/FaturaItem"
import { PagarFaturaModal } from "@/features/cartao/components/PagarFaturaModal"
import { validarPagamentoFatura } from "@/features/cartao/lib/validarPagamentoFatura"
import { dataDeHoje } from "@/features/cartao/lib/formatarData"
import { ApiError } from "@/shared/api/client"
import type { FaturaResponse } from "@/features/cartao/types"

type FaturasSectionProps = {
  contaId: string
}

// Container: le as faturas via React Query e decide qual fatura esta em
// pagamento (estado de UI). A apresentacao pura fica em
// FaturaItem/PagarFaturaModal - mesma divisao de responsabilidade de
// ContaInvestimentoItem/ContaInvestimentoCard em investimentos.
export function FaturasSection({ contaId }: FaturasSectionProps) {
  const { data: faturas, isLoading, error } = useFaturas(contaId)
  const { mutate: pagarFatura, isPending: pagando } = usePagarFatura()

  const [faturaEmPagamento, setFaturaEmPagamento] = useState<FaturaResponse | null>(null)
  const [contaOrigemId, setContaOrigemId] = useState("")
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje())
  const [erroPagamento, setErroPagamento] = useState<string | null>(null)

  if (error) {
    console.error("Falha ao carregar faturas do cartao", error)
  }

  function abrirPagamento(fatura: FaturaResponse) {
    setFaturaEmPagamento(fatura)
    setContaOrigemId("")
    setValor(fatura.valorPendente.toString())
    setData(dataDeHoje())
    setErroPagamento(null)
  }

  function fecharPagamento() {
    setFaturaEmPagamento(null)
    setErroPagamento(null)
  }

  function handleSubmitPagamento(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (faturaEmPagamento === null) {
      return
    }

    const erroValidacao = validarPagamentoFatura(
      contaOrigemId,
      valor,
      data,
      faturaEmPagamento.valorPendente,
    )
    if (erroValidacao) {
      setErroPagamento(erroValidacao)
      return
    }

    pagarFatura(
      {
        contaId,
        faturaId: faturaEmPagamento.id,
        request: {
          contaOrigemId,
          valor: Number(valor.trim().replace(",", ".")),
          data,
        },
      },
      {
        onSuccess: fecharPagamento,
        onError: (erro) => {
          console.error("Falha ao pagar fatura", erro)
          setErroPagamento(
            erro instanceof ApiError
              ? erro.message
              : "Nao foi possivel registrar o pagamento. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-[19px] font-medium text-text-primary">Faturas</h2>

      {isLoading && <p className="text-sm text-text-muted">Carregando faturas...</p>}
      {error && <p className="text-sm text-negativo">Nao foi possivel carregar as faturas.</p>}

      {faturas && faturas.length === 0 && (
        <p className="text-sm text-text-muted">Nenhuma fatura ainda.</p>
      )}

      {faturas && faturas.length > 0 && (
        <div className="flex flex-col gap-3">
          {faturas.map((fatura) => (
            <FaturaItem key={fatura.id} fatura={fatura} onPagar={() => abrirPagamento(fatura)} />
          ))}
        </div>
      )}

      <p className="text-[12px] text-text-faint">Compras individuais nao disponiveis nesta versao.</p>

      {faturaEmPagamento && (
        <PagarFaturaModal
          valorPendente={faturaEmPagamento.valorPendente}
          contaOrigemId={contaOrigemId}
          valor={valor}
          data={data}
          isSubmitting={pagando}
          errorMessage={erroPagamento}
          onContaOrigemIdChange={setContaOrigemId}
          onValorChange={setValor}
          onDataChange={setData}
          onSubmit={handleSubmitPagamento}
          onFechar={fecharPagamento}
        />
      )}
    </section>
  )
}
