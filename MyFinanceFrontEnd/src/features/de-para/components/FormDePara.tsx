import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { CategoriaSelect } from "@/features/categorias/components/CategoriaSelect"
import { useCriarDePara } from "@/features/de-para/hooks/useCriarDePara"
import { useEditarDePara } from "@/features/de-para/hooks/useEditarDePara"
import type { TipoCategoria } from "@/features/categorias/types"
import type { DeParaCategoriaResponse } from "@/features/de-para/types"

type FormDeParaProps = {
  // Presente -> EDITAR (so a categoria do app muda; a string importada e a
  // chave do vinculo). Ausente -> CRIAR.
  vinculoParaEditar?: DeParaCategoriaResponse
  onSalvar: () => void
  onCancelar: () => void
}

const TIPOS: TipoCategoria[] = ["Despesa", "Receita"]

function mensagemDeErro(erro: unknown): string {
  return erro instanceof ApiError ? erro.message : "Nao foi possivel salvar o vinculo. Tente novamente."
}

export function FormDePara({ vinculoParaEditar, onSalvar, onCancelar }: FormDeParaProps) {
  const modoEdicao = vinculoParaEditar !== undefined

  const [categoriaPierre, setCategoriaPierre] = useState(vinculoParaEditar?.categoriaPierre ?? "")
  const [tipo, setTipo] = useState<TipoCategoria>(vinculoParaEditar?.categoria?.tipo ?? "Despesa")
  const [categoriaId, setCategoriaId] = useState<string | undefined>(vinculoParaEditar?.categoriaId)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: criar, isPending: criando } = useCriarDePara()
  const { mutate: editar, isPending: editando } = useEditarDePara()
  const salvando = criando || editando

  function handleTrocarTipo(novoTipo: TipoCategoria) {
    setTipo(novoTipo)
    setCategoriaId(undefined)
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErro(null)

    const textoPierre = categoriaPierre.trim()
    if (!textoPierre) {
      setErro("Informe a categoria importada.")
      return
    }
    if (!categoriaId) {
      setErro("Selecione a categoria do app.")
      return
    }

    const opcoes = { onSuccess: onSalvar, onError: (e: unknown) => setErro(mensagemDeErro(e)) }

    if (vinculoParaEditar) {
      editar({ id: vinculoParaEditar.id, request: { categoriaId } }, opcoes)
    } else {
      criar({ categoriaPierre: textoPierre, categoriaId }, opcoes)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="de-para-pierre">Categoria importada</Label>
        <Input
          id="de-para-pierre"
          value={categoriaPierre}
          onChange={(e) => setCategoriaPierre(e.target.value)}
          disabled={modoEdicao || salvando}
          className={CLASSE_CAMPO}
          placeholder="Ex: SUPERMERC PAO ACUCAR"
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label>Tipo</Label>
        <div className="flex gap-2" role="group" aria-label="Tipo de categoria">
          {TIPOS.map((t) => (
            <Button
              key={t}
              type="button"
              variant={tipo === t ? "default" : "outline"}
              className="h-auto flex-1 rounded-[10px] px-[18px] py-[11px] text-sm"
              disabled={salvando}
              onClick={() => handleTrocarTipo(t)}
            >
              {t}
            </Button>
          ))}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label>Categoria do app</Label>
        <CategoriaSelect tipo={tipo} value={categoriaId} onChange={setCategoriaId} />
      </div>

      {erro ? (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      ) : null}

      <div className="flex justify-end gap-2.5">
        <Button type="button" variant="outline" className={CLASSE_BOTAO_CANCELAR} disabled={salvando} onClick={onCancelar}>
          Cancelar
        </Button>
        <Button type="submit" className={CLASSE_BOTAO_PRIMARIO} disabled={salvando}>
          {modoEdicao ? "Salvar" : "Aplicar e lembrar"}
        </Button>
      </div>
    </form>
  )
}
