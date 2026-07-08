import type { ReactElement } from 'react';
import styles from './Badge.module.css';

/**
 * Tom semantico do badge. O mapeamento de um status/origem de dominio
 * (ex: fatura "Paga" -> tone "positivo") e responsabilidade de quem chama
 * este componente (feature ou funcao util), nunca deste componente.
 */
export type BadgeTone = 'positivo' | 'negativo' | 'alerta' | 'accent' | 'neutro';

interface BadgeProps {
  label: string;
  tone: BadgeTone;
}

export function Badge({ label, tone }: BadgeProps): ReactElement {
  return <span className={`${styles.badge} ${styles[tone]}`}>{label}</span>;
}
