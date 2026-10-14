import { situacaoDoVencimento } from "@/shared/lib/situacaoDoVencimento"
import { STATUS_CONTA_RECEBER, type ContaReceberResponse } from "@/features/contas-receber/types"

export type ResumoContasReceber = {
  aReceber: number
  jaRecebido: number
  atrasado: number
}

type ContaParaResumo = Pick<ContaReceberResponse, "valorTotal" | "saldoPendente" | "status" | "dataPrevista">

export function estaAtrasada(conta: ContaParaResumo, hoje: Date = new Date()): boolean {
  if (conta.status === STATUS_CONTA_RECEBER.RECEBIDO || conta.dataPrevista === null) return false
  return situacaoDoVencimento(conta.dataPrevista, hoje).situacao === "atrasado"
}

// Somas de exibicao sobre valores ja calculados pelo backend (item 13):
// A receber = saldo pendente das nao quitadas; Ja recebido = valorTotal -
// saldoPendente de todas; Atrasado = saldo pendente das nao quitadas com data
// prevista anterior a hoje (sobreposicao visual, nao muda o status da API).
export function resumirContasReceber(
  contas: ReadonlyArray<ContaParaResumo>,
  hoje: Date = new Date(),
): ResumoContasReceber {
  return contas.reduce<ResumoContasReceber>(
    (resumo, conta) => {
      const quitada = conta.status === STATUS_CONTA_RECEBER.RECEBIDO
      return {
        aReceber: resumo.aReceber + (quitada ? 0 : conta.saldoPendente),
        jaRecebido: resumo.jaRecebido + (conta.valorTotal - conta.saldoPendente),
        atrasado: resumo.atrasado + (estaAtrasada(conta, hoje) ? conta.saldoPendente : 0),
      }
    },
    { aReceber: 0, jaRecebido: 0, atrasado: 0 },
  )
}

export type DescricaoVencimento = { rotulo: string; atrasada: boolean }

const ROTULO_SEM_DATA = "—"

// "Venceu ha N dias" / "Vence em N dias" / "Vence hoje"; sem data, travessao.
// Conta quitada nao e cobrada de prazo: mostra a data prevista formatada
// pelo chamador (rotuloQuitada).
export function descreverVencimento(
  conta: ContaParaResumo,
  rotuloQuitada: (dataPrevista: string) => string,
  hoje: Date = new Date(),
): DescricaoVencimento {
  if (conta.dataPrevista === null) return { rotulo: ROTULO_SEM_DATA, atrasada: false }
  if (conta.status === STATUS_CONTA_RECEBER.RECEBIDO) return { rotulo: rotuloQuitada(conta.dataPrevista), atrasada: false }

  const { situacao, rotulo } = situacaoDoVencimento(conta.dataPrevista, hoje)
  return { rotulo, atrasada: situacao === "atrasado" }
}
