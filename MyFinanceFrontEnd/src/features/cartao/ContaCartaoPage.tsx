import { useState } from "react"
import type { FormEvent } from "react"
import { Link } from "react-router-dom"
import { useContaCartaoAtual } from "@/features/cartao/hooks/useContaCartaoAtual"
import { useCriarContaCartao } from "@/features/cartao/hooks/useCriarContaCartao"
import { useSaldoCartao } from "@/features/cartao/hooks/useSaldoCartao"
import { useLancarCompra } from "@/features/cartao/hooks/useLancarCompra"
import { CartaoVisual } from "@/features/cartao/components/CartaoVisual"
import { CriarContaCartaoForm } from "@/features/cartao/components/CriarContaCartaoForm"
import { LancarCompraForm } from "@/features/cartao/components/LancarCompraForm"
import { FaturasSection } from "@/features/cartao/components/FaturasSection"
import { validarNovaContaCartao } from "@/features/cartao/lib/validarNovaContaCartao"
import { validarCompra } from "@/features/cartao/lib/validarCompra"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { ApiError } from "@/shared/api/client"

// Container: nao calcula nada de dominio (saldo vem pronto do backend), so
// orquestra estado de servidor (React Query) e estado de UI (formularios
// abertos) e decide qual apresentacao mostrar. A conta CARTAO "atual" vem de
// GET /api/contas?tipo=cartao (ver hooks/useContaCartaoAtual.ts) - apos criar
// uma conta, a invalidacao de cache dentro de useCriarContaCartao ja faz essa
// query refletir a conta nova, sem estado local proprio aqui.
export function ContaCartaoPage() {
  const {
    contaCartaoAtual,
    isLoading: carregandoContaCartao,
    isError: erroContaCartao,
  } = useContaCartaoAtual()
  const { mutate: criarContaCartao, isPending: criandoConta } = useCriarContaCartao()
  const {
    data: saldo,
    isLoading: carregandoSaldo,
    isError: erroSaldo,
    error: erroSaldoDetalhe,
  } = useSaldoCartao(contaCartaoAtual?.id ?? null)
  const { mutate: lancarCompra, isPending: lancandoCompra } = useLancarCompra()

  const [nome, setNome] = useState("")
  const [diaFechamento, setDiaFechamento] = useState("")
  const [diaVencimento, setDiaVencimento] = useState("")
  const [erroFormularioConta, setErroFormularioConta] = useState<string | null>(null)

  const [formularioCompraAberto, setFormularioCompraAberto] = useState(false)
  const [descricaoCompra, setDescricaoCompra] = useState("")
  const [valorCompra, setValorCompra] = useState("")
  const [dataCompra, setDataCompra] = useState("")
  const [erroFormularioCompra, setErroFormularioCompra] = useState<string | null>(null)

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
        onSuccess: () => {
          // A conta nova entra em useContaCartaoAtual pela invalidacao de
          // cache dentro de useCriarContaCartao - nao precisa guardar estado
          // local aqui.
          setErroFormularioConta(null)
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

  function abrirFormularioCompra() {
    setDescricaoCompra("")
    setValorCompra("")
    setDataCompra("")
    setErroFormularioCompra(null)
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

    lancarCompra(
      {
        contaId: contaCartaoAtual.id,
        request: {
          descricao: descricaoCompra.trim(),
          valor: Number(valorCompra.trim().replace(",", ".")),
          data: dataCompra,
          categoriaId: null,
        },
      },
      {
        onSuccess: fecharFormularioCompra,
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
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-[19px] font-medium text-text-primary">Cartao de credito</h1>
        <p className="text-sm text-text-muted">
          Compras entram por competencia; a fatura so fecha o saldo quando for totalmente paga.
        </p>
      </header>

      {carregandoContaCartao ? (
        <p className="text-sm text-text-muted">Carregando cartao...</p>
      ) : erroContaCartao ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar o cartao</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : contaCartaoAtual === null ? (
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
        <div className="flex flex-col gap-6">
          <CartaoVisual nome={contaCartaoAtual.nome} />

          <Card>
            <CardContent className="flex flex-col gap-1">
              <span className="text-[13px] text-text-muted">Saldo do cartao</span>
              {carregandoSaldo && <span className="text-sm text-text-muted">Calculando...</span>}
              {erroSaldo && (
                <span className="text-sm text-negativo">Nao foi possivel calcular o saldo.</span>
              )}
              {!carregandoSaldo && !erroSaldo && saldo !== undefined && (
                <span className="text-[28px] font-medium text-text-primary">
                  {formatarMoeda(saldo.saldo)}
                </span>
              )}
            </CardContent>
          </Card>

          <Button type="button" onClick={abrirFormularioCompra} className="self-start">
            Lancar compra
          </Button>

          <FaturasSection contaId={contaCartaoAtual.id} />

          <Link className="text-sm text-accent underline-offset-4 hover:underline" to="/cartao/relatorio">
            Ver relatorio por categoria
          </Link>
        </div>
      )}

      {formularioCompraAberto && (
        <LancarCompraForm
          descricao={descricaoCompra}
          valor={valorCompra}
          data={dataCompra}
          isSubmitting={lancandoCompra}
          errorMessage={erroFormularioCompra}
          onDescricaoChange={setDescricaoCompra}
          onValorChange={setValorCompra}
          onDataChange={setDataCompra}
          onSubmit={handleSubmitCompra}
          onFechar={fecharFormularioCompra}
        />
      )}
    </div>
  )
}
