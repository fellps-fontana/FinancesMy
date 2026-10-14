import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { useRegistrarDividendo } from "@/features/investimentos/hooks/useRegistrarDividendo"
import { dataDeHoje } from "@/shared/lib/datas"
import { validarDividendo } from "@/features/investimentos/lib/validarDividendo"
import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"

type FormRegistrarDividendoProps = {
  ativoId: string
  ativoNome: string
  onFechar: () => void
}

// Modal "Registrar dividendo" (regra-de-negocio.md item 8.4): so coleta valor
// e data - tipo (DIVIDENDO) e origem (MANUAL) sao decididos pelo backend.
export function FormRegistrarDividendo({ ativoId, ativoNome, onFechar }: FormRegistrarDividendoProps) {
  const { mutate: registrarDividendo, isPending } = useRegistrarDividendo()
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje)
  const [erro, setErro] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarDividendo(valor, data)
    if (erroValidacao) {
      setErro(erroValidacao)
      return
    }

    registrarDividendo(
      { ativoId, request: { valor: converterValorParaNumero(valor), data } },
      {
        onSuccess: onFechar,
        onError: (error) => {
          console.error("Falha ao registrar dividendo", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar o dividendo. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <FormModal title={`Registrar dividendo - ${ativoNome}`} onClose={onFechar}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="grid grid-cols-2 gap-2.5">
          <div className="flex min-w-0 flex-col gap-1.5">
            <Label htmlFor="valorRegistrarDividendo">Valor</Label>
            <Input
              id="valorRegistrarDividendo"
              type="number"
              step="0.01"
              min="0.01"
              inputMode="decimal"
              autoFocus
              required
              value={valor}
              onChange={(event) => setValor(event.target.value)}
              className={CLASSE_CAMPO}
            />
          </div>
          <div className="flex min-w-0 flex-col gap-1.5">
            <Label htmlFor="dataRegistrarDividendo">Data</Label>
            <Input
              id="dataRegistrarDividendo"
              type="date"
              required
              value={data}
              onChange={(event) => setData(event.target.value)}
              className={CLASSE_CAMPO}
            />
          </div>
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
            {isPending ? "Salvando..." : "Salvar dividendo"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
