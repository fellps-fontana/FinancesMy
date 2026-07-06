import type { ReactElement } from 'react';
import { Badge } from '../../shared/components/Badge';
import type { BadgeTone } from '../../shared/components/Badge';
import { Card } from '../../shared/components/Card';
import { formatarMoeda } from '../../shared/format/moeda';
import type { FaturaResponse, StatusFatura } from './api';
import { useFaturas } from './hooks/useFaturas';
import styles from './FaturaPage.module.css';

/**
 * Mapeia o Status da fatura (regra de negocio item 12) para o tom de badge
 * da identidade visual (identidade-visual.md: "Status (badges): pago ->
 * positivo; pendente -> alerta; manual -> neutro"). ABERTA ainda esta
 * acumulando compras (equivalente a pendente/atencao); FECHADA ja tem valor
 * definitivo mas ainda nao foi paga (neutro, aguardando); PAGA e o estado
 * final positivo.
 */
const BADGE_POR_STATUS: Record<StatusFatura, { label: string; tone: BadgeTone }> = {
  ABERTA: { label: 'Aberta', tone: 'alerta' },
  FECHADA: { label: 'Fechada', tone: 'neutro' },
  PAGA: { label: 'Paga', tone: 'positivo' },
};

/**
 * Formata uma data DateOnly do backend (string "yyyy-MM-dd", ver
 * FaturaResponseDto.cs) no locale pt-BR do projeto. Parse manual dos
 * componentes (em vez de `new Date(iso)`) evita o deslocamento de fuso que
 * `Date` aplicaria ao interpretar a string como UTC meia-noite.
 */
function formatarData(dataIso: string): string {
  const [ano, mes, dia] = dataIso.split('-').map(Number);
  return new Intl.DateTimeFormat('pt-BR').format(new Date(ano, mes - 1, dia));
}

interface FaturaPageProps {
  contaId: string;
}

/**
 * Secao de faturas do cartao (regra de negocio item 12: "Fatura" e "Duas
 * visoes"). Lista as faturas da conta com os totais ja calculados pelo
 * backend (FaturaSaldoCalculator) — este componente so exibe.
 *
 * LIMITACAO CONHECIDA (documentada para o Kira): nao existe endpoint para
 * listar as compras individuais de cada fatura (ver api.ts/listarFaturas).
 * Por isso cada fatura mostra so o agregado (total/pago/pendente), sem a
 * lista de compras que o mockup "05 Cartao de credito" sugere dentro da
 * fatura selecionada. Quando esse endpoint existir, adicionar o detalhe
 * expansivel por fatura.
 */
export function FaturaPage({ contaId }: FaturaPageProps): ReactElement {
  const faturasQuery = useFaturas(contaId);

  return (
    <section className={styles.secao}>
      <h2 className={styles.titulo}>Faturas</h2>

      {faturasQuery.isLoading && <span className={styles.estado}>Carregando faturas...</span>}

      {faturasQuery.isError && (
        <span className={styles.estadoErro}>Nao foi possivel carregar as faturas.</span>
      )}

      {faturasQuery.isSuccess && faturasQuery.data.length === 0 && (
        <span className={styles.estado}>Nenhuma fatura ainda.</span>
      )}

      {faturasQuery.isSuccess && faturasQuery.data.length > 0 && (
        <ul className={styles.lista}>
          {faturasQuery.data.map((fatura) => (
            <FaturaItem key={fatura.id} fatura={fatura} />
          ))}
        </ul>
      )}

      <p className={styles.hintCompras}>Compras individuais nao disponiveis nesta versao.</p>
    </section>
  );
}

function FaturaItem({ fatura }: { fatura: FaturaResponse }): ReactElement {
  const badge = BADGE_POR_STATUS[fatura.status];

  return (
    <li>
      <Card variant="surface" className={styles.item}>
        <div className={styles.cabecalho}>
          <span className={styles.periodo}>
            {formatarData(fatura.dataFechamento)} - {formatarData(fatura.dataVencimento)}
          </span>
          <Badge label={badge.label} tone={badge.tone} />
        </div>

        <span className={styles.valorTotal}>{formatarMoeda(fatura.valorTotal)}</span>
        <span className={styles.vencimento}>Vence em {formatarData(fatura.dataVencimento)}</span>

        <div className={styles.detalhes}>
          <span className={styles.detalhe}>
            Pago <strong className={styles.valorPositivo}>{formatarMoeda(fatura.valorPago)}</strong>
          </span>
          <span className={styles.detalhe}>
            Pendente{' '}
            <strong className={styles.valorAlerta}>{formatarMoeda(fatura.valorPendente)}</strong>
          </span>
        </div>
      </Card>
    </li>
  );
}
