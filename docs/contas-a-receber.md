# Módulo: Contas a Receber

## Visão geral

Modela dívidas que giram a favor do usuário — dinheiro que vai entrar. Uma
única entidade `ContaReceber` cobre dois casos (campo `Tipo`): **RECEBIVEL**
(expectativa solta de entrada, sem vínculo com nenhuma conta do sistema) e
**EMPRESTIMO** (dinheiro que o usuário emprestou a uma pessoa — texto livre,
sem cadastro próprio). O valor combinado (`ValorTotal`) é fixo desde o
registro, sem juros/correção; o que muda com o tempo é o saldo pendente,
conforme recebimentos incrementais vão sendo registrados, até o estado virar
`RECEBIDO`.

## Regras de negócio implementadas

Item 13 da `regra-de-negocio.md`, ponto a ponto:

- **Empréstimo como transferência de perna única**: ao registrar, o valor sai
  da conta de origem via `Transferencia` com `ContaDestinoId = null` (campo
  virou nullable — antes só existia o padrão de duas pernas do item 3) e
  `ContaReceberId` preenchido, mais **um único** `Lancamento` (Debit, Pago)
  vinculado a essa transferência via `TransferenciaId`. Reaproveita a mesma
  regra de exclusão de gasto/receita do item 3 (`transferencia_id != null`),
  sem lógica nova.
- **Recebimento incremental**: cada recebimento gera um `Lancamento` novo
  (Credit, Pago) vinculado via `ContaReceberId`, na conta que o usuário
  escolher naquele momento (pode variar entre recebimentos).
- **Estados** (`ContaReceberSaldoCalculator`, espelha `FaturaSaldoCalculator`):
  `PENDENTE` (nada recebido) → `PARCIAL` (`0 < saldo_pendente < valor_total`)
  → `RECEBIDO` (`saldo_pendente == 0`). Persistido em `ContaReceber.Status`
  após cada recebimento.
- **Overpayment rejeitado**: recebimento que excede o saldo pendente atual
  lança `ValorRecebimentoExcedeSaldoPendenteException` (422), sem persistir
  nada — saldo pendente nunca fica negativo. Decisão confirmada com o usuário
  (a alternativa seria aceitar e deixar saldo negativo).
- **Projeção do mês** (item 9, fatia isolada — não existe endpoint de
  dashboard/projeção completo no codebase ainda): `total_a_receber_esperado_no_mes`
  soma o **saldo pendente** (nunca `valor_total`) de toda `ContaReceber`
  `PENDENTE` com `DataPrevista` caindo no mês/ano pedido, ou `PARCIAL` (sem
  filtro de data — entra em qualquer mês consultado, até zerar).

## Modelo de dados e endpoints

`ContaReceber` (tabela nova) + `TipoContaReceber`/`StatusContaReceber` (enums,
padrão `ToStorageValue`/`FromStorageValue` já usado no projeto). Duas tabelas
existentes ganharam FK nova: `Transferencia.ContaDestinoId` virou nullable e
ganhou `ContaReceberId`; `Lancamento` ganhou `ContaReceberId` (1:N via
`ContaReceber.Recebimentos`).

Endpoints (`ContasReceberController`):
- `POST /api/contas-receber/recebiveis`, `POST /api/contas-receber/emprestimos`
- `POST /api/contas-receber/{id}/recebimentos`
- `GET /api/contas-receber?status=`, `GET /api/contas-receber/{id}`
- `GET /api/contas-receber/total-esperado-mes?ano=&mes=`

Frontend em `MyFinanceFrontEnd/src/features/contas-receber/`: listagem com
badge de status/tipo, formulário de criar (toggle Recebível/Empréstimo) e ação
inline de registrar recebimento — mesmo padrão container/apresentação já
usado em `features/investimentos/`.

## Lacunas conhecidas

- ~~Sem endpoint de projeção/dashboard completo~~ — resolvido no módulo
  [Projeção do Mês](projecao-do-mes.md), que reaproveita
  `total_a_receber_esperado_no_mes` sem alteração.
- Sem combobox de categoria no frontend (não existe endpoint de listagem de
  categorias do usuário ainda) — `categoriaId` fica de fora dos formulários,
  embora continue opcional na API.
- Sem endpoint combinado de listagem de contas por múltiplos tipos — o front
  busca `banco`+`investimento` em duas chamadas paralelas (hook
  `useContasParaSelecao`) pra popular os selects de conta de origem/destino,
  excluindo `cartao` (linha de crédito, não fonte de fundos).

## O que cada agent entregou

- **killua**: modelou a entidade única com campo `Tipo`, decidiu a
  transferência de perna única (opção A: reaproveitar `Transferencia` com
  `ContaDestinoId` nullable, em vez de criar uma segunda regra de exclusão de
  gasto/receita), e quebrou o módulo em 15 tasks com ciclo TDD explícito na
  regra crítica.
- **levi**: implementou entidade/migrations, repository, service e controller
  em 4 rodadas de correção pós-`style` — a mais séria: `RegistrarEmprestimo`
  nunca setava `Lancamento.TransferenciaId`, o que quebraria silenciosamente a
  exclusão de gasto/receita do item 3 (todo empréstimo apareceria como
  despesa real). Também corrigiu `ContaReceber.Status` que nunca transicionava
  após recebimento (ficava travado em `PENDENTE` pra sempre).
- **mike**: TDD completo da regra crítica (16 testes RED→GREEN), mais testes
  de integração com SQLite in-memory que provaram dois bugs reais de
  `Include` ausente (`ObterPorId` e depois `Listar`/`ListarParaProjecaoDoMes`)
  — sem o `Include(Recebimentos)`, o cálculo de saldo pendente lia uma coleção
  sempre vazia em produção, mascarado pelos testes com mock.
- **hanzo**: camada de dados (types/api/hooks) e as 3 telas, seguindo
  `features/investimentos/` como referência. Achou que o token shadcn
  `--accent` do projeto não é o roxo de `identidade-visual.md` (é uma
  superfície neutra escura) — usou `--primary` pro badge de status `PARCIAL`,
  decisão documentada no componente. Extraiu `useContasParaSelecao`
  compartilhado entre os dois formulários que precisam de select de conta.
- **style**: 4 rodadas na regra crítica (2 bugs reais: `Status` não
  transicionava, `Include` ausente em `ObterPorId`), mais 2 rodadas no
  controller (`TransferenciaId` ausente — o achado mais grave do módulo — e
  nome de classe fora do padrão plural) e 4 rodadas na fatia de projeção
  (lógica correta desde a primeira, mas sem nenhum teste — depois achou 2
  testes duplicados disfarçados de diferentes e comentários acentuados).

## Notas operacionais

Padrão recorrente: código que passa em testes com mock pode esconder bug real
de carregamento de dados (`Include` ausente). Toda vez que um cálculo depende
de uma coleção de navegação (`ContaReceber.Recebimentos`), vale desconfiar dos
métodos de repository que ainda não foram auditados — o mesmo bug já se
repetiu 2x neste módulo (`ObterPorId`, depois `Listar`) antes de virar hábito
conferir todos os métodos de uma vez.
