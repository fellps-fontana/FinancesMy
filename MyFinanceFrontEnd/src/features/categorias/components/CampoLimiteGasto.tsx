import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useDefinirLimiteGasto } from "@/features/limite-gasto/hooks/useDefinirLimiteGasto"
import { useRemoverLimiteGasto } from "@/features/limite-gasto/hooks/useRemoverLimiteGasto"

type LimiteGastoAtual = {
  valorLimite: number
}

type CampoLimiteGastoProps = {
  categoriaId: string
  categoriaTipo: "DESPESA" | "RECEITA"
  limiteAtual?: LimiteGastoAtual | null
}

// Cadastro/edicao de valor_limite embutido na tela de categoria
// (regra-de-negocio.md item 14, "Onde aparece": "cadastro/edicao do
// valor_limite fica embutido no form/lista de categoria, nao ha tela
// separada de Limites"). Limite so faz sentido para categoria DESPESA -
// orcamento e conceito de gasto, categoria RECEITA nunca tem `limite_gasto`
// (item 14, primeiro paragrafo: "categoria tipo RECEITA nao pode ter
// limite").
//
// Quem monta `limiteAtual` e o componente pai (lista/form de categoria) -
// este componente nao busca dado sozinho (sem fetch proprio), so recebe via
// prop e apresenta, pra continuar reutilizavel dentro de uma lista/form
// maior sem acoplar em de onde vem o dado (mesmo espirito de
// FormRegistrarRecebimento, features/contas-receber).
export function CampoLimiteGasto({ categoriaId, categoriaTipo, limiteAtual }: CampoLimiteGastoProps) {
  const [editando, setEditando] = useState(false)
  const [valor, setValor] = useState("")
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: definirLimite, isPending: salvando } = useDefinirLimiteGasto()
  const { mutate: removerLimite, isPending: removendo } = useRemoverLimiteGasto()

  // Categoria RECEITA nao pode ter limite (item 14) - o campo nem existe
  // nessa superficie, antes de qualquer outro hook de leitura de estado.
  if (categoriaTipo === "RECEITA") {
    return null
  }

  function abrirEdicao() {
    setValor(limiteAtual ? String(limiteAtual.valorLimite) : "")
    setErro(null)
    setEditando(true)
  }

  function cancelarEdicao() {
    setErro(null)
    setEditando(false)
  }

  function handleSalvar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const valorLimite = Number(valor.replace(",", "."))
    if (!valor || Number.isNaN(valorLimite) || valorLimite <= 0) {
      setErro("Informe um valor de limite maior que zero.")
      return
    }

    definirLimite(
      { categoriaId, valorLimite },
      {
        onSuccess: () => setEditando(false),
        onError: (error) => {
          console.error("Falha ao definir limite de gasto", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel salvar o limite. Tente novamente.",
          )
        },
      },
    )
  }

  function handleRemover() {
    setErro(null)
    removerLimite(categoriaId, {
      onError: (error) => {
        console.error("Falha ao remover limite de gasto", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel remover o limite. Tente novamente.",
        )
      },
    })
  }

  if (editando) {
    return (
      <form onSubmit={handleSalvar} className="flex flex-col gap-2">
        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`limiteGasto-${categoriaId}`}>Limite de gasto mensal</Label>
          <Input
            id={`limiteGasto-${categoriaId}`}
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            autoFocus
            required
            value={valor}
            onChange={(event) => setValor(event.target.value)}
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" size="sm" onClick={cancelarEdicao} disabled={salvando}>
            Cancelar
          </Button>
          <Button type="submit" size="sm" disabled={salvando}>
            {salvando ? "Salvando..." : "Salvar limite"}
          </Button>
        </div>
      </form>
    )
  }

  if (!limiteAtual) {
    return (
      <Button type="button" variant="link" size="sm" className="h-auto p-0" onClick={abrirEdicao}>
        Definir limite de gasto mensal
      </Button>
    )
  }

  return (
    <div className="flex flex-col gap-1.5">
      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <div className="flex items-center justify-between gap-2">
        <div className="flex flex-col">
          <span className="text-[12px] text-text-muted">Limite de gasto mensal</span>
          <span className="text-[14px] font-medium text-text-primary">
            {formatarMoeda(limiteAtual.valorLimite)}
          </span>
        </div>

        <div className="flex gap-2">
          <Button type="button" variant="ghost" size="sm" onClick={abrirEdicao} disabled={removendo}>
            Editar
          </Button>
          <Button type="button" variant="destructive" size="sm" onClick={handleRemover} disabled={removendo}>
            {removendo ? "Removendo..." : "Remover"}
          </Button>
        </div>
      </div>
    </div>
  )
}
