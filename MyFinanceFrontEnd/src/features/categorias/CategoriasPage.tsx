import { useMemo, useState } from "react"
import { Archive, Pencil, Plus, RotateCcw } from "lucide-react"
import { ApiError } from "@/shared/api/client"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { useArquivarCategoria } from "@/features/categorias/hooks/useArquivarCategoria"
import { useReativarCategoria } from "@/features/categorias/hooks/useReativarCategoria"
import { useLimitesGasto } from "@/features/limite-gasto/hooks/useLimitesGasto"
import { useGastoVsLimiteTodasCategorias } from "@/features/limite-gasto/hooks/useGastoVsLimiteTodasCategorias"
import { CategoriaItem } from "@/features/categorias/components/CategoriaItem"
import { FormCategoria } from "@/features/categorias/FormCategoria"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { FormModal } from "@/shared/ui/FormModal"
import { Button } from "@/shared/ui/button"
import { Chip } from "@/shared/ui/chip"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { PageHeader } from "@/shared/ui/page-header"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"
import type { GastoVsLimiteResponse, LimiteGastoResponse } from "@/features/limite-gasto/types"

const TIPO_PADRAO: TipoCategoria = "Despesa"

const ABAS: ReadonlyArray<{ tipo: TipoCategoria; rotulo: string }> = [
  { tipo: "Despesa", rotulo: "Despesas" },
  { tipo: "Receita", rotulo: "Receitas" },
]

// Um unico estado cobre qual superficie esta aberta: formulario (criar/editar),
// folha de acoes (mobile) ou confirmacao de arquivamento.
type Superficie =
  | { tipo: "criar" }
  | { tipo: "editar"; categoria: CategoriaResponse }
  | { tipo: "acoes"; categoria: CategoriaResponse }
  | { tipo: "arquivar"; categoria: CategoriaResponse }
  | null

// Container roteado: le estado de servidor (categorias, limites e
// gasto-vs-limite do mes corrente via React Query) e orquestra as superficies
// de UI. Gasto, percentual e `estourado` chegam prontos do backend
// (regra-de-negocio.md item 14); renderizacao fica em CategoriaItem.
// Nova subcategoria: escolher a categoria-pai no proprio form (item 7).
export function CategoriasPage() {
  const [tipoSelecionado, setTipoSelecionado] = useState<TipoCategoria>(TIPO_PADRAO)
  const [superficie, setSuperficie] = useState<Superficie>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const {
    data: categorias,
    isLoading: carregandoCategorias,
    error: erroCategorias,
  } = useCategorias(tipoSelecionado)

  const { data: limites, error: erroLimites } = useLimitesGasto()

  const hoje = new Date()
  const { data: gastosVsLimite, error: erroGastos } = useGastoVsLimiteTodasCategorias(
    hoje.getFullYear(),
    hoje.getMonth() + 1,
  )

  const { mutate: arquivar, isPending: arquivando } = useArquivarCategoria()
  const { mutate: reativar } = useReativarCategoria()

  if (erroCategorias) console.error("Falha ao carregar categorias", erroCategorias)
  if (erroLimites) console.error("Falha ao carregar limites de gasto", erroLimites)
  if (erroGastos) console.error("Falha ao carregar gasto vs limite", erroGastos)

  const limitesPorCategoriaId = useMemo(() => {
    const mapa: Record<string, LimiteGastoResponse> = {}
    for (const limite of limites ?? []) mapa[limite.categoriaId] = limite
    return mapa
  }, [limites])

  const gastoVsLimitePorCategoriaId = useMemo(() => {
    const mapa: Record<string, GastoVsLimiteResponse> = {}
    for (const item of gastosVsLimite ?? []) mapa[item.categoriaId] = item
    return mapa
  }, [gastosVsLimite])

  const categoriasTopLevel = (categorias ?? []).filter((categoria) => !categoria.parentId)

  function fechar() {
    setSuperficie(null)
  }

  function handleTrocarTipo(novoTipo: TipoCategoria) {
    setTipoSelecionado(novoTipo)
    setSuperficie(null)
  }

  function mensagemDeErro(error: unknown, padrao: string): string {
    return error instanceof ApiError ? error.message : padrao
  }

  function handleConfirmarArquivar(categoria: CategoriaResponse) {
    setErroAcao(null)
    arquivar(categoria.id, {
      onSuccess: fechar,
      onError: (error) => {
        console.error("Falha ao arquivar categoria", error)
        fechar()
        setErroAcao(mensagemDeErro(error, "Nao foi possivel arquivar a categoria. Tente novamente."))
      },
    })
  }

  function handleReativar(categoria: CategoriaResponse) {
    setErroAcao(null)
    fechar()
    reativar(categoria.id, {
      onError: (error) => {
        console.error("Falha ao reativar categoria", error)
        setErroAcao(mensagemDeErro(error, "Nao foi possivel reativar a categoria. Tente novamente."))
      },
    })
  }

  const formularioAberto = superficie?.tipo === "criar" || superficie?.tipo === "editar"

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Categorias"
        action={
          <>
            <Button
              type="button"
              onClick={() => setSuperficie({ tipo: "criar" })}
              className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
            >
              <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
              Nova categoria
            </Button>
            <Button
              type="button"
              onClick={() => setSuperficie({ tipo: "criar" })}
              aria-label="Nova categoria"
              className="size-8 rounded-[9px] p-0 md:hidden"
            >
              <Plus className="size-4" strokeWidth={2} aria-hidden="true" />
            </Button>
          </>
        }
      />

      <div className="flex gap-2" role="group" aria-label="Tipo de categoria">
        {ABAS.map(({ tipo, rotulo }) => (
          <Chip
            key={tipo}
            selected={tipoSelecionado === tipo}
            onClick={() => handleTrocarTipo(tipo)}
            className="px-3.5 py-2 md:text-[13px]"
          >
            {rotulo}
          </Chip>
        ))}
      </div>

      {erroAcao && (
        <Alert variant="destructive">
          <AlertDescription>{erroAcao}</AlertDescription>
        </Alert>
      )}

      {erroCategorias ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as categorias</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : carregandoCategorias ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : categoriasTopLevel.length > 0 ? (
        <div className="grid grid-cols-1 content-start gap-3 md:grid-cols-2 md:gap-x-6 md:gap-y-4">
          {categoriasTopLevel.map((categoria) => (
            <CategoriaItem
              key={categoria.id}
              categoria={categoria}
              gastoVsLimitePorCategoriaId={gastoVsLimitePorCategoriaId}
              onEditar={(c) => setSuperficie({ tipo: "editar", categoria: c })}
              onArquivar={(c) => setSuperficie({ tipo: "arquivar", categoria: c })}
              onReativar={handleReativar}
              onAbrirAcoes={(c) => setSuperficie({ tipo: "acoes", categoria: c })}
            />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma categoria cadastrada ainda.</p>
      )}

      {/* Mesmo form em criar/editar; `key` remonta o estado ao trocar de alvo. */}
      <FormModal
        open={formularioAberto}
        onClose={fechar}
        title={superficie?.tipo === "editar" ? "Editar categoria" : "Nova categoria"}
      >
        {superficie?.tipo === "editar" ? (
          <FormCategoria
            key={superficie.categoria.id}
            categoriaParaEditar={superficie.categoria}
            limiteAtual={limitesPorCategoriaId[superficie.categoria.id] ?? null}
            onSalvar={fechar}
            onCancelar={fechar}
          />
        ) : (
          <FormCategoria key="nova" onSalvar={fechar} onCancelar={fechar} />
        )}
      </FormModal>

      {/* Folha de acoes (mobile): toque na linha. */}
      <BottomSheet
        open={superficie?.tipo === "acoes"}
        onClose={fechar}
        title={superficie?.tipo === "acoes" ? superficie.categoria.nome : "Acoes"}
      >
        {superficie?.tipo === "acoes" && (
          <div className="flex flex-col gap-1">
            <Button
              type="button"
              variant="ghost"
              size="lg"
              className="h-11 justify-start gap-3 px-3 text-text-body"
              onClick={() => setSuperficie({ tipo: "editar", categoria: superficie.categoria })}
            >
              <Pencil className="text-text-muted" aria-hidden="true" />
              Editar
            </Button>
            {superficie.categoria.arquivada ? (
              <Button
                type="button"
                variant="ghost"
                size="lg"
                className="h-11 justify-start gap-3 px-3 text-text-body"
                onClick={() => handleReativar(superficie.categoria)}
              >
                <RotateCcw className="text-text-muted" aria-hidden="true" />
                Reativar
              </Button>
            ) : (
              <Button
                type="button"
                variant="ghost"
                size="lg"
                className="h-11 justify-start gap-3 px-3 text-negativo"
                onClick={() => setSuperficie({ tipo: "arquivar", categoria: superficie.categoria })}
              >
                <Archive aria-hidden="true" />
                Arquivar
              </Button>
            )}
          </div>
        )}
      </BottomSheet>

      <ConfirmDialog
        open={superficie?.tipo === "arquivar"}
        title={superficie?.tipo === "arquivar" ? `Arquivar ${superficie.categoria.nome}?` : "Arquivar"}
        description="Ela sai das listas de escolha. Os lancamentos antigos continuam."
        icon={<Archive className="size-[18px]" />}
        destructive
        confirmLabel="Arquivar"
        loading={arquivando}
        onCancel={fechar}
        onConfirm={() => {
          if (superficie?.tipo === "arquivar") handleConfirmarArquivar(superficie.categoria)
        }}
      />
    </div>
  )
}
