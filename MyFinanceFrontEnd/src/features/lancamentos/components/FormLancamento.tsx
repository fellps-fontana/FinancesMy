import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { dataDeHoje } from "@/features/cartao/lib/formatarData"
import { CategoriaSelect } from "@/features/categorias/components/CategoriaSelect"
import { AvisoLimiteGasto } from "@/features/lancamentos/components/AvisoLimiteGasto"
import { useCriarLancamento } from "@/features/lancamentos/hooks/useCriarLancamento"
import { useEditarLancamento } from "@/features/lancamentos/hooks/useEditarLancamento"
import type { LancamentoResponse, TipoLancamento } from "@/features/lancamentos/types"

// Status ofertado no form (regra-de-negocio.md item 5): SUGERIDO NUNCA e uma
// opcao aqui - so existe quando a conciliacao Open Finance entrar em v2, e
// nasce da conciliacao automatica, nunca de escolha manual do usuario. O tipo
// StatusLancamento (types.ts) inclui os 3 valores porque espelha o backend,
// mas este form restringe deliberadamente as opcoes visiveis.
type StatusFormLancamento = "PENDENTE" | "PAGO"

const STATUS_OPCOES: { value: StatusFormLancamento; label: string }[] = [
  { value: "PENDENTE", label: "Pendente" },
  { value: "PAGO", label: "Pago" },
]

// tipo (DEBIT/CREDIT) decide o tipo de categoria oferecido pelo CategoriaSelect
// (regra-de-negocio.md item 2 + item 7: categoria tem `tipo` DESPESA|RECEITA).
// Rotulo do segmented control usa a mesma linguagem de categoria (Despesa/
// Receita) em vez de Debito/Credito - termo tecnico do dado, nao da tela.
const LABEL_POR_TIPO: Record<TipoLancamento, string> = {
  DEBIT: "Despesa",
  CREDIT: "Receita",
}

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

function validarFormulario(descricao: string, valor: string, data: string): string | null {
  if (descricao.trim().length === 0) {
    return "Informe uma descricao."
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

// Extrai ano/mes de uma data "yyyy-MM-dd" (mesmo formato de <input type=date>)
// so pra alimentar AvisoLimiteGasto (regra-de-negocio.md item 14, "Tela de
// lancamento: aviso ao selecionar/criar lancamento numa categoria perto ou
// acima do limite") - o mes calendario do lancamento, nao o mes corrente do
// sistema. Nao e calculo de dominio (gasto/limite continua 100% no backend
// via useGastoVsLimite), so leitura de componentes de string ja validada.
function extrairAnoMes(data: string): { ano: number; mes: number } {
  const [ano, mes] = data.split("-").map(Number)
  return { ano, mes }
}

type FormLancamentoProps = {
  // Endpoint de lancamento vive sob /api/contas/{contaId}/lancamentos - nao
  // ha campo de "conta" neste form (escopo da task nao lista selecao de
  // conta), entao a conta ja em contexto na tela que hospeda o form (a
  // pagina roteada, TASK-095) precisa ser passada aqui. Em modo edicao o
  // contaId real vem de `lancamentoParaEditar.contaId` (o lancamento ja
  // sabe a que conta pertence); a prop serve so pro modo criar.
  contaId: string
  lancamentoParaEditar?: LancamentoResponse
  onSalvar?: () => void
}

// Mesmo padrao de FormContaFixa: presenca de `lancamentoParaEditar` decide
// criar (useCriarLancamento) vs editar (useEditarLancamento).
export function FormLancamento({ contaId, lancamentoParaEditar, onSalvar }: FormLancamentoProps) {
  const modoEdicao = lancamentoParaEditar !== undefined
  const contaIdEfetivo = lancamentoParaEditar?.contaId ?? contaId

  const [descricao, setDescricao] = useState(lancamentoParaEditar?.descricao ?? "")
  const [valor, setValor] = useState(
    lancamentoParaEditar ? String(lancamentoParaEditar.valor) : "",
  )
  const [tipo, setTipo] = useState<TipoLancamento>(lancamentoParaEditar?.tipo ?? "DEBIT")
  const [data, setData] = useState(lancamentoParaEditar?.data ?? dataDeHoje())
  const [status, setStatus] = useState<StatusFormLancamento>(
    lancamentoParaEditar?.status === "PAGO" ? "PAGO" : "PENDENTE",
  )
  const [categoriaId, setCategoriaId] = useState<string | undefined>(
    lancamentoParaEditar?.categoriaId ?? undefined,
  )
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarLancamento, isPending: criando } = useCriarLancamento()
  const { mutate: editarLancamento, isPending: editando } = useEditarLancamento()

  const isSubmitting = criando || editando
  const tipoCategoria = tipo === "DEBIT" ? "Despesa" : "Receita"
  const { ano, mes } = extrairAnoMes(data)

  function restaurarValoresIniciais() {
    setDescricao(lancamentoParaEditar?.descricao ?? "")
    setValor(lancamentoParaEditar ? String(lancamentoParaEditar.valor) : "")
    setTipo(lancamentoParaEditar?.tipo ?? "DEBIT")
    setData(lancamentoParaEditar?.data ?? dataDeHoje())
    setStatus(lancamentoParaEditar?.status === "PAGO" ? "PAGO" : "PENDENTE")
    setCategoriaId(lancamentoParaEditar?.categoriaId ?? undefined)
    setErroFormulario(null)
  }

  // Trocar o tipo muda o universo de categorias validas (Despesa vs Receita,
  // regra-de-negocio.md item 7) - a categoria selecionada antes da troca
  // pode nao existir no outro tipo, entao zera pra nao enviar um
  // categoriaId de tipo incompativel.
  function handleTrocarTipo(novoTipo: TipoLancamento) {
    setTipo(novoTipo)
    setCategoriaId(undefined)
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarFormulario(descricao, valor, data)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    if (modoEdicao && lancamentoParaEditar) {
      editarLancamento(
        {
          contaId: contaIdEfetivo,
          lancamentoId: lancamentoParaEditar.id,
          request: {
            descricao: descricao.trim(),
            valor: converterValorParaNumero(valor),
            categoriaId,
            tipo,
            data,
            status,
          },
        },
        {
          onSuccess: () => {
            setErroFormulario(null)
            onSalvar?.()
          },
          onError: (error) => {
            console.error("Falha ao editar lancamento", error)
            setErroFormulario(
              error instanceof ApiError
                ? error.message
                : "Nao foi possivel salvar o lancamento. Tente novamente.",
            )
          },
        },
      )
      return
    }

    criarLancamento(
      {
        contaId: contaIdEfetivo,
        request: {
          descricao: descricao.trim(),
          valor: converterValorParaNumero(valor),
          categoriaId,
          tipo,
          data,
          status,
        },
      },
      {
        onSuccess: () => {
          restaurarValoresIniciais()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar lancamento", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel criar o lancamento. Tente novamente.",
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
        <Label htmlFor="descricaoLancamento">Descricao</Label>
        <Input
          id="descricaoLancamento"
          name="descricao"
          placeholder="Ex: Supermercado"
          autoFocus={!modoEdicao}
          required
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      {/* Segmented control de tipo (regra-de-negocio.md item 2, CRITICA): o
          sinal de entrada/saida vem SEMPRE de `tipo`, nunca decidido pelo
          front a partir do valor - mesmo padrao ja usado no toggle
          Recebivel/Emprestimo de FormRegistrarContaReceber (dois Button
          com variant condicional, sem componente de tabs novo). */}
      <div className="flex gap-2" role="group" aria-label="Tipo de lancamento">
        <Button
          type="button"
          variant={tipo === "DEBIT" ? "default" : "outline"}
          onClick={() => handleTrocarTipo("DEBIT")}
          disabled={isSubmitting}
          className="flex-1"
        >
          {LABEL_POR_TIPO.DEBIT}
        </Button>
        <Button
          type="button"
          variant={tipo === "CREDIT" ? "default" : "outline"}
          onClick={() => handleTrocarTipo("CREDIT")}
          disabled={isSubmitting}
          className="flex-1"
        >
          {LABEL_POR_TIPO.CREDIT}
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorLancamento">Valor</Label>
          <Input
            id="valorLancamento"
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
          <Label htmlFor="dataLancamento">Data</Label>
          <Input
            id="dataLancamento"
            name="data"
            type="date"
            required
            value={data}
            onChange={(event) => setData(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="statusLancamento">Status</Label>
        <select
          id="statusLancamento"
          name="status"
          required
          value={status}
          onChange={(event) => setStatus(event.target.value as StatusFormLancamento)}
          className={cn(
            "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
          )}
        >
          {STATUS_OPCOES.map((opcao) => (
            <option key={opcao.value} value={opcao.value}>
              {opcao.label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="categoriaLancamento">Categoria</Label>
        <CategoriaSelect tipo={tipoCategoria} value={categoriaId} onChange={setCategoriaId} />
      </div>

      {/* Aviso de limite (regra-de-negocio.md item 14) so faz sentido pra
          DESPESA (limite_gasto e exclusivo de categoria tipo DESPESA) e so
          quando ja existe categoria selecionada - AvisoLimiteGasto ja trata
          ausencia de limite (404) internamente, mas sem categoria nao ha o
          que consultar. */}
      {tipo === "DEBIT" && categoriaId && (
        <AvisoLimiteGasto categoriaId={categoriaId} ano={ano} mes={mes} />
      )}

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={restaurarValoresIniciais}
          disabled={isSubmitting}
        >
          {modoEdicao ? "Desfazer" : "Limpar"}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
