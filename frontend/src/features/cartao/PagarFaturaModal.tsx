import { useState } from 'react';
import type { FormEvent, ReactElement } from 'react';
import { formatarMoeda } from '../../shared/format/moeda';
import styles from './PagarFaturaModal.module.css';

export interface PagarFaturaInput {
  contaOrigemId: string;
  data: string;
  valor: number;
}

interface PagarFaturaModalProps {
  valorPendente: number;
  onSubmit: (input: PagarFaturaInput) => void;
  onFechar: () => void;
  enviando: boolean;
  mensagemErro: string | null;
}

/** Data de hoje no formato yyyy-MM-dd, valor padrao do campo "Data do pagamento". */
function dataDeHoje(): string {
  const hoje = new Date();
  const ano = hoje.getFullYear();
  const mes = String(hoje.getMonth() + 1).padStart(2, '0');
  const dia = String(hoje.getDate()).padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
}

/**
 * Formulario de pagamento de fatura (regra de negocio item 12, "Pagamento x
 * fatura (revisado)"): o pagamento pode ser ANTECIPADO (fatura ainda ABERTA)
 * e PARCIAL — varios pagamentos ate quitar o saldo pendente. Por isso o
 * campo "Valor a pagar" vem pre-preenchido com o `valorPendente` mas fica
 * editavel para um valor MENOR (pagamento parcial); o `max` do input reflete
 * o teto (nao ha overpayment), mas a validacao de fato e do backend.
 *
 * Puramente apresentacao: guarda so o estado transiente dos campos; a
 * mutation, o estado de envio e o erro vem do container (FaturaPage) — mesmo
 * padrao de LancarCompraForm.
 *
 * LIMITACAO CONHECIDA (documentada para o Kira): nao existe endpoint para
 * listar contas BANCO existentes (nem para cria-las na UI, ver
 * CriarContaCartaoForm que so cria CARTAO). Ate esse endpoint existir, a
 * conta de origem do pagamento e informada como texto livre (o usuario
 * precisa saber o GUID de uma conta BANCO ja existente no banco de dados).
 */
export function PagarFaturaModal({
  valorPendente,
  onSubmit,
  onFechar,
  enviando,
  mensagemErro,
}: PagarFaturaModalProps): ReactElement {
  const [contaOrigemId, setContaOrigemId] = useState('');
  const [data, setData] = useState(dataDeHoje());
  const [valor, setValor] = useState(valorPendente.toString());

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    onSubmit({
      contaOrigemId,
      data,
      valor: Number(valor),
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
          <h2 className={styles.titulo}>Pagar fatura</h2>
          <button
            className={styles.fechar}
            type="button"
            onClick={onFechar}
            aria-label="Fechar formulario"
          >
            ×
          </button>
        </div>

        <span className={styles.saldoPendente}>
          Saldo pendente <strong>{formatarMoeda(valorPendente)}</strong>
        </span>

        <label className={styles.campo}>
          <span className={styles.label}>ID da conta de origem</span>
          <input
            className={styles.input}
            type="text"
            value={contaOrigemId}
            onChange={(evento) => setContaOrigemId(evento.target.value)}
            placeholder="GUID da conta bancaria"
            required
          />
          <span className={styles.hint}>
            Cole o ID da conta bancaria de origem. Ainda nao ha selecao visual de contas — depende
            de um endpoint de listagem de contas que nao existe no backend.
          </span>
        </label>

        <label className={styles.campo}>
          <span className={styles.label}>Data do pagamento</span>
          <input
            className={styles.input}
            type="date"
            value={data}
            onChange={(evento) => setData(evento.target.value)}
            required
          />
        </label>

        <label className={styles.campo}>
          <span className={styles.label}>Valor a pagar</span>
          <input
            className={styles.input}
            type="number"
            min={0.01}
            max={valorPendente}
            step={0.01}
            value={valor}
            onChange={(evento) => setValor(evento.target.value)}
            required
          />
          <span className={styles.hint}>
            Pode ser menor que o saldo pendente — pagamento parcial e permitido, e a fatura
            continua com o restante em aberto ate um novo pagamento.
          </span>
        </label>

        {mensagemErro !== null && <span className={styles.erro}>{mensagemErro}</span>}

        <button className={styles.botao} type="submit" disabled={enviando}>
          {enviando ? 'Pagando...' : 'Pagar fatura'}
        </button>
      </form>
    </div>
  );
}
