# Módulo: Projeção do Mês (dashboard)

## Visão geral

Fecha o item 9 da `regra-de-negocio.md`: o balanço real do mês corrente, não
uma estimativa futura. Combina 4 fontes que já existiam isoladas em outros
módulos — fluxo de caixa genérico (Lançamento Geral), contas a receber, e
fatura de cartão — numa única fórmula e num único endpoint de dashboard.

```
saldo_projetado = (total_recebido_no_mes + total_a_receber_esperado_no_mes)
                  - (total_pago_no_mes + total_a_pagar_no_mes)
```

## Regras de negócio implementadas

Item 9 da `regra-de-negocio.md`, ponto a ponto:

- **`total_recebido_no_mes`/`total_pago_no_mes`/`total_a_pagar_no_mes`** (genéricos):
  somam `Lancamento` do mês por `Tipo`/`Status` (Credit+Pago, Debit+Pago,
  Debit+Pendente), excluindo lançamentos classificados como `Transferencia`
  via `ClassificacaoLancamentoService.Classificar` — cobre transferência comum
  entre contas do usuário E empréstimo (item 13, perna única), ambos fora da
  projeção por decisão confirmada com o usuário em 2026-07-20 (dinheiro
  emprestado vira "ativo" via `ContaReceber`, só conta quando volta).
- **`total_a_receber_esperado_no_mes`**: já existia (`ContaReceberService`,
  módulo Contas a Receber) — reaproveitado sem alteração.
- **Cartão de crédito como UMA conta a pagar** (item 12): as compras
  individuais nunca entram no fluxo de caixa genérico (`FaturaId != null` já
  filtrado no repository). A fatura do mês (`DataVencimento` no ano/mês
  consultado, decisão confirmada em 2026-07-20) entra **fracionada** — nunca
  binária por `Status` — somando a parte já paga (`FaturaSaldoCalculator.ValorPago`)
  em `total_pago_no_mes` e o saldo pendente em `total_a_pagar_no_mes`. Múltiplos
  cartões no mesmo mês somam num único total (sem breakdown por cartão),
  decisão confirmada em 2026-07-20.
- **Sem double-counting**: cada fonte cobre uma fatia distinta e mutuamente
  exclusiva (compra de cartão só na fatura; pagamento de fatura só via
  `FaturaProjecaoService`, nunca pelo fluxo de caixa genérico, porque o
  lançamento de pagamento tem `TransferenciaId` e é excluído de lá) —
  verificado explicitamente pelo `style` na revisão final da cadeia.

## Modelo de dados e endpoints

Nenhuma tabela nova. Dois métodos novos de repository (`ListarParaFluxoCaixaDoMes`
em `ILancamentoRepository`, `ListarFaturasCartaoPorVencimentoNoMes` em
`IFaturaRepository`) e três services novos/estendidos:

- `FluxoCaixaService`: +3 métodos de agregação mensal (`CalcularTotalRecebidoNoMes`,
  `CalcularTotalPagoNoMes`, `CalcularTotalAPagarNoMes`), reusando um helper
  privado comum (`SomarLancamentosDoMes`).
- `FaturaProjecaoService` (novo): `CalcularProjecaoCartaoDoMes` — fraciona
  pago/pendente por fatura via `FaturaSaldoCalculator`.
- `ProjecaoMesService` (novo, master): compõe os 3 services acima e aplica a
  fórmula final.

Endpoint: `GET /api/dashboard/projecao-mes?ano=&mes=` (`DashboardController`),
retornando os 7 campos da fórmula (`Ano`, `Mes`, `TotalRecebidoNoMes`,
`TotalAReceberEsperadoNoMes`, `TotalPagoNoMes`, `TotalAPagarNoMes`,
`SaldoProjetado`).

## Lacunas conhecidas

- **Sem UI**: `MyFinanceFrontEnd/src/features/dashboard/` só tem `.gitkeep`.
  Não decompôs tasks de frontend por falta de definição de quais cards/gráfico
  exibir — pendência registrada, não bloqueia o backend.
- **Conta Fixa (item 6) não existe no codebase** (nem `Domain`, nem migration
  da tabela, só a FK morta `conta_fixa_id` em `Lancamento`). Não bloqueia a
  fórmula (quando existir, vai gerar `Lancamento` comuns que o agregador
  genérico já soma), mas nenhuma conta fixa aparece na projeção v1 até esse
  módulo ser construído à parte.
- Dois testes de "empréstimo" em `FluxoCaixaServiceTests.cs` descrevem
  modelagem que diverge do item 13 real (recebimento deveria vincular por
  `ContaReceberId`, não `TransferenciaId`; saída de empréstimo é sempre
  Pago, nunca Pendente) — achado pelo `style`, funcionalmente inofensivo
  (mesmo caminho de exclusão de Transferência comum cobre o caso), mas
  documentação de teste enganosa. Não corrigido ainda.

## O que cada agent entregou

- **killua**: mapeou o que faltava pra montar o `saldo_projetado` completo
  (nenhum dos 3 termos de despesa tinha agregador mensal ainda — só listagem),
  decompôs em 16 tasks (TASK-051 a TASK-066) com ciclo TDD explícito nas 3
  peças críticas, e levantou 6 dúvidas de regra de negócio antes de travar
  qualquer teste.
- **levi**: implementou os 2 métodos de repository, os 3 services e o
  endpoint. Em paralelo, encontrou e corrigiu um bug de compilação
  pré-existente em `main` (`TransferenciaResponse.ContaDestinoId` não
  acompanhou `Transferencia.ContaDestinoId` virar nullable num commit anterior
  — CS0266) — não fazia parte do escopo da task, mas era bloqueante.
- **mike**: TDD completo das 2 regras críticas (18 testes de agregação mensal,
  8 de fatura fracionada, 6 da fórmula master — 32 testes novos no total),
  RED→GREEN em cada uma.
- **style**: 2 rodadas na agregação mensal (duplicação real entre os 3
  métodos, resolvida com extração de helper privado), 2 rodadas na fatura
  fracionada (achado mais sério do módulo: o código confiava em `fatura.Status`
  pra decidir o cálculo, mascarado por uma invariante que vivia em 3 arquivos
  externos — corrigido pra sempre usar o saldo calculado), e aprovação de
  primeira na fórmula master e no endpoint final, com verificação explícita
  de ausência de double-counting entre as fontes.

## Notas operacionais

- **Desvio de escopo recorrente**: dois executores (`mike` na TASK-039 e
  `levi` na TASK-051) tentaram "corrigir" a nullability de
  `TransferenciaResponse.ContaDestinoId` — a primeira vez foi revertida por
  engano (parecia regressão), até `dotnet build` confirmar que era bug real
  de `main`. Lição: nunca reverter uma mudança de um executor sem rodar
  build/teste primeiro, mesmo quando parece óbvio que é desvio.
- **`tasks.md` estava desatualizado**: a fila anterior (Lançamento Geral,
  TASK-039 a TASK-050) já tinha sido implementada, testada, revisada e
  mergeada em `main` via PR #28 antes desta sessão começar — os `STATUS`
  ainda diziam `PENDENTE`. Corrigido no início desta entrega.
