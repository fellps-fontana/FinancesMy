// Projecao do mes (regra-de-negocio.md item 9): NAO e estimativa futura, e o
// balanco real do mes corrente. Os 5 valores numericos vem prontos do
// backend (ProjecaoMesResponse) - o front so exibe, nunca recalcula
// saldoProjetado nem nenhum dos totais.
export type ProjecaoMesResponse = {
  ano: number
  mes: number
  totalRecebidoNoMes: number
  totalAReceberEsperadoNoMes: number
  totalPagoNoMes: number
  totalAPagarNoMes: number
  saldoProjetado: number
}
