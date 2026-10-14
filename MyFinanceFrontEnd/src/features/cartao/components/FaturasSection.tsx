import { useMemo, useState } from "react"
import type { FormEvent } from "react"
import { Plus } from "lucide-react"
import { useFaturas } from "@/features/cartao/hooks/useFaturas"
import { useComprasDaFatura } from "@/features/cartao/hooks/useComprasDaFatura"
import { usePagarFatura } from "@/features/cartao/hooks/usePagarFatura"
import { useContasBanco } from "@/features/cartao/hooks/useContasBanco"
import { useParcelasElegiveis } from "@/features/cartao/hooks/useParcelasElegiveis"
import { useAnteciparParcelas } from "@/features/cartao/hooks/useAnteciparParcelas"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { construirMapaCategorias } from "@/features/categorias/lib/construirMapaCategorias"
import { FaturaResumo } from "@/features/cartao/components/FaturaItem"
import { ListaComprasFatura } from "@/features/cartao/components/ListaComprasFatura"
import { AcoesCompraSheet } from "@/features/cartao/components/AcoesCompraSheet"
import { AnteciparParcelasModal } from "@/features/cartao/components/AnteciparParcelasModal"
import { PagarFaturaModal } from "@/features/cartao/components/PagarFaturaModal"
import { validarPagamentoFatura } from "@/features/cartao/lib/validarPagamentoFatura"
import { selecionarFaturaAtual } from "@/features/cartao/lib/selecionarFaturaAtual"
import { dataDeHoje } from "@/shared/lib/datas"
import { rotuloMes } from "@/features/cartao/lib/rotuloMes"
import { ApiError } from "@/shared/api/client"
import { Button } from "@/shared/ui/button"
import { Chip } from "@/shared/ui/chip"
import type { CompraResponse, FaturaResponse } from "@/features/cartao/types"

type FaturasSectionProps = {
  contaId: string
  onNovaCompra: () => void
}

// Alvo da acao de antecipar: "menu" e a folha de acoes mobile; "antecipar" e
// o modal com stepper (desktop abre direto, mobile vem do menu).
type AlvoAntecipacao = { compra: CompraResponse; etapa: "menu" | "antecipar" }

// Container: le faturas e compras via React Query e guarda o estado de UI
// (aba selecionada, pagamento e antecipacao em andamento). A apresentacao fica
// em FaturaResumo/ListaComprasFatura/modais; elegibilidade e valores vem do
// backend.
export function FaturasSection({ contaId, onNovaCompra }: FaturasSectionProps) {
  const { data: faturas, isLoading, error } = useFaturas(contaId)
  const { mutate: pagarFatura, isPending: pagando } = usePagarFatura()
  const { data: contasBanco, isLoading: carregandoContasBanco } = useContasBanco()
  const { mutate: anteciparParcelas, isPending: antecipando } = useAnteciparParcelas()
  const { data: categoriasAtivas } = useCategorias(undefined, false)
  const { data: categoriasArquivadas } = useCategorias(undefined, true)

  const [faturaSelecionadaId, setFaturaSelecionadaId] = useState<string | null>(null)
  const [faturaEmPagamento, setFaturaEmPagamento] = useState<FaturaResponse | null>(null)
  const [contaOrigemId, setContaOrigemId] = useState("")
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje())
  const [erroPagamento, setErroPagamento] = useState<string | null>(null)

  const [alvoAntecipacao, setAlvoAntecipacao] = useState<AlvoAntecipacao | null>(null)
  const [quantidadeAntecipar, setQuantidadeAntecipar] = useState(1)
  const [erroAntecipacao, setErroAntecipacao] = useState<string | null>(null)

  const faturaSelecionada =
    faturas?.find((f) => f.id === faturaSelecionadaId) ?? selecionarFaturaAtual(faturas ?? [])

  const {
    data: compras,
    isLoading: carregandoCompras,
    error: erroCompras,
  } = useComprasDaFatura(contaId, faturaSelecionada?.id ?? null)

  const compraEmAntecipacao = alvoAntecipacao?.compra ?? null
  const { data: elegiveis, isError: erroElegiveis } = useParcelasElegiveis(
    contaId,
    compraEmAntecipacao?.compraParceladaId ?? null,
  )

  const categoriaPorId = useMemo(
    () => construirMapaCategorias([...(categoriasAtivas ?? []), ...(categoriasArquivadas ?? [])]),
    [categoriasAtivas, categoriasArquivadas],
  )

  if (error) {
    console.error("Falha ao carregar faturas do cartao", error)
  }
  if (erroCompras) {
    console.error("Falha ao carregar compras da fatura", erroCompras)
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

  function abrirAntecipacao(compra: CompraResponse, etapa: AlvoAntecipacao["etapa"]) {
    setAlvoAntecipacao({ compra, etapa })
    setQuantidadeAntecipar(1)
    setErroAntecipacao(null)
  }

  function fecharAntecipacao() {
    setAlvoAntecipacao(null)
    setErroAntecipacao(null)
  }

  function handleConfirmarAntecipacao() {
    const compraParceladaId = compraEmAntecipacao?.compraParceladaId
    if (!compraParceladaId) {
      return
    }

    // O destino (fatura aberta) e a data sao decididos pelo backend.
    anteciparParcelas(
      { contaId, compraParceladaId, request: { quantidade: quantidadeAntecipar } },
      {
        onSuccess: fecharAntecipacao,
        onError: (erro) => {
          console.error("Falha ao antecipar parcelas", erro)
          setErroAntecipacao(
            erro instanceof ApiError
              ? erro.message
              : "Nao foi possivel antecipar as parcelas. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center gap-2">
        <div className="flex min-w-0 items-center gap-2 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {faturas?.map((fatura) => (
          <Chip
            key={fatura.id}
            selected={fatura.id === faturaSelecionada?.id}
            onClick={() => setFaturaSelecionadaId(fatura.id)}
            className="whitespace-nowrap md:px-3.5 md:py-2 md:text-[13px]"
          >
            {rotuloMes(fatura)}
          </Chip>
        ))}
        </div>
        <div className="flex-1" />
        <Button
          type="button"
          onClick={onNovaCompra}
          className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
        >
          <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
          Nova compra
        </Button>
      </div>

      {isLoading && <p className="text-sm text-text-muted">Carregando faturas...</p>}
      {error && <p className="text-sm text-negativo">Nao foi possivel carregar as faturas.</p>}

      {faturas && faturas.length === 0 && (
        <p className="text-sm text-text-muted">Nenhuma fatura ainda.</p>
      )}

      {faturaSelecionada && (
        <>
          <ListaComprasFatura
            compras={compras}
            carregando={carregandoCompras}
            erro={erroCompras ? "Nao foi possivel carregar as compras da fatura." : null}
            categoriaPorId={categoriaPorId}
            onAntecipar={(compra) => abrirAntecipacao(compra, "antecipar")}
            onAbrirAcoes={(compra) => abrirAntecipacao(compra, "menu")}
          />
          <FaturaResumo fatura={faturaSelecionada} onPagar={() => abrirPagamento(faturaSelecionada)} />
        </>
      )}

      <AcoesCompraSheet
        compra={alvoAntecipacao?.etapa === "menu" ? alvoAntecipacao.compra : null}
        quantidadeElegiveis={elegiveis ? elegiveis.length : null}
        onAnteciparParcelas={() => {
          if (alvoAntecipacao) abrirAntecipacao(alvoAntecipacao.compra, "antecipar")
        }}
        onFechar={fecharAntecipacao}
      />

      <AnteciparParcelasModal
        open={alvoAntecipacao?.etapa === "antecipar"}
        descricao={compraEmAntecipacao?.descricao ?? ""}
        elegiveis={elegiveis ?? null}
        erroCarregamento={erroElegiveis}
        quantidade={quantidadeAntecipar}
        isSubmitting={antecipando}
        errorMessage={erroAntecipacao}
        onQuantidadeChange={setQuantidadeAntecipar}
        onConfirmar={handleConfirmarAntecipacao}
        onFechar={fecharAntecipacao}
      />

      {faturaEmPagamento && (
        <PagarFaturaModal
          valorPendente={faturaEmPagamento.valorPendente}
          contaOrigemId={contaOrigemId}
          contasBanco={contasBanco ?? []}
          carregandoContasBanco={carregandoContasBanco}
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
