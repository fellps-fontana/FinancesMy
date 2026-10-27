# Módulo: Planejamento Mensal (sublimite por categoria)

## Visão geral

Plano de um mês calendário: parte da renda prevista, desconta as despesas
fixas (conta fixa e assinatura de cartão) e o investimento, e distribui a
sobra em **sublimites percentuais** por categoria de despesa. Só calcula,
informa e alerta — nunca bloqueia lançamento nem gera lançamento,
transferência ou aporte. Substitui o antigo módulo de Limite de Gasto
(`docs/limite-gasto.md`, removido): o "limite de gasto" de uma categoria
passou a ser o sublimite dela no plano do mês, e a tabela `limite_gasto` foi
dropada.

## Regras de negócio implementadas

Referência: `regra-de-negocio.md` itens **20** (novo), **14** (reescrito como
sublimite) e **19** (barra do detalhe do relatório). Decisões do usuário
registradas na regra em 2026-10-13 e 2026-10-14.

- **Cálculo (item 20, crítico, TDD)**: fixas, investimento planejado e
  calculado (reduzido só o necessário quando não cabe), sobra, sublimite de
  1º nível (% da sobra) e de subcategoria (% do sublimite do pai),
  `nao_alocado` e `gasto_sem_sublimite`. Nada em R$ é gravado — tudo é
  recalculado a cada consulta, sem arredondamento intermediário. Cada despesa
  cai em um balde só: fixa **ou** sublimite/sem sublimite.
- **Gasto do sublimite (item 14)**: só gasto variável (sem conta fixa e
  assinatura), competência pela data, Pendente ou Pago; o pai soma as
  subcategorias diretas. Sublimite R$ 0 com qualquer gasto > 0 está estourado.
- **Renda prevista**: nasce pela soma das ocorrências dos moldes ATIVOS de
  recebível recorrente no mês (materializadas ou não); depois é do plano.
- **Geração sob demanda**: mês corrente/futuro sem plano nasce na primeira
  consulta copiando % de investimento e sublimites do plano anterior (sem
  categoria arquivada, nem subcategoria cujo pai ficou de fora). Mês passado
  é só leitura; sem plano continua sem plano.
- **Horizonte**: consulta e salvamento só até **mês corrente + 12**; além
  disso 400 e nada é criado (achado de segurança do `gon`).
- **Salvar**: substitui o conjunto inteiro de sublimites (replace-all, diff
  in-place); mês sem plano gravado nasce com os valores enviados. Validações:
  renda ≥ 0 e ≤ teto técnico, percentuais 0–100, soma de 1º nível ≤ 100, soma
  por pai ≤ 100, subcategoria exige pai, sem RECEITA nem arquivada (inclusive
  reenvio de sublimite pré-existente de categoria arquivada), sem duplicata.
- **Relatório (item 19)**: a barra do detalhe compara sublimite x gasto
  variável (pode ser menor que o total da linha); mês sem plano ou além do
  horizonte = sem barra.

## Modelo de dados e endpoints

- `plano_mensal` (`ano`, `mes` únicos; `renda_prevista` 18,2;
  `percentual_investimento` 5,2) e `plano_mensal_sublimite`
  (`plano_mensal_id` cascade, `categoria_id` restrict, `percentual` 5,2;
  único por plano + categoria). Migrations `AddPlanoMensal` e
  `DropLimiteGasto`.
- Domínio puro: `PlanejamentoMensalCalculator` (um método por termo),
  `PlanoMensalValidator` (ordem de checagem documentada),
  `PlanoMensalInicialFactory` (renda + cópia). `PlanejamentoMensalService`
  só orquestra; `PlanoMensalRepository.AdicionarOuObterExistente` resolve a
  corrida da geração pelo índice único (vale fora de transação explícita —
  nota LIMITACAO no código).
- `GET /api/planejamento-mensal/{ano}/{mes}` → 200 plano calculado (incluindo `gastosPorCategoria` com o gasto variável de todas as categorias `DESPESA`) | 404 mês
  passado sem plano | 400 ano/mês inválido ou fora do horizonte.
- `PUT /api/planejamento-mensal/{ano}/{mes}` (`rendaPrevista`,
  `percentualInvestimento`, `sublimites[]` — todos obrigatórios; `[]` limpa)
  → 200 | 400 | 404 categoria | 409 mês passado | 422 regra de sublimite.
- `GET /api/relatorios/gasto-por-categoria/detalhe`: `limite` agora traz
  `estourado` (o front não recalcula).
- MCP: `obter_planejamento_mensal` e `salvar_planejamento_mensal` substituem
  as 4 tools de limite de gasto.
- Front (`TASK-265..269`, 2026-10-22): tela unificada `/planejamento` (menu
  "Planejamento") reúne definição de sublimites e comparativo sublimite x
  gasto variável do mês em uma única tela, com edição inline em tempo real,
  inputs bidirecionais `%` e `R$`, listagem direta de todas as categorias
  `DESPESA` ativas de 1º nível (com expansão para subcategorias ativas) e
  barra explícita `"Salvar alterações"` / `"Descartar"`; `/limites-gasto`
  redireciona para `/planejamento` preservando `?categoriaId=`; indicador do
  dashboard, aviso do lançamento (mês da **data** do lançamento) e cadastro de
  categoria (sem campo de limite) migrados.

## Lacunas conhecidas

- O GET do plano (e o detalhe do relatório) **grava** quando gera o plano sob
  demanda. Permitido pelo item 20; se incomodar, criar caminho read-only no
  service.
- Dois salvamentos concorrentes do mesmo plano existente podem estourar o
  índice (plano, categoria) → 500. Single-user: só registrado.
- Fora da v1 (regra): conferir se o investimento foi realizado; diluir conta
  fixa ANUAL; sublimite em valor fixo.
- Premissa do projeto continua: dado sem `usuario_id` e registro aberto — o
  plano é global como o resto do domínio.

## O que cada agent entregou

- **killua**: modelagem (2 tabelas, domínio puro em 3 peças), esqueleto e
  decomposição TASK-242..264; redigiu as inserções de regra das decisões de
  2026-10-14. Sem Write — Kira gravou esqueleto, schema e regra.
- **mike**: 79 testes do calculator, 30 do validator, 21 da factory, 41 do
  service, 38 HTTP (relógio fake) e os do relatório. Os testes HTTP acharam 2
  bugs de produção que os mocks não pegavam (categoria arquivada fora da
  listagem; sublimite novo virando UPDATE no EF) e o 500 do `[Required]` em
  record.
- **levi**: implementação GREEN, controller, MCP, relatório, remoção do
  módulo antigo.
- **hanzo**: camada de dados e tela do planejamento, migração das telas que
  liam o limite, invalidação do cache do plano em 17 mutações.
- **style**: 4 rodadas no back e 2 no front. Principais: teto do ano
  (DateOnly), `Sublimites` não anulável, regressão do relatório com o
  horizonte, relatório descartando gasto de categoria arquivada (bug anterior
  à feature, corrigido), barra vazia com sublimite R$ 0 estourado, flash do
  plano antigo após salvar.
- **gon**: 2 rodadas. Achou GET criando plano para qualquer mês futuro
  (→ horizonte de 12 meses), renda acima de `numeric(18,2)` virando 500 e
  campos ausentes no PUT zerando o plano em silêncio.

## Notas operacionais

- Os itens 20/14/19 nasceram como alteração não commitada no checkout
  principal (base antiga); foram mesclados manualmente na regra deste
  worktree. A task original era TASK-171 e foi renumerada para TASK-242 por
  colisão de ID.
- Typecheck do front neste worktree usa junction de `node_modules` para o
  checkout principal (ignorada pelo git).
