import { useCallback, useState } from 'react';

const CHAVE_STORAGE = 'myfinances:contaCartaoAtual';

export interface ContaCartaoAtual {
  id: string;
  nome: string;
}

function lerDoStorage(): ContaCartaoAtual | null {
  const bruto = localStorage.getItem(CHAVE_STORAGE);
  if (bruto === null) {
    return null;
  }
  try {
    return JSON.parse(bruto) as ContaCartaoAtual;
  } catch {
    return null;
  }
}

/**
 * Guarda qual conta CARTAO esta ativa nesta pagina.
 *
 * LIMITACAO CONHECIDA (documentada para o Kira): o backend ainda nao expoe
 * endpoint de listagem de contas (GET /api/contas) nem de consulta por id
 * (GET /api/contas/{id}) — so existem POST /api/contas e
 * GET /api/contas/{id}/saldo. Por isso nao ha como, hoje, descobrir se ja
 * existe uma conta CARTAO cadastrada nem recuperar seu nome a partir do id.
 * Como solucao temporaria, a conta criada (id + nome) fica persistida no
 * localStorage deste navegador assim que a criacao tem sucesso. Se o
 * localStorage for limpo, ou o acesso vier de outro navegador/dispositivo,
 * a pagina volta a pedir a criacao — sem saber que a conta ja existe no
 * banco, o que pode levar a criacao de uma segunda conta CARTAO duplicada.
 * Quando existir endpoint de listagem/consulta, substituir por uma query
 * real (React Query) e remover esta persistencia local.
 */
export function useContaCartaoAtual(): {
  contaCartaoAtual: ContaCartaoAtual | null;
  setContaCartaoAtual: (conta: ContaCartaoAtual) => void;
} {
  const [contaCartaoAtual, setContaCartaoAtualState] = useState<ContaCartaoAtual | null>(
    lerDoStorage,
  );

  const setContaCartaoAtual = useCallback((conta: ContaCartaoAtual) => {
    localStorage.setItem(CHAVE_STORAGE, JSON.stringify(conta));
    setContaCartaoAtualState(conta);
  }, []);

  return { contaCartaoAtual, setContaCartaoAtual };
}
