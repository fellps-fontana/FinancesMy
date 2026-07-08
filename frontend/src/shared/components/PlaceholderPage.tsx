import type { ReactElement } from 'react';
import styles from './PlaceholderPage.module.css';

interface PlaceholderPageProps {
  title: string;
}

/**
 * Placeholder generico para rotas ainda nao implementadas. Cada feature
 * substitui este componente pela tela real quando for a sua vez.
 */
export function PlaceholderPage({ title }: PlaceholderPageProps): ReactElement {
  return (
    <div className={styles.page}>
      <h1 className={styles.title}>{title}</h1>
      <span className={styles.hint}>Em construcao.</span>
    </div>
  );
}
