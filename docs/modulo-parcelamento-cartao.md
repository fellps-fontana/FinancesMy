# Módulo: Parcelamento de Compra no Cartão

## Visão geral

Estende o módulo Cartão de Crédito (item 12 da `regra-de-negocio.md`) para
suportar compra parcelada. Decisão de modelagem central: uma compra parcelada
**não** é 1 lançamento pai com N parcelas dependentes — é N lançamentos
independentes, um por parcela, cada um caindo na fatura do seu próprio mês de
vencimento, exatamente como uma compra à vista. Um agrupador leve
(`CompraParcelada`) só serve pra UI mostrar "Notebook Dell 3/10" — fatura,
projeção do mês e relatório por categoria continuam somando cada lançamento
individualmente, sem ler esse agrupamento.

Essa demanda nasceu de um levantamento de módulos pendentes feito após
reconciliar o `main` local com `origin/main` (merge `e118ee6`) — ver
`docs/`-adjacent `.claude/demands.md`, DEMANDA-005.

## Regras de negócio implementadas

- **Modelo N-lançamentos, não 1+N-parcelas** (regra estava omissa, decidida
  com o usuário em 2026-07-12, registrada em `regra-de-negocio.md` item 12,
  subseção "Parcelamento"). A tabela `parcela` do `schema.dbml` original foi
  **removida** — ela guardava um campo `paga` por parcela que contradizia
  diretamente a regra já existente do item 12 ("pagamento fecha a fatura como
  um todo, nunca a compra/parcela específica"). No lugar entrou
  `compra_parcelada`, tabela de metadado sem estado de pagamento próprio,
  mesmo padrão já usado por `transferencia` (item 3).
- **Resolução de fatura por ciclo, não por soma de meses corridos**: cada
  parcela reaproveita `FaturaCicloService.ResolverFaturaParaLancamentoAsync`
  (o mesmo método usado por compra à vista), encadeado N vezes — parcela 1
  pela data da compra, parcela seguinte por um dia dentro do ciclo seguinte ao
  da parcela anterior (`DataVencimento.AddDays(1)`). Zero lógica de ciclo
  duplicada.
- **Split de valor**: divisão automática (`valor_total / quantidade_parcelas`),
  truncada em 2 casas nas N-1 primeiras parcelas, resto inteiro na última —
  garante que a soma bate exatamente com `valor_total` em qualquer caso
  (`ParcelamentoCalculator`, regra crítica sob TDD completo).
- **Persistência transacional**: resolução das N faturas + criação da
  `CompraParcelada` + N `Lancamento` só commitam como uma única operação —
  se qualquer fatura falhar no meio, nada fica órfão no banco.

## Modelo de dados e endpoints

`Lancamento` ganhou `CompraParceladaId`/`ParcelaNumero` (nullable, `null` pra
compra à vista), FK com `OnDelete(SetNull)`. Nova entidade `CompraParcelada`
(`Descricao`, `ValorTotal`, `QuantidadeParcelas`, `DataCompra`, sem `ContaId`
— a conta é resolvida via os `Lancamento` filhos). Migration `AddCompraParcelada`
(só `ADD`, a tabela `parcela` nunca chegou a existir via EF — schema-only).

Controle transacional exposto na interface `ICompraParceladaRepository`
(`BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`) — o `Service` nunca
recebe `DbContext` direto, só a abstração do repository.

Endpoint: `POST /api/contas/{contaId}/compras-parceladas` — 201 com
`CompraParceladaResponse` (inclui `Parcelas: CompraResponse[]`) em sucesso,
400 com `{ erro }` em falha de validação (quantidade < 2, valor ≤ 0, conta
não-cartão).

## Lacunas conhecidas

- **Estorno de compra parcelada**: fora de escopo desta leva, regra omissa.
  Virou demanda própria — ver `.claude/demands.md`, DEMANDA-006 (perguntas em
  aberto: cancela todas as parcelas futuras ou só uma; alcança fatura já paga
  ou só aberta/fechada; ação única ou por parcela).
- **Edição de compra parcelada existente** (mudar quantidade de parcelas
  depois de criada): fora de escopo, regra omissa, sem demanda aberta ainda.
- **Teto de `quantidade_parcelas`**: não há limite superior — `Service` só
  valida `>= 2`. Se precisar de um teto (12x, 24x), é decisão de produto.
- **Front (hanzo)**: não entrou nesta leva — formulário de criação e exibição
  agrupada "3/10" ficam pra rodada própria.

## O que cada agent entregou

- **killua**: arquitetou o módulo em duas rodadas — a primeira leu o brief
  errado (caminho relativo resolveu pro checkout principal, sem a decisão já
  tomada) e recusou gerar tasks em cima de contexto que não existia; corrigido
  com caminhos absolutos na worktree. Entregou 13 tasks (TASK-025 a 037) com
  esqueleto de TDD pra regra crítica de cálculo, e sinalizou de forma explícita
  as 3 suposições que precisavam de confirmação do usuário antes de codar
  (modelo N-lançamentos vs 1+N, split automático vs manual, ciclo de fatura vs
  soma de meses) — todas resolvidas via pergunta direta antes do levi começar.
- **levi**: implementou as 13 tasks; 3 rodadas de correção na TASK-037 (a
  mais complexa — orquestração do `Service`), todas apontadas pelo `style`.
- **style**: aprovou de primeira TASK-032 (`ParcelamentoCalculator`) só na
  2ª rodada (número mágico, `Aggregate` reinventando `Sum`, cálculo repetido
  em loop). Na TASK-037, achou um problema real e grave na 1ª rodada
  (transacionalidade quebrada — `FaturaCicloService.ResolverFaturaParaLancamentoAsync`
  commitava fatura nova no meio do loop de resolução, antes dos lançamentos
  existirem), e mais 2 rodadas de correção arquitetural (vazamento de
  `DbContext` via downcast pra tipo concreto, e código morto — métodos criados
  na interface do repository mas nunca chamados de fato).
- **mike**: 10 testes unitários do `ParcelamentoCalculator` (RED→GREEN) + 5
  testes de integração do `ComprasParceladasService` (split com resto de
  arredondamento, fatura correta por mês, rejeição sem persistência parcial,
  agrupamento por `CompraParceladaId`). Reconfirmou GREEN de forma
  independente em cada uma das 4 rodadas de correção do `style` na TASK-037.

## Notas operacionais

- Antes desta demanda, o `main` local sofreu uma regressão séria: um commit
  de "consolidação de DbContext" apagou ~9950 linhas de 4 módulos já
  mergeados (Categorias, Lançamento Geral, Cartão de Crédito, Investimento
  Detalhado), que só existiam intactos em `origin/main`. Foi corrigido via
  merge antes de qualquer trabalho desta demanda começar — ver histórico do
  `main`, commit `e118ee6`.
- Durante a execução da fila de tasks, o `tasks.md` desta worktree foi
  resetado para `STATUS: PENDENTE` em todas as tasks já concluídas — alguma
  rodada de subagent rodou um comando git amplo que descartou edições locais
  não commitadas. Corrigido reaplicando os status com os hashes corretos e
  passando a commitar cada atualização de status imediatamente após cada
  task, em vez de acumular. Todos os subagents subsequentes foram
  instruídos explicitamente a nunca usar `git add -A`/`git checkout`/`git reset`
  amplos, só `git add` nos arquivos exatos permitidos pela task.
