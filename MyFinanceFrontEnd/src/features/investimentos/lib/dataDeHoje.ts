// Data de hoje no formato yyyy-MM-dd (mesmo formato do <input type="date"> e
// do DateOnly serializado pelo backend), valor padrao do campo "Data da
// compra" no formulario de novo ativo.
export function dataDeHoje(): string {
  return new Date().toISOString().slice(0, 10)
}
