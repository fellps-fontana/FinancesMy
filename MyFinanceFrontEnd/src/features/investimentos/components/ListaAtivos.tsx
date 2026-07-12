import { useState, type FormEvent } from "react"
import { useAtivosDaConta } from "@/features/investimentos/hooks/useAtivosDaConta"
import { useRegistrarCompraAtivo } from "@/features/investimentos/hooks/useRegistrarCompraAtivo"
import { useRegistrarVendaAtivo } from "@/features/investimentos/hooks/useRegistrarVendaAtivo"
import { calcularValorAtivo } from "@/features/investimentos/lib/calcularValorAtivo"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import {
  validarCompraAtivo,
  converterCompraParaNumero,
} from "@/features/investimentos/lib/validarCompraAtivo"
import {
  validarVendaAtivo,
  converterVendaParaNumero,
} from "@/features/investimentos/lib/validarVendaAtivo"
import { FormRegistrarCompraAtivo } from "@/features/investimentos/components/FormRegistrarCompraAtivo"
import { FormRegistrarVendaAtivo } from "@/features/investimentos/components/FormRegistrarVendaAtivo"
import { GraficoCotacaoAtivo } from "@/features/investimentos/components/GraficoCotacaoAtivo"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ApiError } from "@/shared/api/client"
import type { AtivoResponse } from "@/features/investimentos/types"

// Quantidade nao e valor monetario (formatarMoeda nao serve aqui), mas ainda
// e dado numerico exibido na tela - precisa de locale do projeto (pt-BR), nao
// String(numero) cru. Fracoes de ativo (ETF fracionario) usam ate 8 casas.
const formatadorQuantidade = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 0,
  maximumFractionDigits: 8,
})

function dataDeHoje(): string {
  return new Date().toISOString().slice(0, 10)
}

type ListaAtivosProps = {
  contaId: string
}

// Container: busca os ativos da conta (estado de servidor via
// useAtivosDaConta), guarda o estado de UI do formulario de compra (por
// conta) e aciona useRegistrarCompraAtivo. O formulario de venda (por ativo)
// e o toggle dele moram em AtivoLinha, ja que cada linha vende sua propria
// posicao - ver comentario da funcao. A apresentacao dos formularios fica em
// FormRegistrarCompraAtivo/FormRegistrarVendaAtivo - ver clean-code.md
// "Organizacao (React)" (estado de servidor separado da apresentacao,
// calculo de dominio fora do componente). So aparece dentro de uma conta em
// modo carteira, ja que ListaAtivos so e renderizada nesse contexto (ver
// ContaInvestimentoCard).
export function ListaAtivos({ contaId }: ListaAtivosProps) {
  const { data: ativos, isLoading, error } = useAtivosDaConta(contaId)
  const { mutate: registrarCompra, isPending: registrandoCompra } = useRegistrarCompraAtivo()

  const [mostrarFormulario, setMostrarFormulario] = useState(false)
  const [ticker, setTicker] = useState("")
  const [nome, setNome] = useState("")
  const [quantidade, setQuantidade] = useState("")
  const [precoUnitario, setPrecoUnitario] = useState("")
  const [data, setData] = useState(dataDeHoje)
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  // Log com contexto (qual conta falhou) antes da mensagem generica ao
  // usuario - mesmo padrao de ListaContasInvestimento.tsx (ver clean-code.md
  // "Tratamento de erro").
  if (error) {
    console.error(`Falha ao carregar ativos da conta de investimento - contaId=${contaId}`, error)
  }

  function abrirFormulario() {
    setTicker("")
    setNome("")
    setQuantidade("")
    setPrecoUnitario("")
    setData(dataDeHoje())
    setErroFormulario(null)
    setMostrarFormulario(true)
  }

  function fecharFormulario() {
    setMostrarFormulario(false)
    setErroFormulario(null)
  }

  function handleSubmitCompra(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarCompraAtivo(ticker, quantidade, precoUnitario, data)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    registrarCompra(
      {
        contaId,
        request: {
          ticker: ticker.trim(),
          nome: nome.trim().length > 0 ? nome.trim() : undefined,
          quantidade: converterCompraParaNumero(quantidade),
          precoUnitario: converterCompraParaNumero(precoUnitario),
          data,
        },
      },
      {
        onSuccess: fecharFormulario,
        onError: (erroCompra) => {
          console.error(
            `Falha ao registrar compra de ativo - contaId=${contaId}, ticker=${ticker}`,
            erroCompra,
          )
          setErroFormulario(
            erroCompra instanceof ApiError
              ? erroCompra.message
              : "Nao foi possivel registrar a compra. Tente novamente.",
          )
        },
      },
    )
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Nao foi possivel carregar os ativos</AlertTitle>
        <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {isLoading ? (
        <p className="text-[13px] text-muted-foreground">Carregando ativos...</p>
      ) : !ativos || ativos.length === 0 ? (
        <p className="text-[13px] text-muted-foreground">
          Nenhum ativo registrado nesta carteira ainda.
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {ativos.map((ativo) => (
            <AtivoLinha key={ativo.id} ativo={ativo} contaId={contaId} />
          ))}
        </ul>
      )}

      {mostrarFormulario ? (
        <FormRegistrarCompraAtivo
          ticker={ticker}
          nome={nome}
          quantidade={quantidade}
          precoUnitario={precoUnitario}
          data={data}
          isSubmitting={registrandoCompra}
          errorMessage={erroFormulario}
          onTickerChange={setTicker}
          onNomeChange={setNome}
          onQuantidadeChange={setQuantidade}
          onPrecoUnitarioChange={setPrecoUnitario}
          onDataChange={setData}
          onSubmit={handleSubmitCompra}
          onCancelar={fecharFormulario}
        />
      ) : (
        <div className="flex justify-end">
          <Button type="button" variant="outline" size="sm" onClick={abrirFormulario}>
            Comprar ativo
          </Button>
        </div>
      )}
    </div>
  )
}

type AtivoLinhaProps = {
  ativo: AtivoResponse
  contaId: string
}

// Linha do ativo: alem de exibir o dado ja pronto (valor via
// calcularValorAtivo - regra-de-negocio.md item 8.1/8.4), tambem guarda o
// estado de UI do formulario de venda e aciona useRegistrarVendaAtivo. Venda
// e por ativo (nao por conta, como a compra em ListaAtivos), entao o estado
// do formulario mora aqui, escopado a esta linha - mesmo espirito de
// container+apresentacao usado em ContaInvestimentoItem/ContaInvestimentoCard
// (clean-code.md "Organizacao (React)"), so que sem arquivo separado porque o
// escopo desta tarefa restringe os arquivos tocados. Quando a venda zera a
// quantidade, o backend desativa o ativo (item 8.3) e a invalidacao de cache
// de useRegistrarVendaAtivo faz o ativo sumir da lista - esta linha
// simplesmente deixa de ser renderizada, sem tratamento especial aqui.
// O toggle "Ver grafico" so monta GraficoCotacaoAtivo quando aberto - a busca
// de cotacao (regra-de-negocio.md item 8) e sob demanda, entao fechar o
// toggle desmonta o componente e nao ha polling em background.
function AtivoLinha({ ativo, contaId }: AtivoLinhaProps) {
  const valorAtivo = calcularValorAtivo(ativo)
  const { mutate: registrarVenda, isPending: registrandoVenda } = useRegistrarVendaAtivo()

  const [mostrarFormularioVenda, setMostrarFormularioVenda] = useState(false)
  const [quantidade, setQuantidade] = useState("")
  const [precoUnitario, setPrecoUnitario] = useState("")
  const [data, setData] = useState(dataDeHoje)
  const [observacao, setObservacao] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)
  const [mostrarGrafico, setMostrarGrafico] = useState(false)

  function abrirFormularioVenda() {
    setQuantidade("")
    setPrecoUnitario("")
    setData(dataDeHoje())
    setObservacao("")
    setErroFormulario(null)
    setMostrarFormularioVenda(true)
  }

  function fecharFormularioVenda() {
    setMostrarFormularioVenda(false)
    setErroFormulario(null)
  }

  function handleSubmitVenda(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarVendaAtivo(quantidade, precoUnitario, data, ativo.quantidade)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    registrarVenda(
      {
        contaId,
        ativoId: ativo.id,
        request: {
          quantidade: converterVendaParaNumero(quantidade),
          precoUnitario: converterVendaParaNumero(precoUnitario),
          data,
          observacao: observacao.trim().length > 0 ? observacao.trim() : undefined,
        },
      },
      {
        onSuccess: fecharFormularioVenda,
        onError: (erroVenda) => {
          console.error(
            `Falha ao registrar venda de ativo - contaId=${contaId}, ativoId=${ativo.id}`,
            erroVenda,
          )
          setErroFormulario(
            erroVenda instanceof ApiError
              ? erroVenda.message
              : "Nao foi possivel registrar a venda. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <li className="flex flex-col gap-2.5 rounded-lg border border-border bg-secondary px-3 py-2.5">
      <div className="flex items-center justify-between gap-2">
        <div className="flex flex-col">
          <button
            type="button"
            onClick={() => setMostrarGrafico((atual) => !atual)}
            aria-expanded={mostrarGrafico}
            className="w-fit text-left text-sm font-medium text-secondary-foreground underline-offset-2 hover:text-primary hover:underline"
          >
            {ativo.ticker}
          </button>
          {ativo.nome && (
            <span className="text-[12px] text-muted-foreground">{ativo.nome}</span>
          )}
        </div>
        <span className="text-sm font-medium text-secondary-foreground">
          {formatarMoeda(valorAtivo)}
        </span>
      </div>
      <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 text-[12px] text-muted-foreground">
        <span>{formatadorQuantidade.format(ativo.quantidade)} un.</span>
        <span>Preco medio {formatarMoeda(ativo.precoMedio)}</span>
        <span>Preco atual {formatarMoeda(ativo.precoAtual)}</span>
      </div>

      {mostrarGrafico && <GraficoCotacaoAtivo ticker={ativo.ticker} />}

      {mostrarFormularioVenda ? (
        <FormRegistrarVendaAtivo
          ativoId={ativo.id}
          ticker={ativo.ticker}
          quantidadeDisponivelFormatada={formatadorQuantidade.format(ativo.quantidade)}
          quantidade={quantidade}
          precoUnitario={precoUnitario}
          data={data}
          observacao={observacao}
          isSubmitting={registrandoVenda}
          errorMessage={erroFormulario}
          onQuantidadeChange={setQuantidade}
          onPrecoUnitarioChange={setPrecoUnitario}
          onDataChange={setData}
          onObservacaoChange={setObservacao}
          onSubmit={handleSubmitVenda}
          onCancelar={fecharFormularioVenda}
        />
      ) : (
        <div className="flex justify-end">
          <Button type="button" variant="ghost" size="sm" onClick={abrirFormularioVenda}>
            Vender
          </Button>
        </div>
      )}
    </li>
  )
}
