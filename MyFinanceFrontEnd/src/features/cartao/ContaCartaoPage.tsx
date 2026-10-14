import { useState } from "react"
import type { FormEvent } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { useContaCartaoAtual } from "@/features/cartao/hooks/useContaCartaoAtual"
import { useCriarContaCartao } from "@/features/cartao/hooks/useCriarContaCartao"
import { useSaldoCartao } from "@/features/cartao/hooks/useSaldoCartao"
import { useLancarCompra } from "@/features/cartao/hooks/useLancarCompra"
import { useFaturas } from "@/features/cartao/hooks/useFaturas"
import { selecionarFaturaAtual } from "@/features/cartao/lib/selecionarFaturaAtual"
import { FaturaAtualCard } from "@/features/cartao/components/FaturaItem"
import { CartaoVisual, CartaoVisualNovo } from "@/features/cartao/components/CartaoVisual"
import { CriarContaCartaoForm } from "@/features/cartao/components/CriarContaCartaoForm"
import { LancarCompraForm } from "@/features/cartao/components/LancarCompraForm"
import { FaturasSection } from "@/features/cartao/components/FaturasSection"
import { AssinaturasCartaoSection } from "@/features/assinaturas-cartao/AssinaturasCartaoSection"
import { validarNovaContaCartao } from "@/features/cartao/lib/validarNovaContaCartao"
import { validarCompra } from "@/features/cartao/lib/validarCompra"
import { validarNumeroParcelas } from "@/features/cartao/lib/validarNumeroParcelas"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Card, CardContent } from "@/shared/ui/card"
import { PageHeader } from "@/shared/ui/page-header"
import { Button } from "@/shared/ui/button"
import { ApiError } from "@/shared/api/client"
import type { CompraParceladaResponse } from "@/features/cartao/types"

// Formatacao pura de exibicao do agrupamento de uma compra parcelada (regra
// de negocio item 12, subsecao "Parcelamento") - ex: "Notebook Dell 1/10".
// So le campos ja calculados pelo backend (valores/faturas de cada
// parcela); nenhum calculo de dominio acontece aqui.
function montarConfirmacaoParcelamento(compraParcelada: CompraParceladaResponse): string {
  const primeiraParcela = compraParcelada.parcelas[0]
  const numeroParcela = primeiraParcela?.parcelaNumero ?? 1
  return `${compraParcelada.descricao} ${numeroParcela}/${compraParcelada.quantidadeParcelas} lancada`
}

// Container: nao calcula nada de dominio (saldo vem pronto do backend), so
// orquestra estado de servidor (React Query) e estado de UI (qual cartao
// esta selecionado, formularios abertos) e decide qual apresentacao mostrar.
// As contas CARTAO vem de GET /api/contas?tipo=cartao (ver
// hooks/useContaCartaoAtual.ts) - o backend ja suporta N contas desse tipo
// sem restricao de unicidade, entao a tela lista todas e deixa o usuario
// alternar entre elas (faixa de CartaoVisual) sem perder acesso as demais.
// Apos criar uma conta (useCriarContaCartao), a invalidacao de cache ja faz
// a lista refletir a conta nova, sem estado de servidor proprio aqui.
export function ContaCartaoPage() {
  const [contaSelecionadaId, setContaSelecionadaId] = useState<string | null>(null)

  const {
    contasCartao,
    contaCartaoAtual,
    isLoading: carregandoContaCartao,
    isError: erroContaCartao,
  } = useContaCartaoAtual(contaSelecionadaId)
  const { mutate: criarContaCartao, isPending: criandoConta } = useCriarContaCartao()
  const {
    data: saldo,
    isLoading: carregandoSaldo,
    isError: erroSaldo,
    error: erroSaldoDetalhe,
  } = useSaldoCartao(contaCartaoAtual?.id ?? null)
  const { mutate: lancarCompra, isPending: lancandoCompra } = useLancarCompra()
  const { data: faturas, isLoading: carregandoFaturas } = useFaturas(contaCartaoAtual?.id ?? null)

  const [nome, setNome] = useState("")
  const [diaFechamento, setDiaFechamento] = useState("")
  const [diaVencimento, setDiaVencimento] = useState("")
  const [erroFormularioConta, setErroFormularioConta] = useState<string | null>(null)
  const [formularioNovaContaAberto, setFormularioNovaContaAberto] = useState(false)

  const [formularioCompraAberto, setFormularioCompraAberto] = useState(false)
  const [descricaoCompra, setDescricaoCompra] = useState("")
  const [valorCompra, setValorCompra] = useState("")
  const [dataCompra, setDataCompra] = useState("")
  const [categoriaCompra, setCategoriaCompra] = useState("")
  const [numeroParcelasCompra, setNumeroParcelasCompra] = useState("")
  const [erroFormularioCompra, setErroFormularioCompra] = useState<string | null>(null)
  const [confirmacaoCompraParcelada, setConfirmacaoCompraParcelada] = useState<string | null>(null)

  if (erroSaldo) {
    console.error("Falha ao carregar saldo do cartao", erroSaldoDetalhe)
  }

  function handleSubmitNovaConta(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarNovaContaCartao(nome, diaFechamento, diaVencimento)
    if (erroValidacao) {
      setErroFormularioConta(erroValidacao)
      return
    }

    criarContaCartao(
      {
        nome: nome.trim(),
        tipo: "CARTAO",
        diaFechamento: Number(diaFechamento),
        diaVencimento: Number(diaVencimento),
      },
      {
        onSuccess: (contaCriada) => {
          // A conta nova entra na lista de useContaCartaoAtual pela
          // invalidacao de cache dentro de useCriarContaCartao - aqui so
          // fechamos o formulario e selecionamos o cartao recem-criado, sem
          // guardar a conta em si como estado local.
          setErroFormularioConta(null)
          setContaSelecionadaId(contaCriada.id)
          setFormularioNovaContaAberto(false)
        },
        onError: (erro) => {
          console.error("Falha ao criar conta de cartao", erro)
          setErroFormularioConta(
            erro instanceof ApiError ? erro.message : "Nao foi possivel criar o cartao. Tente novamente.",
          )
        },
      },
    )
  }

  function abrirFormularioNovaConta() {
    setNome("")
    setDiaFechamento("")
    setDiaVencimento("")
    setErroFormularioConta(null)
    setFormularioNovaContaAberto(true)
  }

  function fecharFormularioNovaConta() {
    setFormularioNovaContaAberto(false)
    setErroFormularioConta(null)
  }

  function abrirFormularioCompra() {
    setDescricaoCompra("")
    setValorCompra("")
    setDataCompra("")
    setCategoriaCompra("")
    setNumeroParcelasCompra("")
    setErroFormularioCompra(null)
    setConfirmacaoCompraParcelada(null)
    setFormularioCompraAberto(true)
  }

  function fecharFormularioCompra() {
    setFormularioCompraAberto(false)
    setErroFormularioCompra(null)
  }

  function handleSubmitCompra(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (contaCartaoAtual === null) {
      return
    }

    const erroValidacao = validarCompra(descricaoCompra, valorCompra, dataCompra)
    if (erroValidacao) {
      setErroFormularioCompra(erroValidacao)
      return
    }

    // Numero de parcelas e opcional (regra de negocio item 12, subsecao
    // "Parcelamento"): em branco lanca compra a vista.
    const { erro: erroParcelas, numeroParcelas } = validarNumeroParcelas(numeroParcelasCompra)
    if (erroParcelas) {
      setErroFormularioCompra(erroParcelas)
      return
    }

    lancarCompra(
      {
        contaId: contaCartaoAtual.id,
        request: {
          descricao: descricaoCompra.trim(),
          valor: Number(valorCompra.trim().replace(",", ".")),
          data: dataCompra,
          categoriaId: categoriaCompra.trim().length > 0 ? categoriaCompra : null,
        },
        numeroParcelas,
      },
      {
        onSuccess: (resultado) => {
          fecharFormularioCompra()
          if (resultado.tipo === "parcelada") {
            const { compraParcelada } = resultado
            setConfirmacaoCompraParcelada(montarConfirmacaoParcelamento(compraParcelada))
          } else {
            setConfirmacaoCompraParcelada(null)
          }
        },
        onError: (erro) => {
          console.error("Falha ao lancar compra no cartao", erro)
          setErroFormularioCompra(
            erro instanceof ApiError ? erro.message : "Nao foi possivel lancar a compra. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader title="Cartao de credito" />

      {carregandoContaCartao ? (
        <p className="text-sm text-text-muted">Carregando cartao...</p>
      ) : erroContaCartao ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar o cartao</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : contasCartao.length === 0 ? (
        <CriarContaCartaoForm
          nome={nome}
          diaFechamento={diaFechamento}
          diaVencimento={diaVencimento}
          isSubmitting={criandoConta}
          errorMessage={erroFormularioConta}
          onNomeChange={setNome}
          onDiaFechamentoChange={setDiaFechamento}
          onDiaVencimentoChange={setDiaVencimento}
          onSubmit={handleSubmitNovaConta}
        />
      ) : (
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:gap-6">
          <div className="flex min-w-0 flex-col gap-4 xl:w-[340px] xl:shrink-0">
            <div className="flex flex-wrap gap-3">
              {contasCartao.map((conta) => (
                <CartaoVisual
                  key={conta.id}
                  nome={conta.nome}
                  selecionado={!formularioNovaContaAberto && conta.id === contaCartaoAtual?.id}
                  onSelecionar={() => {
                    setContaSelecionadaId(conta.id)
                    fecharFormularioNovaConta()
                    setConfirmacaoCompraParcelada(null)
                  }}
                />
              ))}
              <CartaoVisualNovo onClick={abrirFormularioNovaConta} />
            </div>

            {!formularioNovaContaAberto && contaCartaoAtual && (
              <>
                <FaturaAtualCard
                  fatura={selecionarFaturaAtual(faturas ?? [])}
                  carregando={carregandoFaturas}
                />
                <Card size="sm">
                  <CardContent className="flex flex-col gap-1">
                    <span className="text-[13px] text-text-muted">Saldo do cartao</span>
                    {carregandoSaldo && <span className="text-sm text-text-muted">Calculando...</span>}
                    {erroSaldo && (
                      <span className="text-sm text-negativo">Nao foi possivel calcular o saldo.</span>
                    )}
                    {!carregandoSaldo && !erroSaldo && saldo !== undefined && (
                      <span className="text-[19px] font-medium text-text-primary">
                        {formatarMoeda(saldo.saldo)}
                      </span>
                    )}
                  </CardContent>
                </Card>
              </>
            )}
          </div>

          <div className="flex min-w-0 flex-1 flex-col gap-4 xl:gap-6">
            {formularioNovaContaAberto ? (
              <div className="flex flex-col gap-3">
                <CriarContaCartaoForm
                  nome={nome}
                  diaFechamento={diaFechamento}
                  diaVencimento={diaVencimento}
                  isSubmitting={criandoConta}
                  errorMessage={erroFormularioConta}
                  onNomeChange={setNome}
                  onDiaFechamentoChange={setDiaFechamento}
                  onDiaVencimentoChange={setDiaVencimento}
                  onSubmit={handleSubmitNovaConta}
                />
                <Button
                  type="button"
                  variant="ghost"
                  onClick={fecharFormularioNovaConta}
                  className="self-start"
                >
                  Cancelar
                </Button>
              </div>
            ) : (
              contaCartaoAtual && (
                <>
                  {confirmacaoCompraParcelada && (
                    <Alert className="border-positivo/30 bg-positivo/10">
                      <AlertDescription className="text-positivo">
                        Compra parcelada lancada: {confirmacaoCompraParcelada}
                      </AlertDescription>
                    </Alert>
                  )}

                  <FaturasSection
                    contaId={contaCartaoAtual.id}
                    onNovaCompra={abrirFormularioCompra}
                  />

                  <AssinaturasCartaoSection contaId={contaCartaoAtual.id} />

                  <Link
                    className="self-start rounded-[9px] border-[0.5px] border-border px-4 py-2 text-[13px] font-medium text-accent-soft transition-colors hover:bg-muted"
                    to="/relatorio"
                  >
                    Ver relatorio por categoria
                  </Link>
                </>
              )
            )}
          </div>
        </div>
      )}

      {/* FAB mobile (mockup mobile): no desktop o botao fica ao lado das abas de mes. */}
      {!formularioNovaContaAberto && contaCartaoAtual && (
        <button
          type="button"
          onClick={abrirFormularioCompra}
          aria-label="Nova compra"
          className="fixed right-5 bottom-24 z-10 flex size-13 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-lg shadow-primary/35 md:hidden"
        >
          <Plus className="size-5" strokeWidth={2} aria-hidden="true" />
        </button>
      )}

      {formularioCompraAberto && (
        <LancarCompraForm
          descricao={descricaoCompra}
          valor={valorCompra}
          data={dataCompra}
          categoriaId={categoriaCompra}
          numeroParcelas={numeroParcelasCompra}
          isSubmitting={lancandoCompra}
          errorMessage={erroFormularioCompra}
          onDescricaoChange={setDescricaoCompra}
          onValorChange={setValorCompra}
          onDataChange={setDataCompra}
          onCategoriaIdChange={setCategoriaCompra}
          onNumeroParcelasChange={setNumeroParcelasCompra}
          onSubmit={handleSubmitCompra}
          onFechar={fecharFormularioCompra}
        />
      )}
    </div>
  )
}
