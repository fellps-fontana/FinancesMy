# Módulo: Lançamento Geral

## Visão geral

Porte da DEMANDA-001 (implementada uma vez numa branch antiga nunca
mergeada, `worktree-lancamento-geral-tasks`) para a arquitetura atual
(`Domain/` + `MyFinancesDbContext`, Repository em vez de DbContext direto,
enum em vez de string constants). Cobre a base de todo lançamento manual do
sistema: classificação de entrada/saída por sinal, CRUD de lançamento em
conta MANUAL/BANCO, transferência entre contas do próprio usuário, e a visão
de fluxo de caixa (o que realmente "conta" como gasto/receita).

## Regras de negócio implementadas

Itens 1, 2, 3, 5 e 12 (parcial) da `regra-de-negocio.md`:

- **Classificação por sinal (item 2, CRÍTICA)**: `ClassificacaoLancamentoService.Classificar`
  nunca lê o campo `Valor` para decidir entrada/saída — só `Tipo` (Debit/Credit)
  e vínculos estruturais. Precedência: `TransferenciaId` > `FaturaId` > `Tipo`
  (Debit=Saída, Credit=Entrada). Prova coberta por 7 testes, incluindo o caso
  que testa Credit com valor negativo (garante que o sinal cru nunca é lido)
  e o de dupla precedência (TransferenciaId vence mesmo com FaturaId também
  preenchido).
- **Exclusão de lançamento manual = hard delete**, bloqueada se `TransferenciaId`,
  `FaturaId` ou `ConciliadoCom` estiverem preenchidos (lançamento vinculado a
  outra estrutura não se apaga isolado).
- **Escrita manual só aceita Status PENDENTE ou PAGO** — `SUGERIDO` é exclusivo
  de conciliação automática, fora de escopo v1, e é rejeitado na entrada.
- **Transferência manual (item 3)**: exige as duas contas com `Origem = MANUAL`
  e `Ativa = true` (validação de conta ativa não estava na regra escrita
  originalmente — decisão confirmada com o usuário em 2026-07-19, aplicada
  também no fluxo de lançamento manual). Cria 2 `Lancamento` (Debit
  origem/Credit destino), `Status=Pago`, `Manual=true`, mesmo `TransferenciaId`,
  atomicamente — mesma forma que `PagamentoFaturaService` (Cartão) já usa.
  A criação de lançamentos pareados foi extraída para `TransferenciaLancamentoHelper`,
  compartilhada entre `TransferenciaService` e `PagamentoFaturaService`.
- **Fluxo de caixa (item 12/3)**: compras de cartão (`FaturaId != null`) e
  lançamentos ocultos ficam de fora; cada transferência aparece como uma
  única linha lógica na visão de caixa de uma conta.

## Corte de escopo confirmado

`LancamentoOcultacaoService` (soft-delete de lançamento Open Finance,
`PATCH /ocultar`, item 4) **não foi portado** — `regra-de-negocio.md` marca
esse comportamento como fora de escopo v1, decisão tomada depois que a
branch antiga (que já tinha isso pronto) foi escrita.

## Modelo de dados e endpoints

Nenhuma tabela nova — reaproveita `Lancamento`/`Transferencia` já existentes
(portados pelo próprio rework do Cartão antes deste módulo). Repository
ganhou `Remover(Lancamento)` e `ListarParaFluxoCaixa(Guid? contaId)`.

Endpoint real ficou diferente do desenho original de killua: em vez de um
`LancamentosController` cross-conta (`GET /api/lancamentos`) + um
`ContaLancamentosController` separado para CRUD, tudo foi consolidado num
único `LancamentosController` sob `api/contas/{contaId}/lancamentos`:

- `POST /api/contas/{contaId}/lancamentos` — criar
- `PUT /api/contas/{contaId}/lancamentos/{id}` — editar
- `POST /api/contas/{contaId}/lancamentos/{id}/pagamentos` — marcar como pago
- `DELETE /api/contas/{contaId}/lancamentos/{id}` — remover (hard delete)
- `GET /api/contas/{contaId}/lancamentos/fluxo-caixa` — visão de caixa da conta
  (sempre escopada a uma conta específica, não cross-conta)
- `POST /api/transferencias` (`TransferenciasController`) — criar transferência
- `GET /api/lancamentos/fluxo-caixa` (rota absoluta, mesmo `LancamentosController`)
  — visão agregada de TODAS as contas do usuário, ver seção abaixo

## Frontend (Bloco K, 2026-07-30)

Tela `LancamentosPage` reconstruída seguindo o mockup `04 Lancamentos.dc.html`
(TASK-146/147): navegador de mês, cards de resumo (Entradas/Saídas/Saldo),
chips de filtro (Todos/Entradas/Saidas), lista agrupada por data, FAB.

O style review da entrega encontrou um CRÍTICO: o resumo do mês e os chips
somavam por `Tipo` cru, sem excluir transferência/pagamento de fatura (item 3,
via item 2 CRITICA) — o `LancamentoResponseDto` não expunha nenhum dado de
classificação pro front distinguir isso. Corrigido em ciclo TDD completo:
`LancamentoResponseDto` ganhou o campo `Classificacao` (serialização do
`ClassificacaoLancamentoService` já existente, sem duplicar a regra), e o
front passou a excluir `TRANSFERENCIA`/`COMPETENCIA_CARTAO` do cálculo
(`deveContarComoEntradaOuSaida` em `lib/filtrarPeriodo.ts`) — a lista
"Todos" continua exibindo a transferência normalmente, só não soma no resumo.

## Visão agregada e "novo lançamento" por conta (2026-08-17, PR #62)

Fecha a lacuna abaixo (~~fluxo de caixa não tem visão cross-conta~~) e mais
dois pedidos de feedback do usuário: por padrão a tela deve mostrar todas as
contas, e a escolha de conta deve morar dentro do fluxo de "novo lançamento",
não como filtro de página.

- **Endpoint** `GET /api/lancamentos/fluxo-caixa`: `FluxoCaixaService.ListarFluxoCaixaTodasContas()`
  compõe, em memória (duas queries, sem join SQL — volume de uso pessoal não
  justifica), os `Lancamento`s reais (`ILancamentoRepository.ListarParaFluxoCaixa(null)`,
  que já exclui compra de cartão e tudo `Classificacao == Transferencia`) com
  as `Transferencia`s (`ITransferenciaRepository.ListarTodas()`) — cada
  transferência (comum, pagamento de fatura via `EhPagamentoFatura`, ou
  empréstimo com `ContaDestinoId == null`, item 13) vira UMA linha lógica em
  vez de aparecer como as duas pernas cruas, fechando de vez o espírito do
  item 3 também na visão agregada (na visão por conta, item 3 já era
  respeitado, mas só "por acidente de escopo" — a outra perna vivia numa
  conta que a query não olhava; aqui é fusão deliberada). Envelope
  discriminado `FluxoCaixaItemDto` (`TipoItem` enum `TipoItemFluxoCaixa`,
  `Lancamento`/`Transferencia` opcionais) — não força os dois formatos num
  DTO só, porque não têm o mesmo shape (transferência não tem `Id` de
  lançamento único, nem `Tipo`, nem `CategoriaId`).
- **Frontend**: `LancamentosPage` abre direto na visão agregada (sem select
  de conta bloqueando a tela). Cada item renderiza `LancamentoItem` ou o novo
  `TransferenciaFluxoCaixaItem` conforme `tipoItem` — este último sem
  ações (editar/pagar/remover não existem pra transferência em nenhum lugar
  da regra). Resumo do mês e chips Entrada/Saída ignoram transferência
  (neutra, item 3); só aparece no chip "Todos". `FormLancamento` ganhou
  select de conta próprio (obrigatório, só modo criar) — modo editar
  continua sem campo de troca de conta (regra omissa, não inventado).
- **Empréstimo na visão agregada**: rótulo genérico "Empréstimo", sem nome
  da pessoa (esse dado vive em `ContaReceber.Pessoa`, não em `Transferencia`
  — juntar exigiria mais um join, fora de escopo).
- **Ciclo de correção `style`** (único bloco desta leva que tocou regra de
  negócio, os outros 2 itens do PR #62 foram puramente visuais): 1ª rodada
  reprovou por 2 motivos — `TipoItem` como string mágica em vez do padrão
  enum+`ToStorageValue()` já convencionado no projeto, e a composição
  lançamento+transferência (regra crítica) sem nenhum teste. Corrigido:
  enum `TipoItemFluxoCaixa` extraído, e `mike` escreveu 6 cenários contra a
  implementação já existente — achou 5 falhas na primeira rodada, que a
  investigação (Kira, leitura de `LancamentoRepository.cs:63-68`) confirmou
  serem falso positivo do mock de teste (não replicava o filtro real do
  repositório, que já exclui `Classificacao == Transferencia` no branch
  agregado) — corrigido o setup do mock, não o código de produção. 2ª rodada:
  aprovado, 568/568 GREEN.
- **Débito técnico reconhecido**: `useFluxoCaixa.ts`/`listarFluxoCaixa`
  (escopados por conta, endpoint antigo) seguem vivos porque
  `features/dashboard/hooks/useUltimosLancamentos.ts` ainda depende deles —
  candidato a task futura de migração pro endpoint agregado.

## Lacunas conhecidas

- `LancamentoRepository.ListarParaFluxoCaixa(Guid? contaId)` (o método que
  exclui transferência no branch agregado, pré-existente, base de que o
  endpoint acima depende) não tem teste de repositório próprio — apontado
  pelo `style` na revisão do PR #62, não bloqueante, mas é uma lacuna real
  sustentando os testes do service sem prova automatizada da camada de
  baixo.

## O que cada agent entregou

Todo o ciclo abaixo foi conduzido **diretamente pelo usuário com Claude
Code, fora da fila do Kira** (`tasks.md` só foi sincronizado depois, em
2026-07-20/21, quando a reconciliação encontrou `.claude/decisions.md` já
com as 12 tasks — TASK-039 a TASK-050 — como `APROVADO`):

- **killua**: decompôs o porte em 13 tasks (038 a 050), mapeando o que já
  tinha sido portado de graça pelo rework do Cartão (`Domain/Lancamento.cs`,
  `Transferencia.cs`, repositories) vs. o que faltava (Service/Controller/DTO).
- **levi**: implementou `ClassificacaoLancamentoService`, `LancamentoManualService`,
  `TransferenciaService`, `FluxoCaixaService` e os controllers, em ciclo
  TDD completo na regra crítica de classificação.
- **mike**: RED→GREEN da regra crítica de classificação (7 testes), testes
  de service para os 3 services novos, testes HTTP dos 2 controllers.
- **style**: revisão geral (TASK-050) encontrou 5 problemas antes de aprovar —
  o mais grave (CRÍTICO): `ListarParaFluxoCaixa` descartava a perna CREDIT de
  toda transferência do filtro por conta, escondendo metade da transferência
  na visão de caixa do destino. Também achou falta de validação de conta
  ativa em `MarcarComoPagoAsync`/`EditarAsync`, status HTTP inconsistentes
  entre controllers, e duplicação de lógica entre `TransferenciaService` e
  `PagamentoFaturaService` (resolvida com `TransferenciaLancamentoHelper`).

**Bloco K, 2026-07-30 (PR #48)** — via Kira, fila `tasks.md` TASK-146/147:

- **hanzo**: reconstruiu `LancamentosPage`/`LancamentoItem` seguindo o
  mockup 04 e implementou, depois, `deveContarComoEntradaOuSaida` no front.
- **style**: rodada 1 achou o CRÍTICO da exclusão de transferência (ver
  seção Frontend acima); rodada 2 aprovou após o ciclo TDD.
- **killua**: desenhou o contrato do campo `Classificacao` no DTO (enum
  string vs. boolean — optou por enum pra não perder informação já
  calculada pelo `ClassificacaoLancamentoService`).
- **mike**: 9 testes RED/GREEN em `LancamentoResponseDtoTests.cs` cobrindo
  os 4 valores de `ClassificacaoLancamento` e o caso de transferência/fatura.
- **levi**: implementou `ClassificacaoLancamentoExtensions.ToStorageValue`.

## Notas operacionais

**Bug pós-merge entre módulos, encontrado e corrigido em 2026-07-20/21
(PR #30):** depois que o módulo Contas a Receber foi mergeado (tornando
`Transferencia.ContaDestinoId` de `Guid` para `Guid?`, para suportar
empréstimo sem conta destino), a `main` ficou com build quebrado —
`TransferenciaResponse.cs` deste módulo ainda declarava `ContaDestinoId`
como `Guid` não-nulo (erro `CS0266`). Nenhum dos dois módulos, revisado
isoladamente, tinha como prever essa colisão — só apareceu na integração
sequencial dos dois merges. Corrigido tornando o campo `Guid?` no DTO;
suite completa (324/324) confirmada verde depois do fix.

Lição: quando dois módulos alteram nullability de um campo compartilhado
(`Transferencia` é usada por Cartão, Lançamento Geral e Contas a Receber),
vale conferir DTOs que espelham a entidade em TODOS os módulos consumidores
antes de considerar o merge seguro, não só o módulo que fez a mudança.
