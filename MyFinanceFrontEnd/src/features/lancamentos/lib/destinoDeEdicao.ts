import type { OrigemVinculo } from "@/features/lancamentos/types"

// Item 18: lancamento com vinculo e editado/excluido na origem. `rota` null
// = a acao acontece na propria tela (ou nao ha navegacao).
export type DestinoDeEdicao = {
  rotulo: string
  rota: string | null
  // Frase exibida quando editar/excluir esta desabilitado.
  dica: string
}

const DESTINOS: Record<OrigemVinculo, DestinoDeEdicao | null> = {
  AVULSO: null,
  CONTA_FIXA: { rotulo: "Contas fixas", rota: "/contas-fixas", dica: "Edite em Contas fixas" },
  COMPRA_PARCELADA: {
    rotulo: "Cancelar a compra parcelada",
    rota: null,
    dica: "Para desfazer, use Cancelar compra parcelada",
  },
  CARTAO: { rotulo: "Cartao", rota: "/cartao", dica: "Edite em Cartao" },
  CONTA_RECEBER: {
    rotulo: "Contas a receber",
    rota: "/contas-receber",
    dica: "Edite em Contas a receber",
  },
  TRANSFERENCIA: { rotulo: "Transferencia", rota: null, dica: "Edite em Transferencia" },
  CONCILIADO: null,
  NAO_MANUAL: null,
}

export function destinoDeEdicao(origem: OrigemVinculo): DestinoDeEdicao | null {
  return DESTINOS[origem]
}
