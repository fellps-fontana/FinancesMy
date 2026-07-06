const formatador = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

/**
 * Formata um valor numerico como moeda BRL (locale pt-BR do projeto).
 * Nenhum componente deve exibir valor monetario cru — sempre via esta funcao.
 */
export function formatarMoeda(valor: number): string {
  return formatador.format(valor);
}
