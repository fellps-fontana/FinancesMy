import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { dataDeHoje } from "@/features/cartao/lib/formatarData"
// Nao ha hook de "listar contas" combinado dentro de features/lancamentos
// (mesma situacao ja registrada em FormContaFixa.tsx). useContasParaSelecao
// ja resolve exatamente "contas MANUAL disponiveis pra origem/destino,
// excluindo cartao" (busca banco + investimento em paralelo) - reaproveitado
// aqui em vez de duplicar a chamada combinada. Import cross-feature,
// registrado no mesmo espirito da nota ja deixada em FormContaFixa.tsx.
import { useContasParaSelecao } from "@/features/contas-receber/hooks/useContasParaSelecao"
import { useCriarTransferencia } from "@/features/lancamentos/hooks/useCriarTransferencia"

function validarValor(valor: string): string | null {
  const valorNormalizado = valor.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o valor."
  }

  const valorNumerico = Number(valorNormalizado)

  if (Number.isNaN(valorNumerico) || valorNumerico <= 0) {
    return "Informe um valor valido, maior que zero."
  }

  return null
}

function converterValorParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}

// Validacao pura do formulario de transferencia (regra-de-negocio.md itens 3
// e 12: movimentacao entre contas do proprio usuario, sem categoria/tipo -
// o backend e quem resolve a natureza da transferencia). Origem e destino
// diferentes e checagem de UX (evitar transferencia sem sentido pra mesma
// conta), nao uma regra de negocio formal documentada.
function validarFormulario(
  contaOrigemId: string,
  contaDestinoId: string,
  valor: string,
  data: string,
): string | null {
  if (contaOrigemId.length === 0) {
    return "Selecione a conta de origem."
  }

  if (contaDestinoId.length === 0) {
    return "Selecione a conta de destino."
  }

  if (contaOrigemId === contaDestinoId) {
    return "A conta de origem e a conta de destino devem ser diferentes."
  }

  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  if (data.trim().length === 0) {
    return "Informe a data."
  }

  return null
}

type FormTransferenciaProps = {
  onSalvar?: () => void
}

// Transferencia entre contas de mesma titularidade (regra-de-negocio.md item
// 3): SEM campo tipo/status visivel ao usuario - a duas-pernas (saida na
// origem, entrada no destino, ambas fora do calculo de gasto/receita) e
// resolvida inteiramente pelo backend a partir de contaOrigemId/
// contaDestinoId. O front so coleta os dados da movimentacao.
export function FormTransferencia({ onSalvar }: FormTransferenciaProps) {
  const [contaOrigemId, setContaOrigemId] = useState("")
  const [contaDestinoId, setContaDestinoId] = useState("")
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje())
  const [descricao, setDescricao] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarTransferencia, isPending: isSubmitting } = useCriarTransferencia()

  // Uma unica chamada, reaproveitada nos dois selects (origem e destino) -
  // ambos escolhem dentro do mesmo universo de contas MANUAL elegiveis.
  const {
    data: contas,
    isLoading: carregandoContas,
    error: erroContas,
  } = useContasParaSelecao()

  if (erroContas) {
    console.error("Falha ao carregar contas para transferencia", erroContas)
  }

  function limparFormulario() {
    setContaOrigemId("")
    setContaDestinoId("")
    setValor("")
    setData(dataDeHoje())
    setDescricao("")
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarFormulario(contaOrigemId, contaDestinoId, valor, data)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarTransferencia(
      {
        contaOrigemId,
        contaDestinoId,
        valor: converterValorParaNumero(valor),
        data,
        descricao: descricao.trim() || undefined,
      },
      {
        onSuccess: () => {
          limparFormulario()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar transferencia", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar a transferencia. Tente novamente.",
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
        <Label htmlFor="contaOrigemTransferencia">Conta de origem</Label>
        <select
          id="contaOrigemTransferencia"
          name="contaOrigemId"
          required
          disabled={carregandoContas}
          value={contaOrigemId}
          onChange={(event) => setContaOrigemId(event.target.value)}
          className={cn(
            "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
          )}
        >
          <option value="" disabled>
            {carregandoContas ? "Carregando contas..." : "Selecione a conta de origem"}
          </option>
          {contas?.map((conta) => (
            <option key={conta.id} value={conta.id}>
              {conta.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="contaDestinoTransferencia">Conta de destino</Label>
        <select
          id="contaDestinoTransferencia"
          name="contaDestinoId"
          required
          disabled={carregandoContas}
          value={contaDestinoId}
          onChange={(event) => setContaDestinoId(event.target.value)}
          className={cn(
            "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
          )}
        >
          <option value="" disabled>
            {carregandoContas ? "Carregando contas..." : "Selecione a conta de destino"}
          </option>
          {contas?.map((conta) => (
            <option key={conta.id} value={conta.id}>
              {conta.nome}
            </option>
          ))}
        </select>
      </div>

      {erroContas && (
        <span className="text-[12px] text-alerta">
          Nao foi possivel carregar as contas. Tente novamente.
        </span>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorTransferencia">Valor</Label>
          <Input
            id="valorTransferencia"
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
          <Label htmlFor="dataTransferencia">Data</Label>
          <Input
            id="dataTransferencia"
            name="data"
            type="date"
            required
            value={data}
            onChange={(event) => setData(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoTransferencia">Descricao (opcional)</Label>
        <Input
          id="descricaoTransferencia"
          name="descricao"
          placeholder="Ex: Reserva para viagem"
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={limparFormulario} disabled={isSubmitting}>
          Limpar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
