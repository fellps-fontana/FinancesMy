import { useState } from 'react';
import type { ReactElement } from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../../shared/components/Card';
import { formatarMoeda } from '../../shared/format/moeda';
import { useContaCartaoAtual } from './hooks/useContaCartaoAtual';
import {
  calcularTotalGeral,
  ordenarItensRelatorio,
  useRelatorioCategoria,
} from './hooks/useRelatorioCategoria';
import styles from './RelatorioCategoriaPage.module.css';

const formatadorMesReferencia = new Intl.DateTimeFormat('pt-BR', {
  month: 'long',
  year: 'numeric',
});

const formatadorPercentual = new Intl.NumberFormat('pt-BR', {
  style: 'percent',
  maximumFractionDigits: 0,
});

/** Mes corrente no formato "yyyy-MM", mesmo que <input type="month"> usa. */
function mesAtualIso(): string {
  const agora = new Date();
  const mes = String(agora.getMonth() + 1).padStart(2, '0');
  return `${agora.getFullYear()}-${mes}`;
}

/** Formata "yyyy-MM" como "Julho de 2026" (locale pt-BR do projeto). */
function formatarMesReferencia(mesIso: string): string {
  const [ano, mes] = mesIso.split('-').map(Number);
  const texto = formatadorMesReferencia.format(new Date(ano, mes - 1, 1));
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

/**
 * Relatorio por categoria (regra de negocio item 12, "Duas visoes" e
 * "Categorico / gasto por categoria"). Visao de COMPETENCIA: soma as compras
 * do cartao por categoria dentro do mes. Isto e DELIBERADAMENTE diferente da
 * visao de fluxo de caixa (que mostraria so o pagamento da fatura) — os dois
 * numeros nunca devem ser somados ou comparados lado a lado como se fossem a
 * mesma grandeza, por isso o subtitulo explicito abaixo.
 *
 * Container: nao calcula nada de dominio — ordenacao e percentual vem de
 * hooks/useRelatorioCategoria.ts (funcoes puras testaveis), este componente
 * so exibe.
 *
 * DECISAO DE UX: rota propria (/cartao/relatorio) em vez de secao dentro de
 * ContaCartaoPage. E um RELATORIO (visao de competencia, pode olhar o mes
 * inteiro independente da fatura aberta), nao uma ACAO sobre o cartao
 * (lancar compra, pagar fatura) — misturar as duas teria facilitado a leitura
 * errada de "isto soma com o saldo do cartao logo acima". ContaCartaoPage
 * ganhou um link de saida para esta rota (ver ContaCartaoPage.tsx).
 *
 * LIMITACAO CONHECIDA: sem pie/donut chart como no mockup "10 Relatorio por
 * Categoria" — a identidade visual so define os tokens `accent`/`accent-deep`/
 * `accent-soft` para roxo (identidade-visual.md), sem uma paleta de tons por
 * categoria. Introduzir hex cru por fatia violaria "nenhum valor cru de cor
 * fora dos tokens". A lista com barra de progresso (mesmo token `accent`)
 * comunica a mesma informacao (proporcao + valor) dentro da paleta definida.
 */
export function RelatorioCategoriaPage(): ReactElement {
  const [mes, setMes] = useState(mesAtualIso);
  const { contaCartaoAtual } = useContaCartaoAtual();
  const relatorioQuery = useRelatorioCategoria(mes, contaCartaoAtual?.id);

  const itens = relatorioQuery.data?.itens ?? [];
  const itensOrdenados = ordenarItensRelatorio(itens);
  const totalGeral = calcularTotalGeral(itens);

  return (
    <div className={styles.pagina}>
      <Link className={styles.voltar} to="/cartao">
        Voltar para o cartao
      </Link>

      <header className={styles.cabecalho}>
        <h1 className={styles.titulo}>Relatorio por categoria</h1>
        <p className={styles.subtitulo}>
          Quanto voce gastou em cada categoria neste mes. Visao de competencia: soma as compras
          do cartao por categoria, sem incluir o pagamento da fatura — esse numero e o do fluxo
          de caixa e nao entra aqui.
        </p>
      </header>

      <label className={styles.seletorMes}>
        <span className={styles.seletorMesLabel}>Mes de referencia</span>
        <input
          className={styles.inputMes}
          type="month"
          value={mes}
          onChange={(evento) => setMes(evento.target.value)}
        />
      </label>

      {relatorioQuery.isLoading && <span className={styles.estado}>Carregando relatorio...</span>}

      {relatorioQuery.isError && (
        <span className={styles.estadoErro}>Nao foi possivel carregar o relatorio.</span>
      )}

      {relatorioQuery.isSuccess && (
        <>
          <Card variant="surface" className={styles.totalCard}>
            <span className={styles.totalLabel}>Total gasto em {formatarMesReferencia(mes)}</span>
            <span className={styles.totalValor}>{formatarMoeda(totalGeral)}</span>
          </Card>

          {itensOrdenados.length === 0 ? (
            <span className={styles.estado}>Nenhuma compra registrada neste mes.</span>
          ) : (
            <ul className={styles.lista}>
              {itensOrdenados.map((item) => (
                <li key={item.categoriaId ?? 'sem-categoria'} className={styles.item}>
                  <div className={styles.itemCabecalho}>
                    <span className={styles.itemNome}>{item.nomeExibicao}</span>
                    <span className={styles.itemValor}>{formatarMoeda(item.total)}</span>
                  </div>
                  <div className={styles.barraTrilho}>
                    <div
                      className={styles.barraPreenchida}
                      style={{ width: `${(item.percentual * 100).toFixed(1)}%` }}
                    />
                  </div>
                  <span className={styles.itemPercentual}>
                    {formatadorPercentual.format(item.percentual)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  );
}
