import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { CategoriaSelect } from "@/features/categorias/components/CategoriaSelect"
import { useCriarAssinaturaCartao } from "@/features/assinaturas-cartao/hooks/useCriarAssinaturaCartao"
import { useEditarAssinaturaCartao } from "@/features/assinaturas-cartao/hooks/useEditarAssinaturaCartao"
import {
  converterDiaReferenciaParaNumero,
  converterValorParaNumero,
  validarAssinaturaCartao,
} from "@/features/assinaturas-cartao/lib/validarAssinaturaCartao"
import type { AssinaturaCartaoResponse } from "@/features/assinaturas-cartao/types"

type FormAssinaturaCartaoProps = {
  // A conta de origem (sempre tipo CARTAO) ja vem selecionada da tela pai
  // (ContaCartaoPage) - diferente de FormContaFixa, aqui nao ha select de
  // conta porque o formulario so aparece dentro do contexto de um cartao ja
  // escolhido.
  contaId: string
  // Presenca de `assinaturaParaEditar` define o modo do formulario: ausente
  // -> CRIAR (useCriarAssinaturaCartao); presente -> EDITAR
  // (useEditarAssinaturaCartao). Diferente de FormContaFixa, aqui descricao
  // e editavel em ambos os modos (EditarAssinaturaCartaoRequest exige
  // Descricao - DTOs/AssinaturaCartao/EditarAssinaturaCartaoRequest.cs).
  assinaturaParaEditar?: AssinaturaCartaoResponse
  onSalvar?: () => void
}

export function FormAssinaturaCartao({
  contaId,
  assinaturaParaEditar,
  onSalvar,
}: FormAssinaturaCartaoProps) {
  const modoEdicao = assinaturaParaEditar !== undefined

  const [descricao, setDescricao] = useState(assinaturaParaEditar?.descricao ?? "")
  const [valor, setValor] = useState(
    assinaturaParaEditar ? String(assinaturaParaEditar.valor) : "",
  )
  const [diaReferencia, setDiaReferencia] = useState(
    assinaturaParaEditar ? String(assinaturaParaEditar.diaReferencia) : "",
  )
  const [categoriaId, setCategoriaId] = useState<string | undefined>(
    assinaturaParaEditar?.categoriaId ?? undefined,
  )
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarAssinatura, isPending: criando } = useCriarAssinaturaCartao()
  const { mutate: editarAssinatura, isPending: editando } = useEditarAssinaturaCartao()

  const isSubmitting = criando || editando

  function restaurarValoresIniciais() {
    setDescricao(assinaturaParaEditar?.descricao ?? "")
    setValor(assinaturaParaEditar ? String(assinaturaParaEditar.valor) : "")
    setDiaReferencia(assinaturaParaEditar ? String(assinaturaParaEditar.diaReferencia) : "")
    setCategoriaId(assinaturaParaEditar?.categoriaId ?? undefined)
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarAssinaturaCartao(descricao, valor, diaReferencia)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    if (modoEdicao && assinaturaParaEditar) {
      editarAssinatura(
        {
          id: assinaturaParaEditar.id,
          request: {
            descricao: descricao.trim(),
            valor: converterValorParaNumero(valor),
            diaReferencia: converterDiaReferenciaParaNumero(diaReferencia),
            categoriaId,
          },
        },
        {
          onSuccess: () => {
            setErroFormulario(null)
            onSalvar?.()
          },
          onError: (error) => {
            console.error("Falha ao editar assinatura de cartao", error)
            setErroFormulario(
              error instanceof ApiError
                ? error.message
                : "Nao foi possivel salvar a assinatura. Tente novamente.",
            )
          },
        },
      )
      return
    }

    criarAssinatura(
      {
        contaId,
        descricao: descricao.trim(),
        valor: converterValorParaNumero(valor),
        diaReferencia: converterDiaReferenciaParaNumero(diaReferencia),
        categoriaId,
      },
      {
        onSuccess: () => {
          restaurarValoresIniciais()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar assinatura de cartao", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel criar a assinatura. Tente novamente.",
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

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoAssinaturaCartao">Descricao</Label>
        <Input
          id="descricaoAssinaturaCartao"
          name="descricao"
          placeholder="Ex: Netflix"
          autoFocus={!modoEdicao}
          required
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorAssinaturaCartao">Valor</Label>
          <Input
            id="valorAssinaturaCartao"
            name="valor"
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            required
            value={valor}
            onChange={(event) => setValor(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="diaReferenciaAssinaturaCartao">Dia de referencia</Label>
          <Input
            id="diaReferenciaAssinaturaCartao"
            name="diaReferencia"
            type="number"
            step="1"
            min="1"
            max="31"
            inputMode="numeric"
            required
            value={diaReferencia}
            onChange={(event) => setDiaReferencia(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="categoriaAssinaturaCartao">Categoria</Label>
        <CategoriaSelect tipo="Despesa" value={categoriaId} onChange={setCategoriaId} />
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
