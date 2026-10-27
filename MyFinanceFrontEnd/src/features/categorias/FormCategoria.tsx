import { useMemo, useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, CLASSE_CAMPO } from "@/shared/ui/FormModal"
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
  onCancelar?: () => void
}

function mensagemDeErro(error: unknown, padrao: string): string {
  return error instanceof ApiError ? error.message : padrao
}

const TIPO_PADRAO: TipoCategoria = "Despesa"

const OPCOES_TIPO: ReadonlyArray<{ valor: TipoCategoria; rotulo: string }> = [
  { valor: "Despesa", rotulo: "Despesa" },
  { valor: "Receita", rotulo: "Receita" },
]

export function FormCategoria({
  categoriaParaEditar,
  onSalvar,
  onCancelar,
}: FormCategoriaProps) {
  const modoEdicao = categoriaParaEditar !== undefined

  const [nome, setNome] = useState(categoriaParaEditar?.nome ?? "")
  const [tipo, setTipo] = useState<TipoCategoria>(categoriaParaEditar?.tipo ?? TIPO_PADRAO)
  const [parentId, setParentId] = useState(categoriaParaEditar?.parentId ?? "")
  // Id de um icone do catalogo fixo (shared/lib/catalogoIcones.ts, mesmo
  // catalogo pensado para reuso em Conta - TASK-129). Opcional: categoria
  // sem icone escolhido fica `undefined`, sem quebra (backend ja trata como
  // nullable, ver types.ts).
  const [icone, setIcone] = useState<string | undefined>(categoriaParaEditar?.icone)
  const [salvando, setSalvando] = useState(false)
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutateAsync: criarCategoria } = useCriarCategoria()
  const { mutateAsync: editarCategoria } = useEditarCategoria()

  // Lista de categorias ativas do tipo selecionado, para oferecer como
  // categoria-pai. Em modo edicao `tipo` fica travado no tipo original.
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

  const ocupado = salvando
  const tipoTravado = modoEdicao

  function handleTrocarTipo(novoTipo: TipoCategoria) {
    // Tipo e imutavel apos a criacao (item 7).
    if (tipoTravado) return
    setTipo(novoTipo)
    // Categoria-pai valida depende do tipo.
    setParentId("")
    setErroFormulario(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarCategoria(nome, parentId, opcoesDeCategoriaPai)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    setErroFormulario(null)
    setSalvando(true)

    // Criar ou editar. O request de edicao nunca leva `tipo` (item 7).
    try {
      if (categoriaParaEditar) {
        await editarCategoria({
          id: categoriaParaEditar.id,
          request: { nome: nome.trim(), parentId: parentId || undefined, icone },
        })
      } else {
        await criarCategoria({
          nome: nome.trim(),
          tipo,
          parentId: parentId || undefined,
          icone,
        })
      }
    } catch (error) {
      console.error("Falha ao salvar categoria", error)
      setErroFormulario(mensagemDeErro(error, "Nao foi possivel salvar a categoria. Tente novamente."))
      setSalvando(false)
      return
    }

    setSalvando(false)
    onSalvar?.()
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      {/* Tipo travado na edicao (regra-de-negocio.md item 7). */}
      <div className="flex gap-2" role="group" aria-label="Tipo da categoria">
        {OPCOES_TIPO.map(({ valor, rotulo }) => (
          <button
            key={valor}
            type="button"
            aria-pressed={tipo === valor}
            onClick={() => handleTrocarTipo(valor)}
            disabled={tipoTravado || ocupado}
            className={cn(
              "flex-1 rounded-[9px] px-3 py-2.5 text-[13px] transition-colors outline-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none",
              tipo === valor ? "bg-accent-deep text-accent-soft" : "bg-accent text-muted-foreground",
              tipoTravado && tipo !== valor && "opacity-50",
            )}
          >
            {rotulo}
          </button>
        ))}
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
          disabled={ocupado}
          onChange={(event) => setNome(event.target.value)}
          className={CLASSE_CAMPO}
        />
      </div>

      {/* Catalogo FIXO (shared/lib/catalogoIcones.ts), sem upload/URL livre.
          Opcional: clicar no icone selecionado desmarca. */}
      <div className="flex flex-col gap-1.5">
        <Label>Icone (opcional)</Label>
        <div className="flex flex-wrap gap-2.5" role="group" aria-label="Icone da categoria">
          {CATALOGO_ICONES.map(({ id, label, Icon }) => {
            const selecionado = icone === id
            return (
              <button
                key={id}
                type="button"
                title={label}
                aria-label={label}
                aria-pressed={selecionado}
                disabled={ocupado}
                onClick={() => setIcone(selecionado ? undefined : id)}
                className={cn(
                  "flex size-10 shrink-0 items-center justify-center rounded-[10px] transition-colors disabled:pointer-events-none disabled:opacity-50",
                  selecionado ? "bg-primary" : "bg-accent hover:bg-accent/70",
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
          disabled={carregandoCategoriasPai || ocupado}
          value={parentId}
          onChange={(event) => setParentId(event.target.value)}
          className={cn(
            "w-full min-w-0 rounded-[10px] border border-border bg-accent px-3.5 py-3 text-sm text-text-body transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
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

      <div className="mt-1.5 flex justify-end gap-2.5">
        <Button
          type="button"
          variant="outline"
          onClick={onCancelar}
          disabled={ocupado}
          className={CLASSE_BOTAO_CANCELAR}
        >
          Cancelar
        </Button>
        <Button type="submit" disabled={ocupado} className={CLASSE_BOTAO_PRIMARIO}>
          {salvando ? "Salvando..." : "Salvar categoria"}
        </Button>
      </div>
    </form>
  )
}
