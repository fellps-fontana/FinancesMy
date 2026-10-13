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
import { CampoLimiteGasto } from "@/features/categorias/components/CampoLimiteGasto"
import { useDefinirLimiteGasto } from "@/features/limite-gasto/hooks/useDefinirLimiteGasto"
import { useRemoverLimiteGasto } from "@/features/limite-gasto/hooks/useRemoverLimiteGasto"
import {
  filtrarOpcoesDeCategoriaPai,
  validarCategoria,
} from "@/features/categorias/lib/validarCategoria"
import { CATALOGO_ICONES } from "@/shared/lib/catalogoIcones"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"
import type { LimiteGastoResponse } from "@/features/limite-gasto/types"

type FormCategoriaProps = {
  // Presenca de `categoriaParaEditar` define o modo do formulario: ausente ->
  // CRIAR (useCriarCategoria, tipo escolhivel); presente -> EDITAR
  // (useEditarCategoria). EditarCategoriaRequest nao aceita `tipo`
  // (types.ts, regra-de-negocio.md item 7: tipo e imutavel apos a criacao) -
  // o campo continua exibido em modo edicao so para dar contexto, mas
  // desabilitado e nunca reenviado no payload.
  categoriaParaEditar?: CategoriaResponse
  // Limite ja gravado da categoria em edicao (regra-de-negocio.md item 14),
  // vindo do estado de servidor da pagina; null/ausente = sem limite.
  limiteAtual?: LimiteGastoResponse | null
  onSalvar?: () => void
  onCancelar?: () => void
}

const MENSAGEM_LIMITE_INVALIDO = "Informe um limite maior que zero ou deixe em branco."

// Converte o texto digitado ("1.850,50" nao e aceito; "1850,50" e "1850.50"
// sim) em numero. Vazio = sem limite (null); invalido ou <= 0 = undefined.
function interpretarLimite(texto: string): number | null | undefined {
  const limpo = texto.trim()
  if (limpo === "") return null
  const numero = Number(limpo.replace(",", "."))
  return Number.isFinite(numero) && numero > 0 ? numero : undefined
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
  limiteAtual,
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
  const [limiteTexto, setLimiteTexto] = useState(
    limiteAtual ? String(limiteAtual.valorLimite).replace(".", ",") : "",
  )
  const [temLimite, setTemLimite] = useState(limiteAtual != null)
  // Id da categoria ja gravada nesta sessao do form. Se o limite falhar depois
  // de a categoria salvar (falha parcial), o retry reaproveita o id em vez de
  // criar uma segunda categoria.
  const [idSalvo, setIdSalvo] = useState<string | null>(null)
  const [salvando, setSalvando] = useState(false)
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutateAsync: criarCategoria } = useCriarCategoria()
  const { mutateAsync: editarCategoria } = useEditarCategoria()
  const { mutateAsync: definirLimite } = useDefinirLimiteGasto()
  const { mutateAsync: removerLimite, isPending: removendoLimite } = useRemoverLimiteGasto()

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
    () => filtrarOpcoesDeCategoriaPai(categoriasDoTipo, idSalvo ?? categoriaParaEditar?.id),
    [categoriasDoTipo, categoriaParaEditar, idSalvo],
  )

  const ocupado = salvando || removendoLimite
  // Limite so existe para DESPESA (regra-de-negocio.md item 14).
  const aceitaLimite = tipo === "Despesa"
  const tipoTravado = modoEdicao || idSalvo !== null

  function handleTrocarTipo(novoTipo: TipoCategoria) {
    // Tipo e imutavel apos a criacao (item 7); tambem trava depois da
    // categoria ja ter sido gravada numa falha parcial.
    if (tipoTravado) return
    setTipo(novoTipo)
    // Categoria-pai valida depende do tipo.
    setParentId("")
    setErroFormulario(null)
  }

  async function handleRemoverLimite() {
    const id = idSalvo ?? categoriaParaEditar?.id
    if (!id) return
    setErroFormulario(null)
    try {
      await removerLimite(id)
      setTemLimite(false)
      setLimiteTexto("")
    } catch (error) {
      console.error("Falha ao remover limite de gasto", error)
      setErroFormulario(mensagemDeErro(error, "Nao foi possivel remover o limite. Tente novamente."))
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarCategoria(nome, parentId, opcoesDeCategoriaPai)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    const limite = aceitaLimite ? interpretarLimite(limiteTexto) : null
    if (limite === undefined) {
      setErroFormulario(MENSAGEM_LIMITE_INVALIDO)
      return
    }

    setErroFormulario(null)
    setSalvando(true)

    // Passo 1: categoria (criar, ou editar - inclusive o retry apos falha
    // parcial). O request de edicao nunca leva `tipo` (item 7).
    let id = idSalvo ?? categoriaParaEditar?.id ?? null
    try {
      if (id) {
        await editarCategoria({
          id,
          request: { nome: nome.trim(), parentId: parentId || undefined, icone },
        })
      } else {
        const criada = await criarCategoria({
          nome: nome.trim(),
          tipo,
          parentId: parentId || undefined,
          icone,
        })
        id = criada.id
        setIdSalvo(criada.id)
      }
    } catch (error) {
      console.error("Falha ao salvar categoria", error)
      setErroFormulario(mensagemDeErro(error, "Nao foi possivel salvar a categoria. Tente novamente."))
      setSalvando(false)
      return
    }

    // Passo 2: limite (so despesa). Campo vazio com limite gravado = remover.
    try {
      if (limite !== null) {
        await definirLimite({ categoriaId: id, valorLimite: limite })
      } else if (temLimite) {
        await removerLimite(id)
      }
    } catch (error) {
      console.error("Falha ao gravar limite de gasto", error)
      setErroFormulario(
        `A categoria foi salva, mas o limite nao: ${mensagemDeErro(error, "tente novamente")}. Salve de novo para repetir so o limite.`,
      )
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
              tipo === valor ? "bg-accent-deep text-accent-soft" : "bg-muted text-muted-foreground",
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
          disabled={carregandoCategoriasPai || ocupado}
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

      {aceitaLimite && (
        <CampoLimiteGasto
          valor={limiteTexto}
          onChange={setLimiteTexto}
          temLimite={temLimite && tipoTravado}
          onRemover={handleRemoverLimite}
          removendo={removendoLimite}
          disabled={salvando}
        />
      )}

      <div className="flex justify-end gap-2.5">
        <Button type="button" variant="outline" size="lg" onClick={onCancelar} disabled={ocupado}>
          Cancelar
        </Button>
        <Button type="submit" size="lg" disabled={ocupado}>
          {salvando ? "Salvando..." : "Salvar categoria"}
        </Button>
      </div>
    </form>
  )
}
