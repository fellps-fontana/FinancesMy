import type { AtivosResumoResponse, ResumoPorTipo, TipoAtivoStorage } from "@/features/investimentos/types"

// Busca o resumo de um tipo especifico dentro de porTipo
// (regra-de-negocio.md item 8: resumo agrupado por RENDA_FIXA/RENDA_VARIAVEL,
// ver Services/AtivoService.cs ObterResumo). Retorna zerado quando a carteira
// nao tem nenhum ativo daquele tipo - os cards de resumo continuam visiveis
// com R$ 0,00 / 0%, em vez de sumir (mockup "11 Investimentos" sempre mostra
// os dois cards lado a lado).
export function obterResumoPorTipo(
  resumo: AtivosResumoResponse | undefined,
  tipo: TipoAtivoStorage,
): ResumoPorTipo {
  const encontrado = resumo?.porTipo.find((item) => item.tipo === tipo)
  return encontrado ?? { tipo, valorAtual: 0, percentualDaCarteira: 0 }
}
