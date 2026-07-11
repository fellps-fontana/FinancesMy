const formatadorData = new Intl.DateTimeFormat("pt-BR")
const formatadorMesReferencia = new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" })

// Formata uma data DateOnly do backend (string "yyyy-MM-dd") no locale
// pt-BR do projeto. Parse manual dos componentes (em vez de `new Date(iso)`)
// evita o deslocamento de fuso que `Date` aplicaria ao interpretar a string
// como UTC meia-noite.
export function formatarData(dataIso: string): string {
  const [ano, mes, dia] = dataIso.split("-").map(Number)
  return formatadorData.format(new Date(ano, mes - 1, dia))
}

// Data de hoje no formato yyyy-MM-dd, valor padrao do campo "Data" nos
// formularios de compra e pagamento.
export function dataDeHoje(): string {
  const hoje = new Date()
  const ano = hoje.getFullYear()
  const mes = String(hoje.getMonth() + 1).padStart(2, "0")
  const dia = String(hoje.getDate()).padStart(2, "0")
  return `${ano}-${mes}-${dia}`
}

// Mes corrente no formato "yyyy-MM", mesmo que <input type="month"> usa.
export function mesAtualIso(): string {
  const hoje = new Date()
  const mes = String(hoje.getMonth() + 1).padStart(2, "0")
  return `${hoje.getFullYear()}-${mes}`
}

// Formata "yyyy-MM" como "Julho de 2026" (locale pt-BR do projeto).
export function formatarMesReferencia(mesIso: string): string {
  const [ano, mes] = mesIso.split("-").map(Number)
  const texto = formatadorMesReferencia.format(new Date(ano, mes - 1, 1))
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}
