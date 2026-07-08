const formatadorReal = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
})

// Formatacao de moeda fica aqui (funcao pura e testavel) em vez de inline no
// JSX - ver clean-code.md "Organizacao (React)".
export function formatarMoeda(valor: number): string {
  return formatadorReal.format(valor)
}
