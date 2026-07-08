import { useState } from 'react';
import type { FormEvent, ReactElement } from 'react';
import styles from './LancarCompraForm.module.css';

export interface NovaCompraInput {
  descricao: string;
  valor: number;
  data: string;
  categoriaId: string | null;
}

interface LancarCompraFormProps {
  onSubmit: (input: NovaCompraInput) => void;
  onFechar: () => void;
  enviando: boolean;
  mensagemErro: string | null;
}

/**
 * Formulario de lancamento de compra no cartao (regra de negocio item 12).
 * Puramente apresentacao: guarda so o estado transiente dos campos; a
 * chamada de API, o estado de envio e o erro vem do container
 * (ContaCartaoPage) via props — mesmo padrao de CriarContaCartaoForm.
 *
 * LIMITACAO CONHECIDA (documentada para o Kira): o backend ainda nao expoe
 * endpoint de listagem de categorias do usuario (regra de negocio item 7).
 * Por isso o campo Categoria fica desabilitado, com nota explicita, e toda
 * compra lancada por aqui sai sem categoria (`categoriaId: null`) ate essa
 * integracao existir. Nao ha categoria fake inventada nesta tela.
 */
export function LancarCompraForm({
  onSubmit,
  onFechar,
  enviando,
  mensagemErro,
}: LancarCompraFormProps): ReactElement {
  const [descricao, setDescricao] = useState('');
  const [valor, setValor] = useState('');
  const [data, setData] = useState('');

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    onSubmit({
      descricao,
      valor: Number(valor),
      data,
      categoriaId: null,
    });
  }

  return (
    <div className={styles.overlay} role="presentation" onClick={onFechar}>
      <form
        className={styles.form}
        onSubmit={handleSubmit}
        onClick={(evento) => evento.stopPropagation()}
      >
        <div className={styles.cabecalho}>
          <h2 className={styles.titulo}>Lancar compra</h2>
          <button
            className={styles.fechar}
            type="button"
            onClick={onFechar}
            aria-label="Fechar formulario"
          >
            ×
          </button>
        </div>

        <label className={styles.campo}>
          <span className={styles.label}>Descricao</span>
          <input
            className={styles.input}
            type="text"
            value={descricao}
            onChange={(evento) => setDescricao(evento.target.value)}
            placeholder="Ex: Restaurante"
            required
          />
        </label>

        <label className={styles.campo}>
          <span className={styles.label}>Valor</span>
          <input
            className={styles.input}
            type="number"
            min={0.01}
            step={0.01}
            value={valor}
            onChange={(evento) => setValor(evento.target.value)}
            placeholder="0,00"
            required
          />
        </label>

        <label className={styles.campo}>
          <span className={styles.label}>Data da compra</span>
          <input
            className={styles.input}
            type="date"
            value={data}
            onChange={(evento) => setData(evento.target.value)}
            required
          />
        </label>

        <label className={styles.campo}>
          <span className={styles.label}>Categoria</span>
          <select className={styles.input} disabled defaultValue="">
            <option value="">Sem categorias cadastradas</option>
          </select>
          <span className={styles.hint}>
            Ainda nao ha endpoint de categorias no backend. A compra sera lancada sem categoria
            ate essa integracao existir.
          </span>
        </label>

        {mensagemErro !== null && <span className={styles.erro}>{mensagemErro}</span>}

        <button className={styles.botao} type="submit" disabled={enviando}>
          {enviando ? 'Lancando...' : 'Lancar compra'}
        </button>
      </form>
    </div>
  );
}
