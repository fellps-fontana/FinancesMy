import { useMemo, useState } from "react"
import { Link } from "react-router-dom"
import { CreditCard, HandCoins, Receipt } from "lucide-react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { PageHeader } from "@/shared/ui/page-header"
import { SegmentedTabs } from "@/shared/ui/segmented-tabs"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { useContasReceber } from "@/features/contas-receber/hooks/useContasReceber"
import { useFluxoCaixaTodasContas } from "@/features/lancamentos/hooks/useFluxoCaixaTodasContas"
import { useMarcarComoPago } from "@/features/lancamentos/hooks/useMarcarComoPago"
import type { LancamentoResponse } from "@/features/lancamentos/types"
import { ItemContaAPagar } from "@/features/conciliacao/components/ItemContaAPagar"
import {
  ehCompraCartaoPendente,
  ehContaAPagar,
  ehPagoDoMes,
  resumirContasAbertas,
} from "@/features/conciliacao/lib/situacaoDoVencimento"
import { situacaoDoVencimento } from "@/shared/lib/situacaoDoVencimento"

type Aba = "A_PAGAR" | "A_RECEBER" | "CONCILIADO"

const ABAS = [
  { value: "A_PAGAR", label: "A pagar" },
  { value: "A_RECEBER", label: "A receber" },
  { value: "CONCILIADO", label: "Conciliado" },
] as const

const STATUS_RECEBIDO = "RECEBIDO"

function porData(a: LancamentoResponse, b: LancamentoResponse) {
  return a.data.localeCompare(b.data)
}

// Container roteado (/conciliacao). v1 (regra-de-negocio.md item 5 / Escopo
// v1 vs v2): so o caminho manual PENDENTE -> PAGO; sugestao de conciliacao
// com extrato (SUGERIDO) e v2 e NAO e renderizada.
export function ConciliacaoPage() {
  const [aba, setAba] = useState<Aba>("A_PAGAR")
  const [erroPagamento, setErroPagamento] = useState<{ id: string; mensagem: string } | null>(null)

  const fluxo = useFluxoCaixaTodasContas()
  const receber = useContasReceber()
  const { mutate: marcarComoPago, isPending: pagando, variables: pagandoVars } = useMarcarComoPago()

  const hoje = useMemo(() => new Date(), [])
  const anoAtual = hoje.getFullYear()
  const mesAtual = hoje.getMonth() + 1

  const itens = useMemo(() => fluxo.data ?? [], [fluxo.data])

  const contasAPagar = useMemo(
    () => itens.filter(ehContaAPagar).map((i) => i.lancamento).sort(porData),
    [itens],
  )

  // Compras de cartao pendentes viram uma unica linha: o pagamento e da fatura
  // em /cartao (item 12), aqui so se sinaliza o total e o vencimento mais proximo.
  const cartao = useMemo(() => {
    const compras = itens.filter(ehCompraCartaoPendente).map((i) => i.lancamento)
    if (compras.length === 0) return null
    return {
      valor: compras.reduce((soma, c) => soma + c.valor, 0),
      vencimento: compras.map((c) => c.data).sort()[0],
    }
  }, [itens])

  const pagosDoMes = useMemo(
    () =>
      itens
        .filter((i) => ehPagoDoMes(i, anoAtual, mesAtual))
        .map((i) => i.lancamento)
        .sort(porData),
    [itens, anoAtual, mesAtual],
  )

  const contasAReceber = useMemo(
    () => (receber.data ?? []).filter((c) => c.status !== STATUS_RECEBIDO),
    [receber.data],
  )

  const resumo = useMemo(() => {
    if (aba === "A_RECEBER") {
      return resumirContasAbertas(
        contasAReceber.map((c) => ({ valor: c.saldoPendente, vencimento: c.dataPrevista })),
        hoje,
      )
    }
    const linhas: Array<{ valor: number; vencimento: string | null }> = contasAPagar.map((l) => ({
      valor: l.valor,
      vencimento: l.data,
    }))
    if (cartao) linhas.push({ valor: cartao.valor, vencimento: cartao.vencimento })
    return resumirContasAbertas(linhas, hoje)
  }, [aba, contasAPagar, cartao, contasAReceber, hoje])

  function pagar(lancamento: LancamentoResponse) {
    setErroPagamento(null)
    marcarComoPago(
      { contaId: lancamento.contaId, lancamentoId: lancamento.id },
      {
        onError: (error) => {
          console.error("Falha ao marcar lancamento como pago", error)
          setErroPagamento({
            id: lancamento.id,
            mensagem: error instanceof ApiError ? error.message : "Nao foi possivel marcar como pago. Tente novamente.",
          })
        },
      },
    )
  }

  const carregando = aba === "A_RECEBER" ? receber.isLoading : fluxo.isLoading
  const erro = aba === "A_RECEBER" ? receber.error : fluxo.error
  const situacaoCartao = cartao ? situacaoDoVencimento(cartao.vencimento, hoje) : null

  const vazio =
    (aba === "A_PAGAR" && contasAPagar.length === 0 && !cartao) ||
    (aba === "A_RECEBER" && contasAReceber.length === 0) ||
    (aba === "CONCILIADO" && pagosDoMes.length === 0)

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader title="Conciliacao / contas a pagar" />

      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between md:gap-5">
        <SegmentedTabs options={ABAS} value={aba} onChange={setAba} ariaLabel="Visao de conciliacao" className="md:w-fit" />
        {aba !== "CONCILIADO" ? (
          <div className="flex justify-between rounded-[14px] border-[0.5px] border-border bg-card p-4 md:gap-[18px] md:rounded-none md:border-0 md:bg-transparent md:p-0">
            <div className="flex flex-col md:flex-row md:gap-1">
              <span className="text-xs text-text-muted md:text-[13px]">Em aberto</span>
              <span className="mt-1 text-[19px] font-medium text-text-primary md:mt-0 md:text-[13px] md:font-normal">
                {formatarMoeda(resumo.emAberto)}
              </span>
            </div>
            <div className="flex flex-col items-end md:flex-row md:gap-1">
              <span className="text-xs text-text-muted md:text-[13px]">Atrasado</span>
              <span className="mt-1 text-[19px] font-medium text-text-primary md:mt-0 md:text-[13px] md:font-normal">
                {formatarMoeda(resumo.atrasado)}
              </span>
            </div>
          </div>
        ) : null}
      </div>

      {erro ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os dados</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : carregando ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : (
        <div className="md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
          <div className="hidden grid-cols-[2.4fr_1fr_1fr_1fr_0.8fr] border-b-[0.5px] border-border px-[22px] py-3.5 md:grid">
            <span className="text-xs text-text-muted">Descricao</span>
            <span className="text-xs text-text-muted">{aba === "CONCILIADO" ? "Data" : "Vencimento"}</span>
            <span className="text-right text-xs text-text-muted">Valor</span>
            <span className="text-right text-xs text-text-muted">Status</span>
            <span aria-hidden="true" />
          </div>

          <ul className="flex flex-col gap-2.5 md:block">
            {aba === "A_PAGAR" ? (
              <>
                {contasAPagar.map((lancamento) => {
                  const situacao = situacaoDoVencimento(lancamento.data, hoje)
                  const pagandoEste = pagando && pagandoVars?.lancamentoId === lancamento.id
                  return (
                    <ItemContaAPagar
                      key={lancamento.id}
                      descricao={lancamento.descricao ?? "Sem descricao"}
                      icone={Receipt}
                      rotuloVencimento={situacao.rotulo}
                      vencimentoAtrasado={situacao.situacao === "atrasado"}
                      valor={lancamento.valor}
                      statusLabel={situacao.situacao === "atrasado" ? "Atrasado" : "Pendente"}
                      statusVariant={situacao.situacao}
                      erro={erroPagamento?.id === lancamento.id ? erroPagamento.mensagem : null}
                      acao={
                        <Button type="button" variant="link" size="sm" disabled={pagandoEste} onClick={() => pagar(lancamento)}>
                          Pagar
                        </Button>
                      }
                    />
                  )
                })}
                {cartao && situacaoCartao ? (
                  <ItemContaAPagar
                    descricao="Cartao de credito"
                    icone={CreditCard}
                    rotuloVencimento={situacaoCartao.rotulo}
                    vencimentoAtrasado={situacaoCartao.situacao === "atrasado"}
                    valor={cartao.valor}
                    statusLabel={situacaoCartao.situacao === "atrasado" ? "Atrasado" : "Pendente"}
                    statusVariant={situacaoCartao.situacao}
                    acao={
                      <Button asChild variant="link" size="sm">
                        <Link to="/cartao">Pagar</Link>
                      </Button>
                    }
                  />
                ) : null}
              </>
            ) : null}

            {aba === "A_RECEBER"
              ? contasAReceber.map((conta) => {
                  const situacao = conta.dataPrevista ? situacaoDoVencimento(conta.dataPrevista, hoje) : null
                  return (
                    <ItemContaAPagar
                      key={conta.id}
                      descricao={conta.pessoa ? `${conta.descricao} - ${conta.pessoa}` : conta.descricao}
                      icone={HandCoins}
                      rotuloVencimento={conta.dataPrevista ? `Previsto ${formatarData(conta.dataPrevista)}` : "Sem previsao"}
                      vencimentoAtrasado={situacao?.situacao === "atrasado"}
                      valor={conta.saldoPendente}
                      statusLabel={situacao?.situacao === "atrasado" ? "Atrasado" : "Pendente"}
                      statusVariant={situacao?.situacao ?? "pendente"}
                      acao={
                        <Button asChild variant="link" size="sm">
                          <Link to="/contas-receber">Receber</Link>
                        </Button>
                      }
                    />
                  )
                })
              : null}

            {aba === "CONCILIADO"
              ? pagosDoMes.map((lancamento) => (
                  <ItemContaAPagar
                    key={lancamento.id}
                    descricao={lancamento.descricao ?? "Sem descricao"}
                    icone={Receipt}
                    rotuloVencimento={formatarData(lancamento.data)}
                    valor={lancamento.valor}
                    statusLabel="Pago"
                    statusVariant="pago"
                  />
                ))
              : null}
          </ul>

          {vazio ? (
            <p className="px-1 py-4 text-sm text-text-muted md:px-[22px]">
              {aba === "A_PAGAR"
                ? "Nenhuma conta a pagar em aberto."
                : aba === "A_RECEBER"
                  ? "Nenhuma conta a receber em aberto."
                  : "Nenhum pagamento conciliado neste mes."}
            </p>
          ) : null}
        </div>
      )}
    </div>
  )
}
