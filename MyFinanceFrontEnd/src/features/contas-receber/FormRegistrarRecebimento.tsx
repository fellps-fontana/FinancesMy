import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { Chip } from "@/shared/ui/chip"
import { SelectField } from "@/shared/ui/select-field"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { dataDeHoje } from "@/shared/lib/datas"
import { BarraRecebido } from "@/features/contas-receber/components/ContaReceberItem"
import { useRegistrarRecebimento } from "@/features/contas-receber/hooks/useRegistrarRecebimento"
import { useContasParaSelecao } from "@/shared/hooks/useContasParaSelecao"
import {
  converterValorRecebimentoParaNumero,
  validarRecebimento,
} from "@/features/contas-receber/lib/validarRecebimento"

type TipoRecebimento = "PARCIAL" | "TOTAL"


type FormRegistrarRecebimentoProps = {
  contaReceberId: string
  descricao: string
  valorTotal: number
  saldoPendente: number
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
  descricao,
  valorTotal,
  saldoPendente,
  onRegistrado,
  onCancelar,
}: FormRegistrarRecebimentoProps) {
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje())
  const [contaDestinoId, setContaDestinoId] = useState("")
  const [erro, setErro] = useState<string | null>(null)
  const [tipoRecebimento, setTipoRecebimento] = useState<TipoRecebimento>("PARCIAL")

  const { mutate: registrarRecebimento, isPending } = useRegistrarRecebimento()

  // Card-resumo (mockup "12 Contas a Receber.dc.html"): "recebido" e a mesma
  // subtracao que ja define saldo_pendente na regra-de-negocio.md item 13
  // (saldo_pendente = valor_total - soma(recebimentos)), so invertida para
  // exibicao - valorTotal/saldoPendente ja chegam prontos do backend, nenhum
  // estado novo de dominio nasce aqui. percentualRecebido e clamp puramente
  // visual (largura da barra), sem efeito em nenhuma decisao de fluxo.
  const valorRecebido = valorTotal - saldoPendente

  // Toggle Parcial/Total (mockup): "Total" trava o campo Valor no
  // saldo_pendente atual - o usuario nao deveria digitar algo diferente do
  // que falta para quitar. "Parcial" libera o campo e limpa o valor
  // auto-preenchido pelo modo Total, para nao deixar lixo de um modo no
  // outro (unico caminho que preenche valor durante o modo Total e este
  // handler, entao limpar ao sair dele e seguro).
  function handleSelecionarParcial() {
    if (tipoRecebimento === "PARCIAL") return
    setTipoRecebimento("PARCIAL")
    setValor("")
  }

  function handleSelecionarTotal() {
    if (tipoRecebimento === "TOTAL") return
    setTipoRecebimento("TOTAL")
    setValor(saldoPendente.toFixed(2))
  }

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
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col rounded-xl bg-accent p-3.5">
        <span className="text-sm text-text-body">{descricao}</span>
        <div className="mt-2 flex items-center justify-between text-xs text-text-muted">
          <span>Recebido</span>
          <span>
            {formatarMoeda(valorRecebido)} de {formatarMoeda(valorTotal)}
          </span>
        </div>
        <BarraRecebido valorTotal={valorTotal} saldoPendente={saldoPendente} className="mt-1.5 bg-border" />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label>Tipo de recebimento</Label>
        <div className="flex gap-2" role="group" aria-label="Tipo de recebimento">
          <Chip
            selected={tipoRecebimento === "PARCIAL"}
            onClick={handleSelecionarParcial}
            className="flex-1 rounded-[9px] py-2.5 text-[13px]"
          >
            Parcial
          </Chip>
          <Chip
            selected={tipoRecebimento === "TOTAL"}
            onClick={handleSelecionarTotal}
            className="flex-1 rounded-[9px] py-2.5 text-[13px]"
          >
            Total ({formatarMoeda(saldoPendente)} restante)
          </Chip>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2.5">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`valorRecebimento-${contaReceberId}`}>Valor recebido</Label>
          <Input
            id={`valorRecebimento-${contaReceberId}`}
            className={CLASSE_CAMPO}
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            autoFocus
            required
            disabled={tipoRecebimento === "TOTAL"}
            value={valor}
            onChange={(event) => setValor(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`dataRecebimento-${contaReceberId}`}>Data</Label>
          <Input
            id={`dataRecebimento-${contaReceberId}`}
            className={CLASSE_CAMPO}
            type="date"
            required
            value={data}
            onChange={(event) => setData(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <SelectField
          label="Conta de destino"
          placeholder={carregandoContasDestino ? "Carregando contas..." : "Selecione a conta de destino"}
          disabled={carregandoContasDestino}
          options={(contasDestino ?? []).map((conta) => ({ value: conta.id, label: conta.nome }))}
          value={contaDestinoId || null}
          onChange={setContaDestinoId}
        />
        {erroContasDestino && (
          <span className="text-[12px] text-alerta">
            Nao foi possivel carregar as contas de destino. Tente novamente.
          </span>
        )}
      </div>

      <div className="mt-1.5 flex justify-end gap-2.5">
        <Button
          type="button"
          variant="outline"
          onClick={onCancelar}
          disabled={isPending}
          className={CLASSE_BOTAO_CANCELAR}
        >
          Cancelar
        </Button>
        <Button type="submit" disabled={isPending} className={CLASSE_BOTAO_PRIMARIO}>
          {isPending ? "Registrando..." : "Confirmar recebimento"}
        </Button>
      </div>
    </form>
  )
}
