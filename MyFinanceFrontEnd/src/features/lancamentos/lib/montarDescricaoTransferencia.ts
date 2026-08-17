import type { ContaParaExibicao, TransferenciaFluxoCaixa } from "../types"

export type DescricaoTransferencia = {
  titulo: string
  subtitulo: string | undefined
}

const NOME_CONTA_DESCONHECIDA = "Conta"

// Resolve id -> nome so por lookup (sem calculo de saldo/dominio nenhum) -
// `contas` pode ainda nao ter chegado (useContasParaExibicaoTransferencia em
// loading), entao cai no rotulo generico em vez de travar a exibicao.
function nomeDaConta(id: string, contas: ContaParaExibicao[] | undefined): string {
  return contas?.find((conta) => conta.id === id)?.nome ?? NOME_CONTA_DESCONHECIDA
}

// Decide como uma linha de TRANSFERENCIA do fluxo de caixa agregado deve ser
// rotulada (components/TransferenciaFluxoCaixaItem.tsx) - tres casos
// mutuamente exclusivos, na mesma ordem de precedencia da regra de negocio:
//   1. Pagamento de fatura (item 12): `ehPagamentoFatura` prevalece sobre
//      qualquer outra leitura - o titulo vira "Pagamento de fatura", o fluxo
//      origem -> destino (conta banco -> conta cartao) vira subtitulo.
//   2. Emprestimo de perna unica (item 13): `contaDestinoId === null` -
//      titulo "Emprestimo", subtitulo mostra so a conta de origem (nao ha
//      conta destino real, o destino e uma pessoa fora do sistema).
//   3. Transferencia comum (item 3): titulo e o proprio fluxo
//      "Conta A -> Conta B"; subtitulo usa a descricao livre da transferencia
//      quando existir.
export function montarDescricaoTransferencia(
  transferencia: TransferenciaFluxoCaixa,
  contas: ContaParaExibicao[] | undefined,
): DescricaoTransferencia {
  const nomeOrigem = nomeDaConta(transferencia.contaOrigemId, contas)

  if (transferencia.ehPagamentoFatura) {
    const nomeDestino = transferencia.contaDestinoId
      ? nomeDaConta(transferencia.contaDestinoId, contas)
      : NOME_CONTA_DESCONHECIDA
    return { titulo: "Pagamento de fatura", subtitulo: `${nomeOrigem} -> ${nomeDestino}` }
  }

  if (transferencia.contaDestinoId === null) {
    return { titulo: "Emprestimo", subtitulo: nomeOrigem }
  }

  const nomeDestino = nomeDaConta(transferencia.contaDestinoId, contas)
  return { titulo: `${nomeOrigem} -> ${nomeDestino}`, subtitulo: transferencia.descricao ?? undefined }
}
