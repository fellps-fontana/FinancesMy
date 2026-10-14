// Chave centralizada para a lista combinada (banco + investimento) desta
// tela. Feature ainda tem so uma query - nao ha invalidacao cruzada hoje,
// mas o padrao (ver stack.md) e o mesmo ja usado por investimentosKeys/
// contasReceberKeys, para nao espalhar string magica quando outra
// query/mutation entrar aqui (ex: criar conta).
export const contasKeys = {
  all: ["contas"] as const,
  lista: () => [...contasKeys.all, "lista"] as const,
  // Contas MANUAL (banco + investimento) disponiveis pra selecao de origem ou
  // destino em formularios. Sem parametro: uma unica chave cobre todos os usos.
  contasParaSelecao: () => [...contasKeys.all, "contasParaSelecao"] as const,
}
