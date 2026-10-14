import { useState, type FormEvent } from "react"
import { useRegistrarAporte } from "@/features/investimentos/hooks/useRegistrarAporte"
import {
  converterValorParaNumero,
  validarValorPositivo,
} from "@/features/investimentos/lib/validarValorPositivo"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { dataDeHoje } from "@/features/investimentos/lib/dataDeHoje"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { ApiError } from "@/shared/api/client"
import type { AtivoResponse } from "@/features/investimentos/types"

type FormRegistrarAporteProps = {
  ativo: AtivoResponse
  onFechar: () => void
}

// Formulario "Novo aporte" (regra-de-negocio.md item 8.1): todo aporte alem
// do primeiro (que ja acontece no cadastro, ver ModalNovoAtivo) passa por
// aqui - gera um AtivoAporte imutavel no back e recalcula
// quantidade/valor_investido por media ponderada (Services/AtivoService.
// RegistrarAporte). O calculo NUNCA acontece no componente - so exibimos o
// preco medio que o back devolve (ativo.precoMedio), ver clean-code.md
// "Logica de calculo... vem do back". Diferente de AtivoItem/AtivoCard
// (container burro + apresentacao separados): este formulario e pequeno e
// vive um por ativo, entao ele mesmo guarda o estado local do form e chama a
// mutation - mesmo criterio de stack.md ("Renderiza JSX? -> components/",
// independente de usar hook), ja seguido por AtivoItem.tsx nesta feature.
export function FormRegistrarAporte({ ativo, onFechar }: FormRegistrarAporteProps) {
  const { mutate: registrarAporte, isPending } = useRegistrarAporte()

  const [quantidade, setQuantidade] = useState("")
  const [precoUnitario, setPrecoUnitario] = useState("")
  const [data, setData] = useState(dataDeHoje)
  const [erro, setErro] = useState<string | null>(null)
  const [aporteRegistrado, setAporteRegistrado] = useState(false)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroQuantidade = validarValorPositivo(quantidade, "Informe a quantidade.")
    if (erroQuantidade) {
      setErro(erroQuantidade)
      return
    }

    const erroPrecoUnitario = validarValorPositivo(precoUnitario, "Informe o preco unitario.")
    if (erroPrecoUnitario) {
      setErro(erroPrecoUnitario)
      return
    }

    if (data.trim().length === 0) {
      setErro("Informe a data do aporte.")
      return
    }

    registrarAporte(
      {
        ativoId: ativo.id,
        request: {
          quantidade: converterValorParaNumero(quantidade),
          precoUnitario: converterValorParaNumero(precoUnitario),
          data,
        },
      },
      {
        onSuccess: () => {
          setErro(null)
          setAporteRegistrado(true)
        },
        onError: (error) => {
          console.error("Falha ao registrar aporte", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar o aporte. Tente novamente.",
          )
        },
      },
    )
  }

  // ativo.precoMedio chega pela invalidacao de cache disparada por
  // useRegistrarAporte (ativos() e resumoAtivos()) - quando o refetch
  // completa, o `ativo` recebido via prop e atualizado e este texto
  // re-renderiza sozinho com o novo preco medio, sem calculo local.
  if (aporteRegistrado) {
    return (
      <FormModal title={`Novo aporte - ${ativo.nome}`} onClose={onFechar}>
        <Alert>
          <AlertDescription>
            Aporte registrado. Preco medio atualizado:{" "}
            <span className="font-medium text-positivo">{formatarMoeda(ativo.precoMedio)}</span>
          </AlertDescription>
        </Alert>
        <div className="flex justify-end">
          <Button type="button" onClick={onFechar} className={CLASSE_BOTAO_PRIMARIO}>
            Fechar
          </Button>
        </div>
      </FormModal>
    )
  }

  return (
    <FormModal title={`Novo aporte - ${ativo.nome}`} onClose={onFechar}>
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <div className="grid grid-cols-2 gap-2.5">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`quantidadeAporte-${ativo.id}`}>Quantidade</Label>
          <Input
            id={`quantidadeAporte-${ativo.id}`}
            type="number"
            step="0.000001"
            min="0.000001"
            inputMode="decimal"
            autoFocus
            required
            className={CLASSE_CAMPO}
            value={quantidade}
            onChange={(event) => setQuantidade(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`precoUnitarioAporte-${ativo.id}`}>Preco unitario</Label>
          <Input
            id={`precoUnitarioAporte-${ativo.id}`}
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            required
            className={CLASSE_CAMPO}
            value={precoUnitario}
            onChange={(event) => setPrecoUnitario(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor={`dataAporte-${ativo.id}`}>Data do aporte</Label>
        <Input
          id={`dataAporte-${ativo.id}`}
          type="date"
          required
          className={CLASSE_CAMPO}
          value={data}
          onChange={(event) => setData(event.target.value)}
        />
      </div>

      <div className="mt-1 flex justify-end gap-2.5">
        <Button
          type="button"
          variant="outline"
          onClick={onFechar}
          disabled={isPending}
          className={CLASSE_BOTAO_CANCELAR}
        >
          Cancelar
        </Button>
        <Button type="submit" disabled={isPending} className={CLASSE_BOTAO_PRIMARIO}>
          {isPending ? "Registrando..." : "Registrar aporte"}
        </Button>
      </div>
    </form>
    </FormModal>
  )
}
