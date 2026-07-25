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

## Lacunas conhecidas

- Fluxo de caixa não tem visão cross-conta via API (o `contaId` é sempre
  obrigatório na rota) — se um dashboard geral precisar agregar todas as
  contas, isso ainda não existe.
- Nenhuma UI de frontend foi construída para este módulo nesta leva — é
  puramente backend (Service/Controller/DTO).

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
