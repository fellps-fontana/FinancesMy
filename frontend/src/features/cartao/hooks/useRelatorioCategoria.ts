import { useQuery } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import { obterRelatorioCategoria } from '../api';
import type { RelatorioCategoriaItem, RelatorioCategoriaResponse } from '../api';

export const CHAVE_RELATORIO_CATEGORIA = 'relatorio-categoria';

/**
 * Visao categorica/competencia do cartao (regra de negocio item 12, "Duas
 * visoes"). `mes` no formato "yyyy-MM" (o mesmo que <input type="month">
 * produz nativamente). `contaId` opcional escopa o relatorio a uma unica
 * conta CARTAO.
 */
export function useRelatorioCategoria(
  mes: string,
  contaId?: string,
): UseQueryResult<RelatorioCategoriaResponse> {
  return useQuery({
    queryKey: [CHAVE_RELATORIO_CATEGORIA, mes, contaId ?? null],
    queryFn: () => obterRelatorioCategoria(mes, contaId),
  });
}

export interface ItemRelatorioCategoriaOrdenado extends RelatorioCategoriaItem {
  /** "Sem categoria" quando `nomeCategoria` vem null (regra de negocio item 7). */
  nomeExibicao: string;
  /** Fracao (0-1) do item sobre o total geral do periodo. */
  percentual: number;
}

/**
 * Soma o total gasto no periodo (todas as categorias, regime de COMPETENCIA
 * — regra de negocio item 12). Funcao pura e testavel, fora do componente
 * (clean-code.md: "logica de calculo nao vive no componente").
 */
export function calcularTotalGeral(itens: RelatorioCategoriaItem[]): number {
  return itens.reduce((soma, item) => soma + item.total, 0);
}

/**
 * Ordena os itens do relatorio por total desc (maior gasto primeiro) e
 * calcula o percentual de cada um sobre o total geral.
 *
 * DECISAO DE UX: o backend ordena por `nomeCategoria` asc
 * (RelatorioCategoriaService.cs: `.OrderBy(i => i.NomeCategoria)`), nao por
 * total. Reordenamos aqui no front, mesma decisao ja tomada em
 * hooks/useFaturas.ts para o campo que o backend nao ordena da forma que a
 * tela precisa.
 *
 * O item com `categoriaId = null` (compra sem categoria vinculada, regra de
 * negocio item 7) NAO e descartado — recebe o rotulo "Sem categoria" e
 * participa da ordenacao e do total normalmente.
 */
export function ordenarItensRelatorio(
  itens: RelatorioCategoriaItem[],
): ItemRelatorioCategoriaOrdenado[] {
  const totalGeral = calcularTotalGeral(itens);

  return [...itens]
    .sort((a, b) => b.total - a.total)
    .map((item) => ({
      ...item,
      nomeExibicao: item.nomeCategoria ?? 'Sem categoria',
      percentual: totalGeral > 0 ? item.total / totalGeral : 0,
    }));
}
