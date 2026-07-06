import type { ReactElement, ReactNode } from 'react';
import styles from './Card.module.css';

export type CardVariant = 'surface' | 'accent';

interface CardProps {
  children: ReactNode;
  variant?: CardVariant;
  className?: string;
}

/**
 * Superficie base (card) da identidade visual. `variant="surface"` para
 * conteudo comum (bg-surface); `variant="accent"` para destaque roxo
 * (ex: cartao fisico no mockup, bg accent-deep).
 */
export function Card({ children, variant = 'surface', className }: CardProps): ReactElement {
  const classes = [styles.card, styles[variant], className].filter(Boolean).join(' ');
  return <div className={classes}>{children}</div>;
}
