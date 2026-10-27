import { useState } from "react"
import { ApiError } from "@/shared/api/client"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { useBuscarPlanoParaCopia } from "@/features/planejamento-mensal/hooks/useBuscarPlanoParaCopia"
import {
  MESES_DO_ANO,
  deslocarMes,
  ehMesmoMes,
  estaDentroDoHorizonte,
  rotuloMes,
  type MesPlano,
} from "@/features/planejamento-mensal/lib/mes"
import type { OpcoesCopiaPlano } from "@/features/planejamento-mensal/lib/rascunho"
import type { PlanoMensalResponse } from "@/features/planejamento-mensal/types"

type CopiarPlanoModalProps = {
  open: boolean
  periodoAtual: MesPlano
  onClose: () => void
  onConfirmarCopia: (planoOrigem: PlanoMensalResponse, opcoes: OpcoesCopiaPlano) => void
}

export function CopiarPlanoModal({
  open,
  periodoAtual,
  onClose,
  onConfirmarCopia,
}: CopiarPlanoModalProps) {
  const sugestaoInicial = deslocarMes(periodoAtual, -1)
  const [mesSelecionado, setMesSelecionado] = useState<number>(sugestaoInicial.mes)
  const [anoTexto, setAnoTexto] = useState<string>(String(sugestaoInicial.ano))
  const [copiarRenda, setCopiarRenda] = useState<boolean>(false)
  const [mesSemPlano, setMesSemPlano] = useState<MesPlano | null>(null)

  const buscarPlano = useBuscarPlanoParaCopia()

  const anoNumero = Number(anoTexto.trim())
  const anoValido = Number.isInteger(anoNumero) && anoNumero >= 2000 && anoNumero <= 2100
  const periodoEscolhido: MesPlano = {
    ano: anoValido ? anoNumero : sugestaoInicial.ano,
    mes: mesSelecionado,
  }

  const selecionouMesmoMes = anoValido && ehMesmoMes(periodoEscolhido, periodoAtual)
  const foraDoHorizonte = anoValido && !estaDentroDoHorizonte(periodoEscolhido)
  const selecaoValida = anoValido && !selecionouMesmoMes && !foraDoHorizonte

  function alterarMes(novoMes: number) {
    setMesSelecionado(novoMes)
    setMesSemPlano(null)
    buscarPlano.reset()
  }

  function alterarAno(novoAno: string) {
    setAnoTexto(novoAno)
    setMesSemPlano(null)
    buscarPlano.reset()
  }

  function submeter(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!selecaoValida || buscarPlano.isPending) return

    setMesSemPlano(null)
    buscarPlano.mutate(periodoEscolhido, {
      onSuccess: (planoOrigem) => {
        if (!planoOrigem) {
          setMesSemPlano(periodoEscolhido)
          return
        }
        onConfirmarCopia(planoOrigem, { copiarRenda })
      },
    })
  }

  const mensagemErroBusca =
    buscarPlano.error instanceof ApiError
      ? buscarPlano.error.message
      : buscarPlano.error
        ? "Não foi possível buscar o planejamento do mês selecionado."
        : null

  return (
    <FormModal open={open} title="Copiar de outro mês" onClose={onClose}>
      <form className="flex flex-col gap-4" onSubmit={submeter}>
        <p className="text-[13px] text-text-muted">
          Preenche o rascunho de {rotuloMes(periodoAtual)} com os percentuais de investimento e de
          sublimites de outro mês. Os valores em R$ são recalculados automaticamente e você poderá
          revisar antes de salvar.
        </p>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="copia-mes">Mês de origem</Label>
            <select
              id="copia-mes"
              value={mesSelecionado}
              onChange={(e) => alterarMes(Number(e.target.value))}
              className={CLASSE_CAMPO}
            >
              {MESES_DO_ANO.map((m) => (
                <option key={m.valor} value={m.valor}>
                  {m.rotulo}
                </option>
              ))}
            </select>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="copia-ano">Ano</Label>
            <Input
              id="copia-ano"
              type="number"
              min={2000}
              max={2100}
              value={anoTexto}
              onChange={(e) => alterarAno(e.target.value)}
              className={CLASSE_CAMPO}
            />
          </div>
        </div>

        <label
          htmlFor="copiar-renda-prevista"
          className="flex cursor-pointer items-start gap-2.5 rounded-[10px] border-[0.5px] border-border bg-bg-surface-alt/30 p-3"
        >
          <input
            id="copiar-renda-prevista"
            type="checkbox"
            checked={copiarRenda}
            onChange={(e) => setCopiarRenda(e.target.checked)}
            className="mt-0.5 size-4 rounded border-border accent-primary"
          />
          <span className="flex flex-col gap-0.5">
            <span className="text-sm font-medium text-text-body">
              Copiar também a renda prevista
            </span>
            <span className="text-[12px] text-text-faint">
              Se desmarcado, mantém a renda prevista atual de {rotuloMes(periodoAtual)} e copia
              apenas os percentuais.
            </span>
          </span>
        </label>

        {!anoValido && (
          <p className="rounded-[6px] bg-negativo/15 px-3 py-2 text-[12px] text-negativo">
            Informe um ano válido entre 2000 e 2100.
          </p>
        )}

        {selecionouMesmoMes && (
          <p className="rounded-[6px] bg-alerta/15 px-3 py-2 text-[12px] text-alerta">
            Escolha um mês diferente do mês atualmente aberto ({rotuloMes(periodoAtual)}).
          </p>
        )}

        {foraDoHorizonte && (
          <p className="rounded-[6px] bg-negativo/15 px-3 py-2 text-[12px] text-negativo">
            Não é possível consultar planos além do horizonte de 12 meses futuros.
          </p>
        )}

        {mesSemPlano && (
          <Alert variant="destructive" className="border-negativo/40 bg-negativo/15">
            <AlertTitle>Mês sem planejamento salvo</AlertTitle>
            <AlertDescription>
              Não há planejamento salvo em {rotuloMes(mesSemPlano)} para copiar.
            </AlertDescription>
          </Alert>
        )}

        {mensagemErroBusca && (
          <Alert variant="destructive">
            <AlertTitle>Falha ao buscar planejamento</AlertTitle>
            <AlertDescription>{mensagemErroBusca}</AlertDescription>
          </Alert>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={buscarPlano.isPending}
            className={CLASSE_BOTAO_CANCELAR}
          >
            Cancelar
          </Button>
          <Button
            type="submit"
            disabled={!selecaoValida || buscarPlano.isPending}
            className={CLASSE_BOTAO_PRIMARIO}
          >
            {buscarPlano.isPending ? "Buscando..." : "Aplicar no rascunho"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
