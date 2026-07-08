import type { ReactElement } from 'react';
import { Card } from '../../../shared/components/Card';
import { formatarMoeda } from '../../../shared/format/moeda';
import styles from './SaldoCartaoCard.module.css';

interface SaldoCartaoCardProps {
  saldo: number | undefined;
  carregando: boolean;
  erro: boolean;
}

/**
 * Bloco de saldo do cartao (regra de negocio item 12: compras - pagamentos -
 * estornos, calculado pelo backend — este componente so exibe). Nao mostra
 * status/vencimento de fatura (isso e "ver fatura", fora do escopo desta
 * task) nem limite/barra de uso (dado que nao existe no backend).
 */
export function SaldoCartaoCard({ saldo, carregando, erro }: SaldoCartaoCardProps): ReactElement {
  return (
    <Card variant="surface" className={styles.card}>
      <span className={styles.label}>Saldo do cartao</span>
      {carregando && <span className={styles.estado}>Calculando...</span>}
      {erro && <span className={styles.estadoErro}>Nao foi possivel calcular o saldo.</span>}
      {!carregando && !erro && saldo !== undefined && (
        <span className={styles.valor}>{formatarMoeda(saldo)}</span>
      )}
    </Card>
  );
}
