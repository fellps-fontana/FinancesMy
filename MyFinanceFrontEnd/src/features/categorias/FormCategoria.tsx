import { useMemo, useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { useCriarCategoria } from "@/features/categorias/hooks/useCriarCategoria"
import { useEditarCategoria } from "@/features/categorias/hooks/useEditarCategoria"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import {
  filtrarOpcoesDeCategoriaPai,
  validarCategoria,
} from "@/features/categorias/lib/validarCategoria"
import { CATALOGO_ICONES } from "@/shared/lib/catalogoIcones"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"

type FormCategoriaProps = {
  // Presenca de `categoriaParaEditar` define o modo do formulario: ausente ->
  // CRIAR (useCriarCategoria, tipo escolhivel); presente -> EDITAR
  // (useEditarCategoria). EditarCategoriaRequest nao aceita `tipo`
  // (types.ts, regra-de-negocio.md item 7: tipo e imutavel apos a criacao) -
  // o campo continua exibido em modo edicao so para dar contexto, mas
  // desabilitado e nunca reenviado no payload.
  categoriaParaEditar?: CategoriaResponse
  onSalvar?: () => void
}

const TIPO_PADRAO: TipoCategoria = "Despesa"

export function FormCategoria({ categoriaParaEditar, onSalvar }: FormCategoriaProps) {
  const modoEdicao = categoriaParaEditar !== undefined

  const [nome, setNome] = useState(categoriaParaEditar?.nome ?? "")
  const [tipo, setTipo] = useState<TipoCategoria>(categoriaParaEditar?.tipo ?? TIPO_PADRAO)
  const [parentId, setParentId] = useState(categoriaParaEditar?.parentId ?? "")
  // Id de um icone do catalogo fixo (shared/lib/catalogoIcones.ts, mesmo
  // catalogo pensado para reuso em Conta - TASK-129). Opcional: categoria
  // sem icone escolhido fica `undefined`, sem quebra (backend ja trata como
  // nullable, ver types.ts).
  const [icone, setIcone] = useState<string | undefined>(categoriaParaEditar?.icone)
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarCategoria, isPending: criando } = useCriarCategoria()
  const { mutate: editarCategoria, isPending: editando } = useEditarCategoria()

  // Lista de categorias ativas do tipo selecionado, para oferecer como
  // categoria-pai. Em modo edicao `tipo` fica travado no tipo original (nao
  // ha como o usuario trocar), entao a lista sempre reflete o mesmo tipo da
  // categoria em edicao.
  const {
    data: categoriasDoTipo,
    isLoading: carregandoCategoriasPai,
    error: erroCategoriasPai,
  } = useCategorias(tipo, false)

  if (erroCategoriasPai) {
    console.error("Falha ao carregar categorias para vinculo de categoria-pai", erroCategoriasPai)
  }

  const opcoesDeCategoriaPai = useMemo(
    () => filtrarOpcoesDeCategoriaPai(categoriasDoTipo, categoriaParaEditar?.id),
    [categoriasDoTipo, categoriaParaEditar],
  )

  const isSubmitting = criando || editando

  function restaurarValoresIniciais() {
    setNome(categoriaParaEditar?.nome ?? "")
    setTipo(categoriaParaEditar?.tipo ?? TIPO_PADRAO)
    setParentId(categoriaParaEditar?.parentId ?? "")
    setIcone(categoriaParaEditar?.icone)
    setErroFormulario(null)
  }

  function handleTrocarTipo(novoTipo: TipoCategoria) {
    if (modoEdicao) {
      return
    }
    setTipo(novoTipo)
    // Categoria-pai valida depende do tipo - trocar o tipo invalida qualquer
    // selecao anterior de categoria-pai de outro tipo.
    setParentId("")
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarCategoria(nome, parentId, opcoesDeCategoriaPai)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    if (modoEdicao && categoriaParaEditar) {
      editarCategoria(
        {
          id: categoriaParaEditar.id,
          request: {
            nome: nome.trim(),
            parentId: parentId || undefined,
            icone,
          },
        },
        {
          onSuccess: () => {
            setErroFormulario(null)
            onSalvar?.()
          },
          onError: (error) => {
            console.error("Falha ao editar categoria", error)
            setErroFormulario(
              error instanceof ApiError
                ? error.message
                : "Nao foi possivel salvar a categoria. Tente novamente.",
            )
          },
        },
      )
      return
    }

    criarCategoria(
      {
        nome: nome.trim(),
        tipo,
        parentId: parentId || undefined,
        icone,
      },
      {
        onSuccess: () => {
          restaurarValoresIniciais()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar categoria", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel criar a categoria. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-4"
    >
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      {/* Segmented control: mesmo padrao ja usado em
          FormRegistrarContaReceber.tsx (dois Button com variant condicional,
          nao ha Tabs/Toggle pronto no projeto). Em modo edicao o tipo fica
          desabilitado - imutavel apos a criacao (regra-de-negocio.md item 7,
          EditarCategoriaRequest sem campo tipo). */}
      <div className="flex flex-col gap-1.5">
        <Label>Tipo</Label>
        <div className="flex gap-2" role="group" aria-label="Tipo da categoria">
          <Button
            type="button"
            variant={tipo === "Despesa" ? "default" : "outline"}
            onClick={() => handleTrocarTipo("Despesa")}
            disabled={modoEdicao || isSubmitting}
            className="flex-1"
          >
            Despesa
          </Button>
          <Button
            type="button"
            variant={tipo === "Receita" ? "default" : "outline"}
            onClick={() => handleTrocarTipo("Receita")}
            disabled={modoEdicao || isSubmitting}
            className="flex-1"
          >
            Receita
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="nomeCategoria">Nome</Label>
        <Input
          id="nomeCategoria"
          name="nome"
          placeholder="Ex: Alimentacao"
          autoFocus
          required
          value={nome}
          onChange={(event) => setNome(event.target.value)}
        />
      </div>

      {/* Seletor de icone (mockup "06 Categorias.dc.html", secao "Modal nova
          categoria"): grade de quadrados 40px, catalogo FIXO (shared/lib/
          catalogoIcones.ts) - sem upload/URL livre, mesmo espirito da
          paleta fixa de cor (identidade-visual.md). Opcional: clicar no
          icone ja selecionado desmarca (categoria pode ficar sem icone). */}
      <div className="flex flex-col gap-1.5">
        <Label>Icone (opcional)</Label>
        <div className="flex flex-wrap gap-2" role="group" aria-label="Icone da categoria">
          {CATALOGO_ICONES.map(({ id, label, Icon }) => {
            const selecionado = icone === id
            return (
              <button
                key={id}
                type="button"
                title={label}
                aria-label={label}
                aria-pressed={selecionado}
                disabled={isSubmitting}
                onClick={() => setIcone(selecionado ? undefined : id)}
                className={cn(
                  "flex size-10 shrink-0 items-center justify-center rounded-lg transition-colors disabled:pointer-events-none disabled:opacity-50",
                  selecionado ? "bg-primary" : "bg-muted hover:bg-muted/70",
                )}
              >
                <Icon
                  className={cn("size-[18px]", selecionado ? "text-background" : "text-muted-foreground")}
                  strokeWidth={1.8}
                  aria-hidden="true"
                />
              </button>
            )
          })}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="parentIdCategoria">Categoria-pai (opcional)</Label>
        <select
          id="parentIdCategoria"
          name="parentId"
          disabled={carregandoCategoriasPai || isSubmitting}
          value={parentId}
          onChange={(event) => setParentId(event.target.value)}
          className={cn(
            "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
          )}
        >
          <option value="">
            {carregandoCategoriasPai ? "Carregando categorias..." : "Nenhuma (categoria principal)"}
          </option>
          {opcoesDeCategoriaPai.map((categoria) => (
            <option key={categoria.id} value={categoria.id}>
              {categoria.nome}
            </option>
          ))}
        </select>
        {erroCategoriasPai && (
          <span className="text-[12px] text-alerta">
            Nao foi possivel carregar as categorias-pai. Tente novamente.
          </span>
        )}
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={restaurarValoresIniciais} disabled={isSubmitting}>
          {modoEdicao ? "Desfazer" : "Limpar"}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
