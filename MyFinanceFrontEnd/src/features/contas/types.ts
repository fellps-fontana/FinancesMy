// Nomes de campo e valores de enum iguais aos DTOs do backend
// (DTOs/Conta/ContaResponse.cs, Domain/TipoConta.cs, Domain/SubtipoConta.cs,
// Domain/OrigemConta.cs). Program.cs registra JsonStringEnumConverter global
// sem naming policy, entao todo enum chega como o NOME em PascalCase (mesmo
// padrao ja documentado em features/investimentos/types.ts para TipoAtivo).
export type TipoConta = "Banco" | "Cartao" | "Investimento"

// Subtipo so existe para Conta tipo Banco (regra-de-negocio.md item 10);
// Conta tipo Investimento e Conta tipo Cartao chegam com Subtipo = null.
export type SubtipoConta = "Corrente" | "Poupanca" | "DinheiroFisico"

// v1 opera SO com MANUAL (regra-de-negocio.md item 1) - OpenFinance fica
// documentado no contrato para quando a integracao entrar em v2, mas nao
// deve aparecer em dado real na v1.
export type OrigemConta = "Manual" | "OpenFinance"

// Contrato completo de ContaResponse (TASK-127): alem dos campos que ja
// existiam (tipo, origem, saldo), agora expoe subtipo/icone/cor para
// personalizacao visual da conta (regra-de-negocio.md item 10).
export type ContaResponse = {
  id: string
  nome: string
  tipo: TipoConta
  subtipo: SubtipoConta | null
  icone: string | null
  cor: string | null
  origem: OrigemConta
  saldo: number
  saldoManual: number | null
  ativa: boolean
  diaFechamento: number | null
  diaVencimento: number | null
  pierreAccountId: string | null
}

// Selecao do dropdown "Tipo de conta" no formulario de criacao
// (components/FormNovaConta.tsx, TASK-129). So Conta tipo Banco tem Subtipo
// (regra-de-negocio.md item 10) - por isso o usuario ve 4 opcoes na tela,
// mas o mapeamento pra (tipo, subtipo) mora em lib/obterTipoESubtipoConta.ts,
// nao aqui (este arquivo so guarda o contrato/tipo).
export type TipoContaFormulario = "CONTA_CORRENTE" | "POUPANCA" | "DINHEIRO_FISICO" | "INVESTIMENTO"

// Contrato de POST /api/contas (DTOs/Conta/CriarContaRequest.cs, TASK-127).
// subtipo/icone/cor sao opcionais no back - so enviados quando o formulario
// de fato os define (Banco tem subtipo; Investimento nao, ver item 10 acima).
// Cartao fica fora: o formulario de nova conta desta tela (mockup 03 Contas)
// so oferece Corrente/Poupanca/DinheiroFisico/Investimento - cartao tem fluxo
// proprio em /cartao.
export type CriarContaRequest = {
  nome: string
  tipo: TipoConta
  subtipo?: SubtipoConta
  icone?: string
  cor?: string
  saldoManual?: number
}

// Contrato de PATCH /api/contas/{id}/saldo (Controllers/ContasController.cs,
// AtualizarSaldo). Edicao continua de saldo_manual - regra-de-negocio.md
// item 10: conta manual (banco ou investimento) tem saldo definido pelo
// usuario, nao so no cadastro inicial, mas a qualquer momento.
export type AtualizarSaldoRequest = {
  novoSaldo: number
}
