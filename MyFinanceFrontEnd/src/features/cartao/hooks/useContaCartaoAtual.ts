import { useCallback, useState } from "react"

const CHAVE_STORAGE = "myfinances:contaCartaoAtual"

export type ContaCartaoAtual = {
  id: string
  nome: string
}

function lerDoStorage(): ContaCartaoAtual | null {
  const bruto = localStorage.getItem(CHAVE_STORAGE)
  if (bruto === null) {
    return null
  }

  try {
    return JSON.parse(bruto) as ContaCartaoAtual
  } catch {
    return null
  }
}

/**
 * Guarda qual conta CARTAO esta ativa nesta pagina.
 *
 * GAP CONHECIDO (verificado em ContasController.ListarContas): GET /api/contas
 * so aceita `tipo=investimento` (ou sem filtro, que tambem cai no default de
 * investimento) - qualquer outro valor, incluindo "cartao", devolve 400. Nao
 * existe tambem GET /api/contas/{id} para buscar uma conta especifica por id.
 * Ou seja, hoje nao ha como listar ou redescobrir a conta CARTAO ja
 * cadastrada no backend a partir do front.
 *
 * Workaround preservado da implementacao isolada de referencia (branch
 * cartao-credito): a conta criada (id + nome) fica persistida no
 * localStorage deste navegador assim que a criacao tem sucesso. Se o
 * localStorage for limpo, ou o acesso vier de outro navegador/dispositivo, a
 * pagina volta a pedir a criacao sem saber que a conta ja existe no banco -
 * risco de criar uma segunda conta CARTAO duplicada. Quando
 * GET /api/contas?tipo=cartao (ou GET /api/contas/{id}) existir, substituir
 * por uma query real (React Query) e remover esta persistencia local.
 */
export function useContaCartaoAtual(): {
  contaCartaoAtual: ContaCartaoAtual | null
  setContaCartaoAtual: (conta: ContaCartaoAtual) => void
} {
  const [contaCartaoAtual, setContaCartaoAtualState] = useState<ContaCartaoAtual | null>(lerDoStorage)

  const setContaCartaoAtual = useCallback((conta: ContaCartaoAtual) => {
    localStorage.setItem(CHAVE_STORAGE, JSON.stringify(conta))
    setContaCartaoAtualState(conta)
  }, [])

  return { contaCartaoAtual, setContaCartaoAtual }
}
