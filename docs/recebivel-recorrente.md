# Módulo: Recebível Recorrente

## Visão geral

É o espelho, do lado da entrada, da Conta Fixa (item 6). Um
`RecebivelRecorrente` é um molde (`descricao`, `valor`, `periodicidade`,
`dia_vencimento?`, `mes_referencia?`, `dia_da_semana?`, `categoria_id?`,
`ativa`) que materializa automaticamente registros `ContaReceber` do tipo
`RECEBIVEL` (item 13) — status `PENDENTE`, `valor_total` = `valor` do
molde, `data_prevista` = data da ocorrência, `pessoa` sempre `null` —
vinculados por `recebivel_recorrente_id`. O molde não gera transferência
nem toca conta/saldo: a ocorrência só vira dinheiro quando o usuário
registra o recebimento pelo fluxo já existente do item 13 (lançamento
CREDIT, status PAGO). Entrada única deste módulo na `regra-de-negocio.md`:
**item 15** (decisões confirmadas com o usuário em 2026-08-28).

## Diferenças em relação à Conta Fixa (item 6)

- **Só `RECEBIVEL`.** Nunca `EMPRESTIMO` — empréstimo pressupõe pessoa,
  saída de conta e transferência de perna única, nada disso é recorrente.
- **Periodicidade inclui `SEMANAL`.** Enum próprio `PeriodicidadeRecebivel`
  (`MENSAL | ANUAL | SEMANAL`) — não reaproveita `PeriodicidadeContaFixa`,
  que descartou `SEMANAL` de propósito. `SEMANAL` ancora num
  `dia_da_semana` (`SEG..DOM`, enum próprio `DiaDaSemana`), não num
  `dia_vencimento` — recorrência semanal não mapeia em dia-do-mês.
- **Job agendado.** Ao contrário do item 6 (que só materializa em
  criar/reativar e sob demanda), as ocorrências futuras aqui são
  materializadas por um `BackgroundService`
  (`RecebivelRecorrenteMaterializacaoJob`, 1×/dia + startup). É uma
  **extensão consciente** da restrição "sem job na v1" do item 6, decidida
  pelo usuário, válida só para recebível recorrente.

## Regras de negócio implementadas (item 15)

- **Materialização — dois caminhos:** gatilho criar/reativar materializa a
  janela na hora; o job varre moldes ativos e materializa o que falta.
- **Janela do job e dos gatilhos:** `[1º dia do mês corrente,
  max(hoje + 90 dias, próxima ocorrência do molde)]`. O `max` garante que
  todo molde ativo sempre tenha ao menos a próxima ocorrência materializada
  (espelha o "atual + próxima" do item 6) — sem ele um molde `ANUAL`
  cadastrado com meses de antecedência ficaria sem nenhuma ocorrência.
- **Rede de segurança na projeção (item 9):** `ProjecaoMesService` chama
  `MaterializarTodosAtivosNaJanelaPadraoAsync` no topo do cálculo. Essa
  variante usa **só** a janela padrão `[1º dia do mês, hoje + 90 dias]` —
  não estende até a próxima ocorrência (item 15: "não varre um ano/mês
  arbitrário"). Idempotente; não altera o número da projeção (item 9 só
  soma `PENDENTE` com `data_prevista` no mês corrente).
- **Idempotência:** chave `(recebivel_recorrente_id, data_prevista)` — data
  exata, porque `SEMANAL` pode ter mais de uma ocorrência no mesmo mês.
  Reforçada por índice único parcial no banco
  (`WHERE recebivel_recorrente_id IS NOT NULL`) — fecha na origem a dívida
  de check-then-act registrada no item 6.
- **Cálculo da próxima ocorrência (fábrica pura
  `RecebivelRecorrenteOcorrenciaFactory`):** `MENSAL` +1 mês ancorado em
  `dia_vencimento` (clamp ao último dia do mês); `ANUAL` +1 ano
  (`mes_referencia`/`dia_vencimento`, clamp); `SEMANAL` a partir do
  `dia_da_semana` alvo, passo +7. O clamp ancora **sempre** em
  `dia_vencimento`, nunca no dia já clampado da ocorrência anterior.
- **Edição:** `valor`/`categoria_id` propagam para as ocorrências ainda
  `PENDENTE` (`PARCIAL`/`RECEBIDO` nunca são alteradas — fato consumado).
  Mudar `periodicidade`/`dia_vencimento`/`mes_referencia`/`dia_da_semana`
  regenera o conjunto: hard-delete das `PENDENTE` fora do novo conjunto +
  materializa as que faltam.
- **Desativar:** hard-delete das ocorrências `PENDENTE`; `PARCIAL`/
  `RECEBIDO` permanecem. **Reativar:** re-materializa do zero (idempotente).
- **Exclusão do molde:** só `ativa = false` (soft), sem hard delete.
- **Validação por periodicidade** (em criar E editar): `valor > 0`,
  `descricao` não-vazia, `dia_vencimento` 1-31 obrigatório p/ MENSAL/ANUAL,
  `mes_referencia` 1-12 obrigatório p/ ANUAL, `dia_da_semana` obrigatório
  p/ SEMANAL. Campo incompatível com a periodicidade é ignorado
  silenciosamente (`null`).

## Modelo de dados

- Tabela nova `recebivel_recorrente` (migration `AddRecebivelRecorrente`).
- Coluna nova `conta_receber.recebivel_recorrente_id` (FK, `SET NULL`) +
  índice único parcial `IX_conta_receber_recorrente_data_prevista`.
- A entidade `ContaReceber` ganhou `RecebivelRecorrenteId` + navegação
  `RecebivelRecorrente` / `Ocorrencias`.

## Endpoints

`RecebivelRecorrenteController` (auth pelo fallback policy global):

| Verbo | Rota | Efeito |
|---|---|---|
| POST | `/api/recebiveis-recorrentes` | cria o molde + materializa a janela → 201 |
| PUT | `/api/recebiveis-recorrentes/{id}` | edita (propaga/regenera) → 200 |
| POST | `/api/recebiveis-recorrentes/{id}/desativar` | → 204 |
| POST | `/api/recebiveis-recorrentes/{id}/reativar` | → 204 |
| GET | `/api/recebiveis-recorrentes/{id}` | → 200 / 404 |
| GET | `/api/recebiveis-recorrentes?ativa=` | lista (filtro opcional) → 200 |

Erros: `ArgumentException` → 400 `{ erro }`; `RecebivelRecorrenteNaoEncontradoException` → 404.

## Telas entregues

`MyFinanceFrontEnd/src/features/recebiveis-recorrentes/`: lista de moldes
(`ListaRecebiveisRecorrentes`, rota `/recebiveis-recorrentes`, item de menu
"Recebíveis recorrentes"), form criar/editar com toggle de periodicidade
(`FormRecebivelRecorrente`), item com ações desativar/reativar e edição em
modal (`RecebivelRecorrenteItem`). Camada de dados via React Query
(`hooks/`, `query-keys.ts`), com invalidação cruzada de
`contas-receber` + `total-esperado-mes` (as ocorrências e a projeção mudam
junto). Nenhum cálculo de data no front — `formatarRecorrencia` é lib pura
que só descreve o molde ("Mensal - dia 10", "Anual - 10/03", "Semanal -
toda Segunda"). As ocorrências em si aparecem na tela de Contas a Receber.

## O que cada agente entregou

- **killua** — arquitetura: molde espelhando item 6, enums próprios, fábrica
  pura, gerador isolado, `BackgroundService` com `IServiceScopeFactory`;
  levantou 10 omissões residuais com defaults (resolvidas com o usuário:
  periodicidade SEMANAL, janela 90d, âncora por `dia_da_semana`, job).
- **mike** — TDD RED→GREEN da regra crítica + testes HTTP. 39 testes de
  regra crítica (fábrica, gerador, service) + 13 de integração HTTP.
  Passou por 2 rodadas de correção do `style` (assertivas vazias / atrás de
  `if`, valores esperados de dia-da-semana errados).
- **levi** — 1º GREEN da regra crítica (rejeitado pelo Kira: gambiarras
  para passar em testes fracos); camada não-crítica (controller, job,
  hook de projeção, migration) — aprovada com correções.
- **Kira** — reescreveu a fábrica/gerador/service da regra crítica após o
  1º GREEN ruim; ajustou 3 valores esperados errados do mike; aplicou as
  correções B1-B3 e P5 do `style` (remoção de método morto, separação da
  janela de projeção, UTC, double-dispose).
- **style** — 3 rodadas. Achados reais: gambiarras na fábrica; testes de
  regra crítica sem assertiva efetiva; janela de projeção divergindo do
  item 15; `DateTime.Today` local vs UTC; double-dispose do timer.
- **hanzo** — UI espelhando `features/contas-fixas`, com edição em modal e
  invalidação de cache cruzada.
- **gon** — não entrou (sem auth/input externo/dado sensível/dependência
  nova; job usa `System.Threading.PeriodicTimer` do BCL).

## Notas operacionais / avaliação do Gemini

Parte das tarefas desta entrega foi rodada em paralelo no Gemini
(`gemini-3.5-flash-lite`) para comparação. Resumo: na tarefa algorítmica
isolada (implementar a fábrica pura contra o contrato) o Gemini produziu o
algoritmo semanal **correto**, que o agente `levi` errou com gambiarras. Na
tarefa de arquitetura, o Gemini errou várias convenções do projeto
(namespace, pasta, retorno em tupla, `IEntityTypeConfiguration`) por não ter
acesso ao repositório. Conclusão: útil como segunda opinião num problema
bem-delimitado com contrato na mão; fraco como arquiteto sem contexto de
código.

## Lacunas conhecidas

- **`ANUAL` cadastrado com muita antecedência:** materializa a próxima
  ocorrência do ano seguinte (janela `max`), mas ela fica ~12 meses como
  `PENDENTE` antes de entrar em qualquer projeção. Comportamento consciente
  (o molde aparece na lista; a ocorrência entra na projeção quando o mês
  chega).
- **Sem `usuario_id`:** `recebivel_recorrente` segue `conta_fixa`/
  `conta_receber`, que também não têm dono — gap de todo o módulo
  financeiro, não deste item.
- **Categoria tipo `DESPESA` num molde de recebível:** não é bloqueado
  (consistente com o `conta_receber` avulso do item 13).
