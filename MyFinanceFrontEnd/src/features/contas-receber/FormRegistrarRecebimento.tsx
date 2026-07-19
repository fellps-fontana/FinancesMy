import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { dataDeHoje } from "@/features/cartao/lib/formatarData"
import { useRegistrarRecebimento } from "@/features/contas-receber/hooks/useRegistrarRecebimento"
import { useContasParaSelecao } from "@/features/contas-receber/hooks/useContasParaSelecao"
import {
  converterValorRecebimentoParaNumero,
  validarRecebimento,
} from "@/features/contas-receber/lib/validarRecebimento"

type FormRegistrarRecebimentoProps = {
  contaReceberId: string
  onRegistrado: () => void
  onCancelar: () => void
}

// Formulario de recebimento incremental (regra-de-negocio.md item 13): cada
// submit gera UM lancamento CREDIT vinculado ao conta_receber, na conta que o
// usuario escolher agora (pode variar entre recebimentos - por isso o select
// de conta de destino nunca herda nada do conta_receber pai). O backend
// rejeita com 422 (ValorRecebimentoExcedeSaldoPendenteException) se o valor
// exceder o saldo_pendente atual; o front so exibe a mensagem que ja vem
// pronta em ApiError.message, sem recalcular nem validar esse limite aqui -
// o item de lista nao tem o saldo_pendente "ao vivo" o suficiente pra validar
// client-side com seguranca, a fonte da verdade e o backend.
//
// Auto-contido (estado dos campos + mutation + query de contas de destino
// dentro do proprio form), mesmo espirito de FormRegistrarContaReceber: nao
// ha estado de lista a coordenar aqui, so o toggle aberto/fechado que o
// container (ContaReceberItem) ja controla.
//
// GAP CONHECIDO (mesma decisao do LancarCompraForm, features/cartao): nao ha
// endpoint de listagem de categorias do usuario (item 7) nem combobox de
// categoria pronto no projeto. Por isso o campo categoria_id fica de fora
// deste formulario - a request ja o trata como opcional (RegistrarRecebimen-
// toRequest.categoriaId?), e inventar um input de texto livre pra colar um
// UUID cru seria pior do que nao ter o campo. Decisao explicita, nao
// esquecimento; reportada na entrega da task.
export function FormRegistrarRecebimento({
  contaReceberId,
  onRegistrado,
  onCancelar,
}: FormRegistrarRecebimentoProps) {
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje())
  const [contaDestinoId, setContaDestinoId] = useState("")
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: registrarRecebimento, isPending } = useRegistrarRecebimento()

  const {
    data: contasDestino,
    isLoading: carregandoContasDestino,
    error: erroContasDestino,
  } = useContasParaSelecao()

  if (erroContasDestino) {
    console.error("Falha ao carregar contas de destino para recebimento", erroContasDestino)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarRecebimento(valor, contaDestinoId)
    if (erroValidacao) {
      setErro(erroValidacao)
      return
    }

    registrarRecebimento(
      {
        contaReceberId,
        request: {
          valor: converterValorRecebimentoParaNumero(valor),
          data,
          contaDestinoId,
        },
      },
      {
        onSuccess: onRegistrado,
        onError: (error) => {
          console.error("Falha ao registrar recebimento", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar o recebimento. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`valorRecebimento-${contaReceberId}`}>Valor recebido</Label>
          <Input
            id={`valorRecebimento-${contaReceberId}`}
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

        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`dataRecebimento-${contaReceberId}`}>Data</Label>
          <Input
            id={`dataRecebimento-${contaReceberId}`}
            type="date"
            required
            value={data}
            onChange={(event) => setData(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor={`contaDestinoRecebimento-${contaReceberId}`}>Conta de destino</Label>
        <select
          id={`contaDestinoRecebimento-${contaReceberId}`}
          required
          disabled={carregandoContasDestino}
          value={contaDestinoId}
          onChange={(event) => setContaDestinoId(event.target.value)}
          className={cn(
            "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
          )}
        >
          <option value="" disabled>
            {carregandoContasDestino ? "Carregando contas..." : "Selecione a conta de destino"}
          </option>
          {contasDestino?.map((conta) => (
            <option key={conta.id} value={conta.id}>
              {conta.nome}
            </option>
          ))}
        </select>
        {erroContasDestino && (
          <span className="text-[12px] text-alerta">
            Nao foi possivel carregar as contas de destino. Tente novamente.
          </span>
        )}
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onCancelar} disabled={isPending}>
          Cancelar
        </Button>
        <Button type="submit" size="sm" disabled={isPending}>
          {isPending ? "Registrando..." : "Registrar recebimento"}
        </Button>
      </div>
    </form>
  )
}
