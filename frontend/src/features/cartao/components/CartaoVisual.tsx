import type { ReactElement } from 'react';
import { Card } from '../../../shared/components/Card';
import styles from './CartaoVisual.module.css';

interface CartaoVisualProps {
  nome: string;
}

/**
 * Representacao visual do cartao fisico (mockup "05 Cartao de credito").
 * O numero mascarado e fixo/decorativo: o backend nao expoe (e nao deveria
 * expor) numero de cartao. Mesmo motivo para nao exibir nome do titular —
 * dado que nao existe no schema de Conta.
 */
export function CartaoVisual({ nome }: CartaoVisualProps): ReactElement {
  return (
    <Card variant="accent" className={styles.cartao}>
      <div className={styles.topo}>
        <span className={styles.nome}>{nome}</span>
        <IconeCartao />
      </div>
      <div className={styles.numero}>•••• •••• •••• ••••</div>
    </Card>
  );
}

function IconeCartao(): ReactElement {
  return (
    <svg
      width="22"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="2" y="5" width="20" height="14" rx="2" />
      <path d="M2 10h20" />
    </svg>
  );
}
