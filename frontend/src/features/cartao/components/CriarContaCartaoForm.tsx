import { useState } from 'react';
import type { FormEvent, ReactElement } from 'react';
import styles from './CriarContaCartaoForm.module.css';

export interface NovaContaCartaoInput {
  nome: string;
  diaFechamento: number;
  diaVencimento: number;
}

interface CriarContaCartaoFormProps {
  onSubmit: (input: NovaContaCartaoInput) => void;
  enviando: boolean;
  mensagemErro: string | null;
}

/**
 * Formulario de criacao da conta CARTAO. Puramente apresentacao: guarda so
 * o estado transiente dos campos (input controlado); a chamada de API, o
 * estado de carregamento e o erro vem do container (ContaCartaoPage) via
 * props.
 */
export function CriarContaCartaoForm({
  onSubmit,
  enviando,
  mensagemErro,
}: CriarContaCartaoFormProps): ReactElement {
  const [nome, setNome] = useState('');
  const [diaFechamento, setDiaFechamento] = useState('');
  const [diaVencimento, setDiaVencimento] = useState('');

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    onSubmit({
      nome,
      diaFechamento: Number(diaFechamento),
      diaVencimento: Number(diaVencimento),
    });
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <h2 className={styles.titulo}>Nova conta de cartao</h2>
      <span className={styles.hint}>
        Ainda nao ha um cartao cadastrado nesta pagina. Informe os dados abaixo para
        criar a conta.
      </span>

      <label className={styles.campo}>
        <span className={styles.label}>Nome</span>
        <input
          className={styles.input}
          type="text"
          value={nome}
          onChange={(evento) => setNome(evento.target.value)}
          placeholder="Ex: Nubank Ultravioleta"
          required
        />
      </label>

      <label className={styles.campo}>
        <span className={styles.label}>Dia de fechamento</span>
        <input
          className={styles.input}
          type="number"
          min={1}
          max={31}
          value={diaFechamento}
          onChange={(evento) => setDiaFechamento(evento.target.value)}
          required
        />
      </label>

      <label className={styles.campo}>
        <span className={styles.label}>Dia de vencimento</span>
        <input
          className={styles.input}
          type="number"
          min={1}
          max={31}
          value={diaVencimento}
          onChange={(evento) => setDiaVencimento(evento.target.value)}
          required
        />
      </label>

      {mensagemErro !== null && <span className={styles.erro}>{mensagemErro}</span>}

      <button className={styles.botao} type="submit" disabled={enviando}>
        {enviando ? 'Criando...' : 'Criar conta'}
      </button>
    </form>
  );
}
