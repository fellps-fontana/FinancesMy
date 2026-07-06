import type { ReactElement } from 'react';
import { extrairMensagemErroApi } from './api';
import { CartaoVisual } from './components/CartaoVisual';
import { CriarContaCartaoForm } from './components/CriarContaCartaoForm';
import type { NovaContaCartaoInput } from './components/CriarContaCartaoForm';
import { SaldoCartaoCard } from './components/SaldoCartaoCard';
import { useCriarContaCartao, useSaldoCartao } from './hooks/useContaCartao';
import { useContaCartaoAtual } from './hooks/useContaCartaoAtual';
import styles from './ContaCartaoPage.module.css';

/**
 * Pagina da conta CARTAO. Container: nao calcula nada de dominio (saldo vem
 * pronto do backend), so orquestra estado de servidor (React Query) e estado
 * de UI (qual conta cartao esta ativa) e decide qual apresentacao mostrar.
 *
 * Ver hooks/useContaCartaoAtual.ts para a limitacao conhecida sobre a
 * ausencia de endpoint de listagem/consulta de contas.
 */
export function ContaCartaoPage(): ReactElement {
  const { contaCartaoAtual, setContaCartaoAtual } = useContaCartaoAtual();
  const criarContaCartao = useCriarContaCartao();
  const saldoQuery = useSaldoCartao(contaCartaoAtual?.id ?? null);

  function handleCriarConta(input: NovaContaCartaoInput): void {
    criarContaCartao.mutate(
      {
        nome: input.nome,
        tipo: 'CARTAO',
        diaFechamento: input.diaFechamento,
        diaVencimento: input.diaVencimento,
      },
      {
        onSuccess: (conta) => setContaCartaoAtual({ id: conta.id, nome: conta.nome }),
      },
    );
  }

  return (
    <div className={styles.pagina}>
      <h1 className={styles.titulo}>Cartao de credito</h1>

      {contaCartaoAtual === null ? (
        <CriarContaCartaoForm
          onSubmit={handleCriarConta}
          enviando={criarContaCartao.isPending}
          mensagemErro={
            criarContaCartao.isError ? extrairMensagemErroApi(criarContaCartao.error) : null
          }
        />
      ) : (
        <div className={styles.conteudo}>
          <CartaoVisual nome={contaCartaoAtual.nome} />
          <SaldoCartaoCard
            saldo={saldoQuery.data?.saldo}
            carregando={saldoQuery.isLoading}
            erro={saldoQuery.isError}
          />
        </div>
      )}
    </div>
  );
}
