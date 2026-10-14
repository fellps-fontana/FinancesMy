import { Fragment, useEffect, useMemo, useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { Plus } from "lucide-react"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { Button } from "@/shared/ui/button"
import { PageHeader } from "@/shared/ui/page-header"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { mesAtualIso } from "@/shared/lib/datas"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { construirMapaCategorias } from "@/features/categorias/lib/construirMapaCategorias"
import { useFluxoCaixaTodasContas } from "@/features/lancamentos/hooks/useFluxoCaixaTodasContas"
import { useMarcarComoPago } from "@/features/lancamentos/hooks/useMarcarComoPago"
import {
  agruparLancamentosPorData,
  calcularResumoLancamentos,
  filtrarLancamentosDoMes,
  filtrarPorTipo,
  somarMeses,
  type FiltroTipoLancamento as FiltroTipoLancamentoValor,
} from "@/features/lancamentos/lib/filtrarPeriodo"
import { AcoesLancamentoSheet } from "@/features/lancamentos/components/AcoesLancamentoSheet"
import { CancelarCompraParceladaDialog } from "@/features/lancamentos/components/CancelarCompraParceladaDialog"
import { ExcluirLancamentoDialog } from "@/features/lancamentos/components/ExcluirLancamentoDialog"
import { FiltroTipoLancamento } from "@/features/lancamentos/components/FiltroTipoLancamento"
import { FormLancamento } from "@/features/lancamentos/components/FormLancamento"
import { FormTransferencia } from "@/features/lancamentos/components/FormTransferencia"
import {
  GRADE_COLUNAS_LANCAMENTO,
  LancamentoItem,
} from "@/features/lancamentos/components/LancamentoItem"
import { mensagemDeErro } from "@/features/lancamentos/lib/mensagemDeErro"
import {
  contarParcelasPorCompra,
  SEM_PARCELAS,
  type ContagemParcelas,
} from "@/features/lancamentos/lib/contarParcelasPorCompra"
import { NavegadorMes } from "@/features/lancamentos/components/NavegadorMes"
import { TransferenciaFluxoCaixaItem } from "@/features/lancamentos/components/TransferenciaFluxoCaixaItem"
import type { LancamentoResponse } from "@/features/lancamentos/types"
import { cn } from "@/shared/lib/utils"

// Qual janela esta aberta. `null` fecha tudo. "acoes" e a folha mobile que
// leva a editar/excluir/cancelar compra.
type Dialogo =
  | { tipo: "novo" }
  | { tipo: "transferencia" }
  | { tipo: "acoes"; lancamento: LancamentoResponse }
  | { tipo: "editar"; lancamento: LancamentoResponse }
  | { tipo: "excluir"; lancamento: LancamentoResponse }
  | { tipo: "cancelarCompra"; lancamento: LancamentoResponse }
  | null

// `?novo=` e enviado pelos atalhos do dashboard (AcoesRapidas).
function dialogoInicial(novo: string | null): Dialogo {
  if (novo === "transferencia") return { tipo: "transferencia" }
  if (novo === "lancamento") return { tipo: "novo" }
  return null
}

// Container da tela Lancamentos (mockup s_lancamentos): visao agregada de
// todas as contas, recorte por mes, chips Todos/Entradas/Saidas e totais do
// mes. Desktop = tabela em largura total; mobile = lista por dia com folha de
// acoes. Nenhum calculo de dominio aqui: recorte, resumo e agrupamento vem de
// lib/filtrarPeriodo.ts; habilitacao de editar/excluir vem de `avulso`.
export function LancamentosPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()

  const [dialogo, setDialogo] = useState<Dialogo>(() => dialogoInicial(searchParams.get("novo")))
  const [mesReferencia, setMesReferencia] = useState(() => mesAtualIso())
  const [filtroTipo, setFiltroTipo] = useState<FiltroTipoLancamentoValor>("TODOS")
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const { data: itens, isLoading: carregandoItens, error: erroItens } = useFluxoCaixaTodasContas()
  const { data: categoriasAtivas } = useCategorias(undefined, false)
  const { data: categoriasArquivadas } = useCategorias(undefined, true)
  const { mutate: marcarComoPago, isPending: marcandoComoPago } = useMarcarComoPago()

  // Consome o atalho `?novo=` uma vez, para atualizar a pagina nao reabrir o formulario.
  useEffect(() => {
    if (!searchParams.has("novo")) return
    const restantes = new URLSearchParams(searchParams)
    restantes.delete("novo")
    setSearchParams(restantes, { replace: true })
  }, [searchParams, setSearchParams])

  if (erroItens) {
    console.error("Falha ao carregar fluxo de caixa", erroItens)
  }

  const [ano, mes] = mesReferencia.split("-").map(Number)

  const itensDoMes = useMemo(
    () => filtrarLancamentosDoMes(itens ?? [], ano, mes),
    [itens, ano, mes],
  )
  // Os totais representam o mes inteiro, independente do chip; transferencia
  // nunca entra (itens 3 e 12).
  const resumo = useMemo(() => calcularResumoLancamentos(itensDoMes), [itensDoMes])
  const gruposPorData = useMemo(
    () => agruparLancamentosPorData(filtrarPorTipo(itensDoMes, filtroTipo)),
    [itensDoMes, filtroTipo],
  )
  const contagemPorCompra = useMemo(() => contarParcelasPorCompra(itens ?? []), [itens])
  const mapaCategorias = useMemo(
    () => construirMapaCategorias([...(categoriasAtivas ?? []), ...(categoriasArquivadas ?? [])]),
    [categoriasAtivas, categoriasArquivadas],
  )

  function fechar() {
    setDialogo(null)
  }

  function handleMarcarComoPago(lancamento: LancamentoResponse) {
    marcarComoPago(
      { contaId: lancamento.contaId, lancamentoId: lancamento.id },
      {
        onSuccess: () => {
          setErroAcao(null)
          fechar()
        },
        onError: (error) => {
          console.error("Falha ao marcar lancamento como pago", error)
          setErroAcao(mensagemDeErro(error, "Nao foi possivel marcar o lancamento como pago."))
          fechar()
        },
      },
    )
  }

  function handleIrParaOrigem(rota: string) {
    fechar()
    navigate(rota)
  }

  function contagemDa(lancamento: LancamentoResponse): ContagemParcelas {
    return (
      (lancamento.compraParceladaId && contagemPorCompra.get(lancamento.compraParceladaId)) ||
      SEM_PARCELAS
    )
  }

  const lancamentoAcoes = dialogo?.tipo === "acoes" ? dialogo.lancamento : null
  const lancamentoEditando = dialogo?.tipo === "editar" ? dialogo.lancamento : null
  const lancamentoExcluindo = dialogo?.tipo === "excluir" ? dialogo.lancamento : null
  const lancamentoCancelando = dialogo?.tipo === "cancelarCompra" ? dialogo.lancamento : null

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Lancamentos"
        action={
          <Button
            type="button"
            onClick={() => setDialogo({ tipo: "novo" })}
            className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
          >
            <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
            Novo lancamento
          </Button>
        }
      />

      <div className="flex flex-col gap-4 md:flex-row md:flex-wrap md:items-center md:gap-2.5">
        <NavegadorMes
          mesReferencia={mesReferencia}
          onMesAnterior={() => setMesReferencia((atual) => somarMeses(atual, -1))}
          onProximoMes={() => setMesReferencia((atual) => somarMeses(atual, 1))}
        />

        {/* Mobile: tres cards Entradas/Saidas/Saldo (mockup mobile). */}
        <div className="grid grid-cols-3 gap-2.5 md:hidden">
          <div className="rounded-xl border-[0.5px] border-border bg-card p-3">
            <div className="text-xs text-positivo">Entradas</div>
            <div className="mt-1 text-base font-medium text-text-primary">
              {formatarMoeda(resumo.totalEntradas)}
            </div>
          </div>
          <div className="rounded-xl border-[0.5px] border-border bg-card p-3">
            <div className="text-xs text-negativo">Saidas</div>
            <div className="mt-1 text-base font-medium text-text-primary">
              {formatarMoeda(resumo.totalSaidas)}
            </div>
          </div>
          <div className="rounded-xl border-[0.5px] border-border bg-card p-3">
            <div className="text-xs text-text-muted">Saldo</div>
            <div className="mt-1 text-base font-medium text-text-primary">
              {formatarMoeda(resumo.saldo)}
            </div>
          </div>
        </div>

        <FiltroTipoLancamento valor={filtroTipo} onChange={setFiltroTipo} />

        {/* Desktop: totais em linha, a direita (mockup s_lancamentos). */}
        <div className="hidden gap-[18px] text-[13px] md:ml-auto md:flex">
          <div>
            <span className="text-positivo">Entradas </span>
            <span className="text-text-primary">{formatarMoeda(resumo.totalEntradas)}</span>
          </div>
          <div>
            <span className="text-negativo">Saidas </span>
            <span className="text-text-primary">{formatarMoeda(resumo.totalSaidas)}</span>
          </div>
        </div>
      </div>

      {erroAcao && (
        <Alert variant="destructive">
          <AlertDescription>{erroAcao}</AlertDescription>
        </Alert>
      )}

      {erroItens ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os lancamentos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : carregandoItens ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : gruposPorData.length > 0 ? (
        <div className="md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
          <div
            className={cn(
              "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted md:grid",
              GRADE_COLUNAS_LANCAMENTO,
            )}
          >
            <span>Descricao</span>
            <span>Categoria</span>
            <span>Data</span>
            <span className="text-right">Valor</span>
            <span />
          </div>
          <ul>
            {gruposPorData.map((grupo) => (
              <Fragment key={grupo.data}>
                <li className="pt-3 pb-1.5 text-xs text-text-faint first:pt-0 md:hidden">
                  {grupo.rotulo}
                </li>
                {grupo.itens.map((item) =>
                  item.tipoItem === "LANCAMENTO" ? (
                    <LancamentoItem
                      key={item.lancamento.id}
                      lancamento={item.lancamento}
                      categoria={
                        item.lancamento.categoriaId
                          ? (mapaCategorias[item.lancamento.categoriaId] ?? null)
                          : null
                      }
                      podeCancelarCompra={contagemDa(item.lancamento).pendentes > 0}
                      marcandoComoPago={marcandoComoPago}
                      onEditar={(lancamento) => setDialogo({ tipo: "editar", lancamento })}
                      onExcluir={(lancamento) => setDialogo({ tipo: "excluir", lancamento })}
                      onCancelarCompra={(lancamento) =>
                        setDialogo({ tipo: "cancelarCompra", lancamento })
                      }
                      onMarcarComoPago={handleMarcarComoPago}
                      onAbrirAcoes={(lancamento) => setDialogo({ tipo: "acoes", lancamento })}
                    />
                  ) : (
                    <TransferenciaFluxoCaixaItem
                      key={item.transferencia.id}
                      transferencia={item.transferencia}
                    />
                  ),
                )}
              </Fragment>
            ))}
          </ul>
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum lancamento neste mes.</p>
      )}

      <BottomSheet open={dialogo?.tipo === "novo"} onClose={fechar} title="Novo lancamento">
        <FormLancamento onSalvar={fechar} onCancelar={fechar} />
      </BottomSheet>

      <BottomSheet open={dialogo?.tipo === "transferencia"} onClose={fechar} title="Nova transferencia">
        <FormTransferencia onSalvar={fechar} onCancelar={fechar} />
      </BottomSheet>

      <BottomSheet open={lancamentoEditando !== null} onClose={fechar} title="Editar lancamento">
        {lancamentoEditando && (
          <FormLancamento
            lancamentoParaEditar={lancamentoEditando}
            onSalvar={fechar}
            onCancelar={fechar}
          />
        )}
      </BottomSheet>

      <AcoesLancamentoSheet
        lancamento={lancamentoAcoes}
        podeCancelarCompra={lancamentoAcoes !== null && contagemDa(lancamentoAcoes).pendentes > 0}
        marcandoComoPago={marcandoComoPago}
        onFechar={fechar}
        onEditar={(lancamento) => setDialogo({ tipo: "editar", lancamento })}
        onExcluir={(lancamento) => setDialogo({ tipo: "excluir", lancamento })}
        onCancelarCompra={(lancamento) => setDialogo({ tipo: "cancelarCompra", lancamento })}
        onMarcarComoPago={handleMarcarComoPago}
        onIrParaOrigem={handleIrParaOrigem}
      />

      <ExcluirLancamentoDialog lancamento={lancamentoExcluindo} onFechar={fechar} />

      <CancelarCompraParceladaDialog
        lancamento={lancamentoCancelando}
        contagem={lancamentoCancelando ? contagemDa(lancamentoCancelando) : SEM_PARCELAS}
        onFechar={fechar}
      />

      {/* FAB mobile (mockup mobile): no desktop o botao fica no cabecalho. */}
      <button
        type="button"
        onClick={() => setDialogo({ tipo: "novo" })}
        aria-label="Novo lancamento"
        className="fixed right-5 bottom-24 z-10 flex size-13 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-lg shadow-primary/35 md:hidden"
      >
        <Plus className="size-5" strokeWidth={2} aria-hidden="true" />
      </button>
    </div>
  )
}
