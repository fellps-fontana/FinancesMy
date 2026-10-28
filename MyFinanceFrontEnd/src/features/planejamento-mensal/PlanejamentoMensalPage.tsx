import { useState } from "react"
import { Copy } from "lucide-react"
import { useSearchParams } from "react-router-dom"
import { ApiError } from "@/shared/api/client"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { usePlanoMensal } from "@/features/planejamento-mensal/hooks/usePlanoMensal"
import { useSalvarPlanoMensal } from "@/features/planejamento-mensal/hooks/useSalvarPlanoMensal"
import { AlertasPlano } from "@/features/planejamento-mensal/components/AlertasPlano"
import { CopiarPlanoModal } from "@/features/planejamento-mensal/components/CopiarPlanoModal"
import { ListaSublimites } from "@/features/planejamento-mensal/components/ListaSublimites"
import { NavegacaoMes } from "@/features/planejamento-mensal/components/NavegacaoMes"
import { ResumoPlano } from "@/features/planejamento-mensal/components/ResumoPlano"
import {
  deslocarMes,
  mesCorrente,
  podeAvancarMes,
  rotuloMes,
} from "@/features/planejamento-mensal/lib/mes"
import {
  alterarPercentualInvestimento,
  alterarPercentualSublimite,
  alterarRendaPrevista,
  alterarValorInvestimento,
  alterarValorSublimite,
  aplicarCopiaDePlano,
  calcularPlanoPreview,
  criarRascunho,
  listarExcessosPercentuais,
  montarRequest,
  podeSalvarRascunho,
  rascunhoLegivel,
  removerSublimite,
  temAlteracoes,
  type OpcoesCopiaPlano,
  type Rascunho,
} from "@/features/planejamento-mensal/lib/rascunho"
import type { PlanoMensalResponse } from "@/features/planejamento-mensal/types"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { PageHeader } from "@/shared/ui/page-header"

type EstadoEdicaoMes = {
  chaveMes: string
  rascunho: Rascunho
}

/**
 * Tela unificada de Planejamento Mensal e Limites de Gasto por Categoria
 * (regra-de-negocio.md itens 14 e 20).
 * Edicao inline com preview em tempo real, conversao bidirecional R$ <-> %
 * e copia de percentuais (e renda opcional) a partir de outro mes.
 */
export function PlanejamentoMensalPage() {
  const [periodo, setPeriodo] = useState(mesCorrente)
  const [edicao, setEdicao] = useState<EstadoEdicaoMes | null>(null)
  const [modalCopiaAberto, setModalCopiaAberto] = useState(false)
  const [versaoExpansao, setVersaoExpansao] = useState(0)
  const [searchParams, setSearchParams] = useSearchParams()

  const { ano, mes } = periodo
  const chaveMes = `${ano}-${mes}`
  const categoriaIdDestaque = searchParams.get("categoriaId")

  const { data: plano, isLoading, error } = usePlanoMensal(ano, mes)
  const { data: categoriasDespesa } = useCategorias("Despesa", false)
  const salvar = useSalvarPlanoMensal(ano, mes)

  if (error) {
    console.error("Falha ao carregar planejamento mensal", error)
  }

  const rascunhoBase = plano ? criarRascunho(plano, categoriasDespesa) : null
  const rascunhoAtual =
    edicao && edicao.chaveMes === chaveMes ? edicao.rascunho : rascunhoBase
  const preview =
    plano && rascunhoAtual ? calcularPlanoPreview(rascunhoAtual, plano) : null

  const podeEditarMes = plano != null && !plano.somenteLeitura
  const possuiAlteracoes =
    podeEditarMes && rascunhoAtual != null && temAlteracoes(rascunhoAtual, plano)

  const excessosPercentuais = rascunhoAtual
    ? listarExcessosPercentuais(rascunhoAtual.sublimites)
    : []
  const legivel = rascunhoAtual ? rascunhoLegivel(rascunhoAtual) : true
  const podeSalvar = rascunhoAtual ? podeSalvarRascunho(rascunhoAtual) : false

  function mudarMes(delta: number) {
    setPeriodo((atual) => deslocarMes(atual, delta))
    setEdicao(null)
    setModalCopiaAberto(false)
    salvar.reset()
  }

  function atualizarRascunho(transformacao: (atual: Rascunho) => Rascunho) {
    if (!rascunhoAtual) return
    salvar.reset()
    setEdicao({
      chaveMes,
      rascunho: transformacao(rascunhoAtual),
    })
  }

  function confirmarCopiaDeOutroMes(
    planoOrigem: PlanoMensalResponse,
    opcoes: OpcoesCopiaPlano,
  ) {
    if (!plano || !rascunhoAtual) return
    salvar.reset()
    setEdicao({
      chaveMes,
      rascunho: aplicarCopiaDePlano(rascunhoAtual, planoOrigem, plano.fixas, opcoes),
    })
    setVersaoExpansao((v) => v + 1)
    setModalCopiaAberto(false)
  }

  function descartarAlteracoes() {
    salvar.reset()
    setEdicao(null)
  }

  function salvarAlteracoes() {
    if (!rascunhoAtual) return
    const request = montarRequest(rascunhoAtual)
    if (!request) return
    salvar.mutate(request, {
      onSuccess: () => setEdicao(null),
    })
  }

  function limparDestaqueCategoria() {
    setSearchParams((atuais) => {
      const proximos = new URLSearchParams(atuais)
      proximos.delete("categoriaId")
      return proximos
    })
  }

  const itemDestacado = categoriaIdDestaque
    ? preview?.itens.find((i) => i.categoriaId === categoriaIdDestaque)
    : undefined

  const mensagemErroCarregamento =
    error instanceof ApiError
      ? error.message
      : "Não foi possível carregar o planejamento mensal."

  const mensagemErroSalvar =
    salvar.error instanceof ApiError
      ? salvar.error.message
      : salvar.error
        ? salvar.error.message
        : null

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <div className="flex flex-col gap-1">
        <PageHeader title="Planejamento mensal" />
        <p className="text-[13px] text-text-muted">
          Renda prevista, menos despesas fixas e investimento, distribuída em sublimites por
          categoria com acompanhamento em tempo real do gasto variável. Só alerta visual — nenhum
          lançamento é bloqueado.
        </p>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <NavegacaoMes
            rotulo={rotuloMes(periodo)}
            podeAvancar={podeAvancarMes(periodo)}
            onAnterior={() => mudarMes(-1)}
            onProximo={() => {
              if (podeAvancarMes(periodo)) mudarMes(1)
            }}
          />

          {podeEditarMes && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setModalCopiaAberto(true)}
            >
              <Copy className="size-3.5" />
              Copiar de outro mês
            </Button>
          )}
        </div>

        {possuiAlteracoes && (
          <div className="flex flex-wrap items-center gap-2 rounded-[10px] border border-accent-deep bg-accent-deep/20 px-3 py-2">
            <span className="text-[13px] text-accent-soft">Alterações não salvas</span>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={descartarAlteracoes}
              disabled={salvar.isPending}
            >
              Descartar
            </Button>
            <Button
              type="button"
              size="sm"
              onClick={salvarAlteracoes}
              disabled={!podeSalvar || salvar.isPending}
            >
              {salvar.isPending ? "Salvando..." : "Salvar alterações"}
            </Button>
          </div>
        )}
      </div>

      {podeEditarMes && modalCopiaAberto && (
        <CopiarPlanoModal
          key={`${chaveMes}-copia`}
          open={modalCopiaAberto}
          periodoAtual={periodo}
          onClose={() => setModalCopiaAberto(false)}
          onConfirmarCopia={confirmarCopiaDeOutroMes}
        />
      )}

      {categoriaIdDestaque && (
        <div className="flex items-center justify-between gap-3 rounded-[10px] border border-accent-deep bg-accent-deep/20 px-3 py-2">
          <span className="text-[13px] text-accent-soft">
            Destacando categoria: {itemDestacado?.nome ?? "categoria selecionada"}
          </span>
          <Button type="button" variant="ghost" size="xs" onClick={limparDestaqueCategoria}>
            Ver todas
          </Button>
        </div>
      )}

      {mensagemErroSalvar && (
        <Alert variant="destructive">
          <AlertTitle>Não foi possível salvar o plano</AlertTitle>
          <AlertDescription>{mensagemErroSalvar}</AlertDescription>
        </Alert>
      )}

      {!legivel && (
        <p className="w-fit rounded-[5px] bg-negativo/15 px-2.5 py-1 text-[12px] text-negativo">
          Preencha renda, investimento e percentuais com números válidos (0 a 100% para
          percentuais).
        </p>
      )}

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Não foi possível carregar o plano</AlertTitle>
          <AlertDescription>{mensagemErroCarregamento}</AlertDescription>
        </Alert>
      ) : isLoading || plano === undefined ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : plano === null || preview === null ? (
        <p className="text-sm text-text-muted">
          Não há plano para {rotuloMes(periodo)}. Meses passados sem plano continuam sem plano.
        </p>
      ) : (
        <>
          {plano.somenteLeitura && (
            <p className="text-[13px] text-text-faint">
              Mês passado: plano somente para consulta.
            </p>
          )}
          <AlertasPlano plano={preview} excessosPercentuais={excessosPercentuais} />
          <ResumoPlano
            plano={preview}
            rascunho={rascunhoAtual}
            onAlterarRenda={(valor) =>
              atualizarRascunho((atual) => alterarRendaPrevista(atual, plano.fixas, valor))
            }
            onAlterarPercentualInvestimento={(valor) =>
              atualizarRascunho((atual) =>
                alterarPercentualInvestimento(atual, plano.fixas, valor),
              )
            }
            onAlterarValorInvestimento={(valor) =>
              atualizarRascunho((atual) => alterarValorInvestimento(atual, plano.fixas, valor))
            }
          />
          <ListaSublimites
            key={`${chaveMes}-${versaoExpansao}`}
            plano={preview}
            categoriaIdDestaque={categoriaIdDestaque}
            onAlterarPercentual={(id, valor) =>
              atualizarRascunho((atual) =>
                alterarPercentualSublimite(atual, plano.fixas, id, valor),
              )
            }
            onAlterarValor={(id, valor) =>
              atualizarRascunho((atual) => alterarValorSublimite(atual, plano.fixas, id, valor))
            }
            onRemover={(id) =>
              atualizarRascunho((atual) => removerSublimite(atual, id))
            }
          />
        </>
      )}
    </div>
  )
}
