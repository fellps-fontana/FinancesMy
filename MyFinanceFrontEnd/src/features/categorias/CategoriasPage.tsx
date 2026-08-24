import { useMemo, useState } from "react"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { useLimitesGasto } from "@/features/limite-gasto/hooks/useLimitesGasto"
import { CategoriaItem } from "@/features/categorias/components/CategoriaItem"
import { FormCategoria } from "@/features/categorias/FormCategoria"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Modal } from "@/shared/ui/Modal"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"
import type { LimiteGastoResponse } from "@/features/limite-gasto/types"

const TIPO_PADRAO: TipoCategoria = "Despesa"

// Estado de qual formulario esta aberto (nao ha estado de servidor aqui, so
// orquestracao de UI - clean-code.md "Organizacao (React)"): `null` fecha o
// form; `{ modo: "criar" }` abre o FormCategoria sem `categoriaParaEditar`;
// `{ modo: "editar", categoria }` reaproveita o mesmo form em modo edicao.
// Um unico estado cobre os dois toggles (Nova categoria / onEditar) sem
// precisar de duas flags booleanas concorrentes.
type EstadoFormulario = { modo: "criar" } | { modo: "editar"; categoria: CategoriaResponse } | null

// Container roteado: le estado de servidor (React Query, via useCategorias e
// useLimitesGasto) e decide qual formulario esta aberto. Renderizacao pura de
// cada categoria/subcategoria fica em CategoriaItem (componente recursivo,
// regra-de-negocio.md item 7) - ver clean-code.md "Organizacao (React)".
//
// Limite de gasto (item 14): useLimitesGasto() busca a lista inteira (sem
// filtro de tipo - categoria RECEITA nunca tem limite, entao o mapa so tera
// entradas de categorias DESPESA) e monta um mapa categoriaId -> limite. Essa
// montagem e indexacao de dado ja pronto, nao calculo de dominio (o
// percentual/estouro em si vem de GastoVsLimiteResponse, consumido dentro de
// CampoLimiteGasto via CategoriaItem).
//
// Nova subcategoria: nao ha atalho dedicado nesta tela. O FormCategoria de
// criar ja permite escolher categoria-pai (select "Categoria-pai"), entao
// criar uma subcategoria e so escolher o pai no mesmo form de "Nova
// categoria" - decisao documentada aqui pra nao adicionar um segundo ponto de
// entrada que faria a mesma chamada (POST /api/categorias com parentId).
export function CategoriasPage() {
  const [tipoSelecionado, setTipoSelecionado] = useState<TipoCategoria>(TIPO_PADRAO)
  const [formulario, setFormulario] = useState<EstadoFormulario>(null)

  const {
    data: categorias,
    isLoading: carregandoCategorias,
    error: erroCategorias,
  } = useCategorias(tipoSelecionado)

  const {
    data: limites,
    error: erroLimites,
  } = useLimitesGasto()

  if (erroCategorias) {
    console.error("Falha ao carregar categorias", erroCategorias)
  }

  if (erroLimites) {
    console.error("Falha ao carregar limites de gasto", erroLimites)
  }

  const limitesPorCategoriaId = useMemo(() => {
    const mapa: Record<string, LimiteGastoResponse> = {}
    for (const limite of limites ?? []) {
      mapa[limite.categoriaId] = limite
    }
    return mapa
  }, [limites])

  // Defensivo: o backend ja devolve so as categorias de nivel 0 no topo do
  // array (subcategorias vem aninhadas em `subcategorias`, ver comentario de
  // useCategorias.ts), mas filtrar aqui deixa explicito que esta tela nunca
  // renderiza uma subcategoria solta na raiz da lista.
  const categoriasTopLevel = (categorias ?? []).filter((categoria) => !categoria.parentId)

  // Enquanto o FormCategoria estiver aberto em modo edicao (acima da lista),
  // o CategoriaItem correspondente aa mesma categoria precisa saber disso
  // para desabilitar seu proprio botao "Editar" - sem isso existiam 2 pontos
  // de entrada simultaneos para editar a mesma categoria (o form aberto e o
  // botao "Editar" da lista, ainda clicavel, abaixo dele).
  const categoriaEmEdicaoId = formulario?.modo === "editar" ? formulario.categoria.id : null

  function handleTrocarTipo(novoTipo: TipoCategoria) {
    setTipoSelecionado(novoTipo)
    setFormulario(null)
  }

  function handleAlternarCriacao() {
    setFormulario((atual) => (atual?.modo === "criar" ? null : { modo: "criar" }))
  }

  function handleEditar(categoria: CategoriaResponse) {
    setFormulario({ modo: "editar", categoria })
  }

  function handleFecharFormulario() {
    setFormulario(null)
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Categorias</h1>
          <p className="text-sm text-text-muted">
            Organize despesas e receitas em categorias e subcategorias.
          </p>
        </div>
        <Button type="button" onClick={handleAlternarCriacao}>
          {formulario?.modo === "criar" ? "Cancelar" : "Nova categoria"}
        </Button>
      </header>

      <div className="flex gap-2" role="group" aria-label="Tipo de categoria">
        <Button
          type="button"
          variant={tipoSelecionado === "Despesa" ? "default" : "outline"}
          onClick={() => handleTrocarTipo("Despesa")}
          className="flex-1"
        >
          Despesa
        </Button>
        <Button
          type="button"
          variant={tipoSelecionado === "Receita" ? "default" : "outline"}
          onClick={() => handleTrocarTipo("Receita")}
          className="flex-1"
        >
          Receita
        </Button>
      </div>

      <Modal open={formulario?.modo === "criar"} onClose={handleFecharFormulario} title="Nova categoria">
        <FormCategoria onSalvar={handleFecharFormulario} />
      </Modal>

      {formulario?.modo === "editar" && (
        <Modal
          open
          onClose={handleFecharFormulario}
          title={`Editar categoria "${formulario.categoria.nome}"`}
        >
          <FormCategoria
            categoriaParaEditar={formulario.categoria}
            onSalvar={handleFecharFormulario}
          />
        </Modal>
      )}

      {erroCategorias ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as categorias</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : carregandoCategorias ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : categoriasTopLevel.length > 0 ? (
        <div className="flex flex-col gap-3">
          {categoriasTopLevel.map((categoria) => (
            <CategoriaItem
              key={categoria.id}
              categoria={categoria}
              limitesPorCategoriaId={limitesPorCategoriaId}
              onEditar={handleEditar}
              categoriaEmEdicaoId={categoriaEmEdicaoId}
            />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma categoria cadastrada ainda.</p>
      )}
    </div>
  )
}
