// Nivel de alerta visual do limite de gasto por categoria (regra-de-negocio.md
// item 14: "Efeito: SOMENTE alerta visual" - nenhum bloqueio de lancamento).
// `estourado` e `percentualUtilizado` chegam prontos do backend
// (GastoVsLimiteResponse, gasto_realizado_no_mes / valor_limite). O threshold
// de 80% abaixo NAO e regra de negocio - e decisao isolada de UX pra dar um
// aviso preventivo antes do estouro, documentada aqui pra poder ajustar sem
// tocar em contrato de API.
export type NivelAlertaLimite = "ok" | "perto" | "estourado"

const LIMIAR_PERTO_DO_LIMITE = 0.8

export function decidirNivelAlerta(
  percentualUtilizado: number,
  estourado: boolean,
): NivelAlertaLimite {
  if (estourado) {
    return "estourado"
  }

  if (percentualUtilizado >= LIMIAR_PERTO_DO_LIMITE) {
    return "perto"
  }

  return "ok"
}
