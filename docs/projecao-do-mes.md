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

## Frontend (Dashboard) e widgets configuráveis

A lacuna de UI registrada abaixo foi fechada em duas leva: primeiro o
esqueleto do dashboard (card de saldo projetado com breakdown dos 4 termos
da fórmula, gráfico entradas vs saídas, indicador de limite de gasto),
depois um bloco de melhorias (ações rápidas, últimos lançamentos, widgets
configuráveis, navegação por categoria).

- **Ações rápidas**: 3 botões no topo do dashboard — "Novo Lançamento" e
  "Transferir" navegam pra `/lancamentos` com o segmented control
  pré-selecionado; "Pagar Conta" navega pra `/cartao` (fluxo de pagamento
  de fatura já existente ali, decisão confirmada com o usuário).
- **Widget "Últimos lançamentos"**: mostra os N lançamentos mais recentes
  do fluxo de caixa, reusando `LancamentoItem`. Garantia de contraste
  cumprida por design — o widget não tem nenhum estado `hover:` (itens não
  são clicáveis nesta leva).
- **Widgets configuráveis** (`SeletorWidgets.tsx` +
  `lib/preferenciaWidgets.ts`): cada card do dashboard — incluindo
  `CardSaldoProjetado`, o gráfico de entradas/saídas, o indicador de
  limite, e os dois widgets novos abaixo — pode ser ligado/desligado pelo
  usuário. Preferência persistida em `localStorage`, sem endpoint de
  backend. `CardSaldoProjetado` (regra crítica desta página, item 9) só
  ganhou a capacidade de ser mostrado/ocultado — a fórmula em si não foi
  tocada, confirmado pelo `style` em 2 rodadas via leitura de diff.
  - Novo widget de **investimentos**: reusa `GraficoConsolidadoAtivos`
    (módulo Investimentos).
  - Novo widget de **rendimentos**: reusa `GraficoRendimentosPorTipo`/
    `useRendimentosResumo` (módulo Investimentos, item 8.4 — dividendo
    manual + valorização automática derivada de `valor_atual`; nunca
    provento de fonte externa).
- **Navegação por categoria**: no indicador de limite de gasto do
  dashboard, clicar numa categoria navega pra
  `/limites-gasto?categoriaId={id}` já filtrada naquela categoria
  (consome o filtro client-side já suportado por
  `ComparativoLimiteGastoPage.tsx`).

## Lacunas conhecidas

- ~~Conta Fixa (item 6) não existe no codebase~~ — desatualizado: o módulo
  foi construído à parte depois (ver `docs/conta-fixa.md`) e seus
  `Lancamento`s já entram na fórmula pelo agregador genérico, sem mudança
  nesta página.
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

### Bloco J — melhorias de dashboard (TASK-141 a 145)

- **hanzo**: ações rápidas, widget de últimos lançamentos, widgets
  configuráveis (`SeletorWidgets.tsx`/`preferenciaWidgets.ts`) e navegação
  por categoria — 4 tasks, arquivos disjuntos entre TASK-143 e TASK-144
  rodadas em paralelo.
- **style**: 1ª rodada — PRECISA CORRIGIR: `useUltimosLancamentos.ts` usava
  queryKey inline (array literal) em vez de `dashboardKeys` centralizado
  em `query-keys.ts`, quebrando o padrão já seguido por `useProjecaoMes.ts`
  na mesma feature. hanzo corrigiu (chave `dashboardKeys.
  ultimosLancamentos(quantidade)` adicionada). 2ª rodada: APROVADO,
  confirmando que `CardSaldoProjetado` seguiu intocado na semântica e que
  nada mais do bloco foi reaberto.

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
- **Checkout local do `main` pode ficar bem atrás de `origin/main`**: numa
  sessão de verificação, o `main` local chegou a ficar 45 commits atrás —
  os Blocos E/F/M (Conta Fixa periodicidade, Investimentos, Rendimentos)
  já estavam mergeados em `origin/main` sem que o checkout local refletisse
  isso. Antes de decompor "o que falta" num módulo, sempre conferir contra
  `origin/main` (fetch + `git log origin/main..HEAD`), não só o `tasks.md`
  do checkout local.
- **Ambiente de dev local instável**: o Postgres local (`myfinances_dev`)
  já perdeu o banco entre sessões de teste manual mais de uma vez
  (possível reinício do serviço/container). Um `Database.EnsureCreated()`
  temporário chegou a ser deixado em `Program.cs` por uma sessão anterior
  pra contornar migrations não aplicadas — já removido, não faz parte de
  nenhum PR mergeado. Se o app voltar a reclamar de coluna/tabela
  faltando, o schema local está desalinhado das migrations reais; reiniciar
  o backend contra um banco vazio recria o schema certo via
  `EnsureCreated`, mas isso não substitui rodar `dotnet ef database update`
  de verdade num ambiente persistente.
