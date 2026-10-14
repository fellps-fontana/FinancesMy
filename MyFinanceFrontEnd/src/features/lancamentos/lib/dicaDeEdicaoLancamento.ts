import { destinoDeEdicao } from "@/features/lancamentos/lib/destinoDeEdicao"
import type { LancamentoResponse } from "@/features/lancamentos/types"

export type DisponibilidadeDeEdicao = {
  // Fonte: `avulso` do backend (item 18). O front nunca recalcula a partir de vinculos.
  podeEditarExcluir: boolean
  // Texto "edite em X" quando desabilitado; null quando habilitado.
  dica: string | null
  // Rota do destino, quando existe tela de origem para navegar.
  rotaDestino: string | null
  rotuloDestino: string | null
}

const DICA_SEM_DESTINO = "Este lancamento nao pode ser alterado por aqui"

export function resolverDisponibilidadeDeEdicao(
  lancamento: LancamentoResponse,
): DisponibilidadeDeEdicao {
  if (lancamento.avulso) {
    return { podeEditarExcluir: true, dica: null, rotaDestino: null, rotuloDestino: null }
  }

  const destino = destinoDeEdicao(lancamento.origemVinculo)

  if (!destino) {
    return { podeEditarExcluir: false, dica: DICA_SEM_DESTINO, rotaDestino: null, rotuloDestino: null }
  }

  return {
    podeEditarExcluir: false,
    dica: destino.dica,
    rotaDestino: destino.rota,
    rotuloDestino: destino.rotulo,
  }
}
